using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protobuf.Protocol
{
    public class MessageDescriptor
    {
        public ReadOnlySpan<byte> PackMessage(int messageType, ReadOnlySpan<byte> protobufMessage, List<ArgumentDescriptor> arguments)
        {
            var argumentLength = arguments.Sum(argument => argument.Argument.Length + ProtobufHubProtocolConstants.ARGUMENT_HEADER_SIZE);

            var totalLength = 1 // messageType
                            + 4 // totalLength
                            + 4 // ProtobufMessageLength
                            + protobufMessage.Length
                            + argumentLength;

            var byteArray = ArrayPool<byte>.Shared.Rent(totalLength);
            try
            {
                byteArray[0] = (byte)messageType;
                BitConverter.GetBytes(totalLength - 5).CopyTo(byteArray, 1);
                BitConverter.GetBytes(protobufMessage.Length).CopyTo(byteArray, 5);
                protobufMessage.ToArray().CopyTo(byteArray, ProtobufHubProtocolConstants.MESSAGE_HEADER_SIZE);

                var currentLength = protobufMessage.Length + ProtobufHubProtocolConstants.MESSAGE_HEADER_SIZE;
                for (var i = 0; i < arguments.Count; i++)
                {
                    BitConverter.GetBytes(arguments[i].Type).CopyTo(byteArray, currentLength);
                    BitConverter.GetBytes(arguments[i].Argument.Length).CopyTo(byteArray, currentLength + ProtobufHubProtocolConstants.ARGUMENT_HEADER_SIZE / 2);
                    arguments[i].Argument.CopyTo(byteArray, currentLength + ProtobufHubProtocolConstants.ARGUMENT_HEADER_SIZE);
                    currentLength = currentLength + ProtobufHubProtocolConstants.ARGUMENT_HEADER_SIZE + arguments[i].Argument.Length;
                }

                return byteArray.AsSpan().Slice(0, totalLength);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteArray);
            }
        }
    }
}
