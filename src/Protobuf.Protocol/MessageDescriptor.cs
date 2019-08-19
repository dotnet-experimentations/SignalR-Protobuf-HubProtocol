using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Protobuf.Protocol
{
    public static class MessageDescriptor
    {
        public static ReadOnlySpan<byte> PackMessage(int messageType, ReadOnlySpan<byte> protobufMessage)
        {
            return PackMessage(messageType, protobufMessage, null);
        }

        public static ReadOnlySpan<byte> PackMessage(int messageType, ReadOnlySpan<byte> protobufMessage, List<ArgumentDescriptor> arguments)
        {
            var argumentLength = arguments?.Sum(argument => argument.Argument.Length + ProtobufHubProtocolConstants.ARGUMENT_HEADER_LENGTH);

            var totalLength = 1 // messageType
                            + 4 // totalLength
                            + 4 // ProtobufMessageLength
                            + protobufMessage.Length
                            + (argumentLength ?? 0);

            var byteArray = ArrayPool<byte>.Shared.Rent(totalLength);
            try
            {
                byteArray[0] = (byte)messageType;
                BitConverter.GetBytes(totalLength - ProtobufHubProtocolConstants.TYPE_AND_TOTAL_LENGTH_HEADER).CopyTo(byteArray, 1);
                BitConverter.GetBytes(protobufMessage.Length).CopyTo(byteArray, 5);
                protobufMessage.ToArray().CopyTo(byteArray, ProtobufHubProtocolConstants.MESSAGE_HEADER_LENGTH);

                var currentLength = protobufMessage.Length + ProtobufHubProtocolConstants.MESSAGE_HEADER_LENGTH;

                for (var i = 0; i < arguments?.Count; i++)
                {
                    BitConverter.GetBytes(arguments[i].Type).CopyTo(byteArray, currentLength);
                    BitConverter.GetBytes(arguments[i].Argument.Length).CopyTo(byteArray, currentLength + ProtobufHubProtocolConstants.ARGUMENT_HEADER_LENGTH / 2);
                    arguments[i].Argument.CopyTo(byteArray, currentLength + ProtobufHubProtocolConstants.ARGUMENT_HEADER_LENGTH);
                    currentLength = currentLength + ProtobufHubProtocolConstants.ARGUMENT_HEADER_LENGTH + arguments[i].Argument.Length;
                }

                return byteArray.AsSpan().Slice(0, totalLength);
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
            return message[0];
        }

        public static int GetTotalMessageLength(ReadOnlySpan<byte> message)
        {
            // We need at least 5 bytes to be get the total length 
            if (message.Length < 5)
            {
                return 0;
            }

            return BitConverter.ToInt32(message.Slice(1, 4));
        }

        // Without a complete header, we are not able to retrieve the protobuf object message
        public static ReadOnlySpan<byte> GetProtobufMessage(ReadOnlySpan<byte> message)
        {
            if (message.Length <= ProtobufHubProtocolConstants.MESSAGE_HEADER_LENGTH)
            {
                return new ReadOnlySpan<byte>();
            }

            var protobufMessageLength = BitConverter.ToInt32(message.Slice(5, 4));

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

            var protobufMessageLength = BitConverter.ToInt32(message.Slice(5, 4));

            message = message.Slice(ProtobufHubProtocolConstants.MESSAGE_HEADER_LENGTH + protobufMessageLength);

            while (!message.IsEmpty)
            {
                var argumentType = BitConverter.ToInt32(message.Slice(0, 4));
                var argumentLength = BitConverter.ToInt32(message.Slice(4, 4));
                var argument = message.Slice(8, argumentLength).ToArray();

                var messageArgument = new ArgumentDescriptor(argumentType, argument);
                arguments.Add(messageArgument);

                message = message.Slice(8 + argumentLength);
            }

            return arguments;
        }
    }
}
