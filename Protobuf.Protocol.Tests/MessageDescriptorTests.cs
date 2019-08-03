using Microsoft.AspNetCore.SignalR.Protocol;
using SignalR.Protobuf.Protocol;
using Xunit;
using Google.Protobuf;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Protobuf.Protocol.Tests
{
    public class MessageDescriptorTests
    {
        public const int ARGUMENT_TYPE = 8;

        private List<ArgumentDescriptor> GetArgumentsDescriptors(params string[] arguments)
        {
            var argumentsDescriptors = new List<ArgumentDescriptor>();

            foreach (var argument in arguments)
            {
                argumentsDescriptors.Add(
                    new ArgumentDescriptor(ARGUMENT_TYPE, new TestMessage { Data = argument }.ToByteArray())
                );
            }

            return argumentsDescriptors;
        }

        [Theory]
        [InlineData(HubProtocolConstants.InvocationMessageType, "Foo", "Bar")]
        [InlineData(HubProtocolConstants.InvocationMessageType, "toto", "tata", "tutu")]
        [InlineData(HubProtocolConstants.StreamInvocationMessageType, "testData", "arg1", "arg2 should be longer", "arg3 should be much more longer than arg1 and arg2")]
        [InlineData(HubProtocolConstants.CloseMessageType, "", "")]
        public void MessageDescriptor_Should_Properly_Pack_A_Message_Proto(int messageType, string data, params string[] arguments)
        {
            var messageDescriptor = new MessageDescriptor();
            var protobufMessage = new TestMessage { Data = data };
            var protobufMessageSerialized = protobufMessage.ToByteArray();
            var argumentsDescriptors = GetArgumentsDescriptors(arguments);
            var totalLength = protobufMessageSerialized.Length + argumentsDescriptors.Sum(argument => argument.Argument.Length + ProtobufHubProtocolConstants.ARGUMENT_HEADER_SIZE) + 4; //Int => // ProtobufMessageLength

            ReadOnlySpan<byte> messagePacked = messageDescriptor.PackMessage(messageType, protobufMessageSerialized, argumentsDescriptors);

            Assert.Equal(messageType, messagePacked[0]);
            Assert.Equal(totalLength, BitConverter.ToInt32(messagePacked.Slice(1, 4)));
            Assert.Equal(protobufMessageSerialized.Length, BitConverter.ToInt32(messagePacked.Slice(5, 4)));
            var protobufObject = new TestMessage();
            protobufObject.MergeFrom(messagePacked.Slice(ProtobufHubProtocolConstants.MESSAGE_HEADER_SIZE, protobufMessageSerialized.Length).ToArray());
            Assert.Equal(protobufMessage, protobufObject);

            var serializedArguments = messagePacked.Slice(ProtobufHubProtocolConstants.MESSAGE_HEADER_SIZE + protobufMessageSerialized.Length);

            var i = 0;
            while (!serializedArguments.IsEmpty)
            {
                var argumentType = BitConverter.ToInt32(serializedArguments.Slice(0, 4));
                var argumentLength = BitConverter.ToInt32(serializedArguments.Slice(4, 4));
                var argument = serializedArguments.Slice(ProtobufHubProtocolConstants.ARGUMENT_HEADER_SIZE, argumentLength).ToArray();

                protobufObject.MergeFrom(argument);

                Assert.Equal(ARGUMENT_TYPE, argumentType);
                Assert.Equal(arguments[i++], protobufObject.Data);

                serializedArguments = serializedArguments.Slice(ProtobufHubProtocolConstants.ARGUMENT_HEADER_SIZE + argumentLength);
            }
        }

    }
}
