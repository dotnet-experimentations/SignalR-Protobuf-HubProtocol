using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Protobuf.Protocol
{
    // TODO: Handle Big Endian
    public static class MessageDescriptor
    {
        public static ReadOnlySpan<byte> PackMessage(int messageType, ReadOnlySpan<byte> protobufMessage)
        {
            return PackMessage(messageType, protobufMessage, null);
        }

        public static ReadOnlySpan<byte> PackMessage(int messageType, ReadOnlySpan<byte> protobufMessage, List<ArgumentDescriptor> arguments)
        {
            var argumentLength = arguments?.Sum(argument => argument.Argument.Length + ProtobufHubProtocolConstants.ARGUMENT_HEADER_LENGTH);

            var totalLength = ProtobufHubProtocolConstants.TYPE_PLACEHOLDER_SIZE // messageType
                            + ProtobufHubProtocolConstants.TOTAL_LENGTH_PLACEHOLDER_SIZE // totalLength
                            + ProtobufHubProtocolConstants.PROTOBUF_MESSAGE_LENGTH_PLACEHOLDER_SIZE // ProtobufMessageLength
                            + protobufMessage.Length
                            + (argumentLength ?? 0);

            var byteArray = ArrayPool<byte>.Shared.Rent(totalLength);
            try
            {
                byteArray[0] = (byte)messageType;

                // Span share memory adress with byteArray
                var span = byteArray.AsSpan(ProtobufHubProtocolConstants.TYPE_PLACEHOLDER_SIZE);
                var messageTotalLength = totalLength - ProtobufHubProtocolConstants.TYPE_AND_TOTAL_LENGTH_HEADER;
                MemoryMarshal.Write(span, ref messageTotalLength);

                span = byteArray.AsSpan(ProtobufHubProtocolConstants.TYPE_AND_TOTAL_LENGTH_HEADER);
                var protobufMessageLength = protobufMessage.Length;
                MemoryMarshal.Write(span, ref protobufMessageLength);
 
                span = byteArray.AsSpan(ProtobufHubProtocolConstants.MESSAGE_HEADER_LENGTH);

                protobufMessage.CopyTo(span);

                var currentLength = protobufMessage.Length + ProtobufHubProtocolConstants.MESSAGE_HEADER_LENGTH;

                for (var i = 0; i < arguments?.Count; i++)
                {
                    span = byteArray.AsSpan(currentLength);
                    var argType = arguments[i].Type;
                    MemoryMarshal.Write(span, ref argType);

                    span = byteArray.AsSpan(currentLength + ProtobufHubProtocolConstants.ARG_TYPE_PLACEHOLDER_SIZE);
                    var argLength = arguments[i].Argument.Length;
                    MemoryMarshal.Write(span, ref argLength);

                    span = byteArray.AsSpan(currentLength + ProtobufHubProtocolConstants.ARGUMENT_HEADER_LENGTH);
                    arguments[i].Argument.CopyTo(span);

                    currentLength = currentLength + ProtobufHubProtocolConstants.ARGUMENT_HEADER_LENGTH + argLength;
                }

                return byteArray.AsSpan(0, totalLength);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteArray);
            }
        }

        public static byte GetMessageType(ReadOnlySpan<byte> message)
        {
            if (message.Length <= 0)
            {
                return 0;
            }
            return MemoryMarshal.Read<byte>(message.Slice(0, ProtobufHubProtocolConstants.TYPE_PLACEHOLDER_SIZE));
        }

        public static int GetTotalMessageLength(ReadOnlySpan<byte> message)
        {
            // We need at least 5 bytes to be get the total length 
            if (message.Length < ProtobufHubProtocolConstants.TYPE_AND_TOTAL_LENGTH_HEADER)
            {
                return 0;
            }

            return MemoryMarshal.Read<int>(message.Slice(ProtobufHubProtocolConstants.TYPE_PLACEHOLDER_SIZE, ProtobufHubProtocolConstants.TOTAL_LENGTH_PLACEHOLDER_SIZE));
        }

        // Without a complete header, we are not able to retrieve the protobuf object message
        public static ReadOnlySpan<byte> GetProtobufMessage(ReadOnlySpan<byte> message)
        {
            if (message.Length <= ProtobufHubProtocolConstants.MESSAGE_HEADER_LENGTH)
            {
                return new ReadOnlySpan<byte>();
            }

            var protobufMessageLength = MemoryMarshal.Read<int>(message.Slice(ProtobufHubProtocolConstants.TYPE_AND_TOTAL_LENGTH_HEADER, ProtobufHubProtocolConstants.PROTOBUF_MESSAGE_LENGTH_PLACEHOLDER_SIZE));

            return message.Slice(ProtobufHubProtocolConstants.MESSAGE_HEADER_LENGTH, protobufMessageLength);
        }

        public static List<ArgumentDescriptor> GetArguments(ReadOnlySpan<byte> message)
        {
            var arguments = new List<ArgumentDescriptor>();

            // Without a complete header, we are not able to retrieve the arguments descriptors
            if (message.Length <= ProtobufHubProtocolConstants.MESSAGE_HEADER_LENGTH)
            {
                return arguments;
            }

            var protobufMessageLength = MemoryMarshal.Read<int>(message.Slice(ProtobufHubProtocolConstants.TYPE_AND_TOTAL_LENGTH_HEADER, ProtobufHubProtocolConstants.PROTOBUF_MESSAGE_LENGTH_PLACEHOLDER_SIZE));

            message = message.Slice(ProtobufHubProtocolConstants.MESSAGE_HEADER_LENGTH + protobufMessageLength);

            while (!message.IsEmpty)
            {
                var argumentType = MemoryMarshal.Read<int>(message.Slice(0, ProtobufHubProtocolConstants.ARG_TYPE_PLACEHOLDER_SIZE));
                var argumentLength = MemoryMarshal.Read<int>(message.Slice(ProtobufHubProtocolConstants.ARG_TYPE_PLACEHOLDER_SIZE, ProtobufHubProtocolConstants.ARG_LENGTH_PLACEHOLDER_SIZE));
                var argument = message.Slice(ProtobufHubProtocolConstants.ARGUMENT_HEADER_LENGTH, argumentLength).ToArray();

                var messageArgument = new ArgumentDescriptor(argumentType, argument);
                arguments.Add(messageArgument);

                message = message.Slice(ProtobufHubProtocolConstants.ARGUMENT_HEADER_LENGTH + argumentLength);
            }

            return arguments;
        }
    }
}
