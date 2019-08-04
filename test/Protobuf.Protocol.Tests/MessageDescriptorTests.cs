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
            var totalLength = protobufMessageSerialized.Length + argumentsDescriptors.Sum(argument => argument.Argument.Length + ProtobufHubProtocolConstants.ARGUMENT_HEADER_LENGTH) + 4; //Int => // ProtobufMessageLength

            ReadOnlySpan<byte> messagePacked = messageDescriptor.PackMessage(messageType, protobufMessageSerialized, argumentsDescriptors);

            Assert.Equal(messageType, messagePacked[0]);
            Assert.Equal(totalLength, BitConverter.ToInt32(messagePacked.Slice(1, 4)));
            Assert.Equal(protobufMessageSerialized.Length, BitConverter.ToInt32(messagePacked.Slice(5, 4)));
            var protobufObject = new TestMessage();
            protobufObject.MergeFrom(messagePacked.Slice(ProtobufHubProtocolConstants.MESSAGE_HEADER_LENGTH, protobufMessageSerialized.Length).ToArray());
            Assert.Equal(protobufMessage, protobufObject);

            var serializedArguments = messagePacked.Slice(ProtobufHubProtocolConstants.MESSAGE_HEADER_LENGTH + protobufMessageSerialized.Length);

            var i = 0;
            while (!serializedArguments.IsEmpty)
            {
                var argumentType = BitConverter.ToInt32(serializedArguments.Slice(0, 4));
                var argumentLength = BitConverter.ToInt32(serializedArguments.Slice(4, 4));
                var argument = serializedArguments.Slice(ProtobufHubProtocolConstants.ARGUMENT_HEADER_LENGTH, argumentLength).ToArray();

                protobufObject.MergeFrom(argument);

                Assert.Equal(ARGUMENT_TYPE, argumentType);
                Assert.Equal(arguments[i++], protobufObject.Data);

                serializedArguments = serializedArguments.Slice(ProtobufHubProtocolConstants.ARGUMENT_HEADER_LENGTH + argumentLength);
            }
        }

        [Theory]
        [InlineData(HubProtocolConstants.InvocationMessageType)]
        [InlineData(HubProtocolConstants.StreamInvocationMessageType)]
        [InlineData(HubProtocolConstants.CompletionMessageType)]
        [InlineData(HubProtocolConstants.CloseMessageType)]
        public void MessageDescriptor_Should_Retrieve_MessageType_From_A_PackedMessage(int messageType)
        {
            var messageDescriptor = new MessageDescriptor();
            var protobufMessageSerialized = new TestMessage { Data = "FooBar" }.ToByteArray();
            var argumentsDescriptors = GetArgumentsDescriptors("myArg");

            ReadOnlySpan<byte> messagePacked = messageDescriptor.PackMessage(messageType, protobufMessageSerialized, argumentsDescriptors);

            var type = messageDescriptor.GetMessageType(messagePacked);

            Assert.Equal(messageType, type);
        }

        [Fact]
        public void MessageDescriptor_Should_Retrieve_A_Zero_TotalLength_When_PackedMessage_DoesNot_Have_Enought_Length()
        {
            var messagePacked = new byte[] { 1, 2, 3 };
            var messageDescriptor = new MessageDescriptor();

            var length = messageDescriptor.GetTotalMessageLength(messagePacked);

            Assert.Equal(0, length);
        }

        [Theory]
        [InlineData(22, "Foo", "Bar")]
        [InlineData(34, "Foo", "Bar", "42")]
        [InlineData(120, "test with a bigger length", "some first parameter", "with a second parameter", "and even a third")]
        [InlineData(12, "", "")]
        public void MessageDescriptor_Should_Retrieve_TotalLength_From_A_PackedMessage(int totalLength, string data, params string[] arguments)
        {
            var messageDescriptor = new MessageDescriptor();
            var protobufMessageSerialized = new TestMessage { Data = data }.ToByteArray();
            var argumentsDescriptors = GetArgumentsDescriptors(arguments);

            ReadOnlySpan<byte> messagePacked = messageDescriptor.PackMessage(1, protobufMessageSerialized, argumentsDescriptors);

            var length = messageDescriptor.GetTotalMessageLength(messagePacked);

            Assert.Equal(totalLength, length);
        }

        [Fact]
        public void MessageDescriptor_Should_Not_Retrieve_ProtobufMessage_When_PackedMessage_DoesNot_Have_A_Full_Header()
        {
            var messagePacked = new byte[] { 1, 2, 3 };
            var messageDescriptor = new MessageDescriptor();

            var protobufMessage = messageDescriptor.GetProtobufMessage(messagePacked);

            Assert.Equal(0, protobufMessage.Length);
        }

        [Theory]
        [InlineData("FooBar")]
        [InlineData("Some test message")]
        [InlineData("A' |message| with {some} %%%% special &&&& ^^^ ##$$$$$$$$$$$ char @@!!]]//>><<")]
        [InlineData("")]
        public void MessageDescriptor_Should_Retrieve_ProtobufMessage_From_A_PackedMessage(string data)
        {
            var messageDescriptor = new MessageDescriptor();
            var protobufMessageSerialized = new TestMessage { Data = data }.ToByteArray();
            var argumentsDescriptors = GetArgumentsDescriptors("arg");

            ReadOnlySpan<byte> messagePacked = messageDescriptor.PackMessage(1, protobufMessageSerialized, argumentsDescriptors);

            var protobufMessage = messageDescriptor.GetProtobufMessage(messagePacked);
            var protobufObject = new TestMessage();
            protobufObject.MergeFrom(protobufMessage.ToArray());

            Assert.Equal(data, protobufObject.Data);
        }

        [Fact]
        public void MessageDescriptor_Should_Not_Retrieve_ArgumentsDescriptors_When_PackedMessage_DoesNot_Have_A_Full_Header()
        {
            var messagePacked = new byte[] { 1, 2, 3, 4, 5, 6 };
            var messageDescriptor = new MessageDescriptor();

            var argumentDescriptors = messageDescriptor.GetArguments(messagePacked);

            Assert.Empty(argumentDescriptors);
        }

        [Theory]
        [InlineData("Only one argument")]
        [InlineData("Foo", "bar")]
        [InlineData("@@@Arg1@@@", "######Arg2######", "{{{{{{{{{{{{{{{{{{{{{{{{{{{{{{Args3}}}}}}}}}}}}}}}}}}}}}}}}", "???????????*********************!!!!!!!!!!!!!!!!!")]
        [InlineData("", "#####", "")]
        [InlineData("")]
        public void MessageDescriptor_Should_Retrieve_Arguments_From_A_PackedMessage(params string[] arguments)
        {
            var messageDescriptor = new MessageDescriptor();
            var protobufMessageSerialized = new TestMessage { Data = "FooBar" }.ToByteArray();
            var argumentsDescriptors = GetArgumentsDescriptors(arguments);

            ReadOnlySpan<byte> messagePacked = messageDescriptor.PackMessage(1, protobufMessageSerialized, argumentsDescriptors);

            var descriptors = messageDescriptor.GetArguments(messagePacked);

            var i = 0;
            foreach (var descriptor in descriptors)
            {
                var protobufObject = new TestMessage();

                protobufObject.MergeFrom(descriptor.Argument);

                Assert.Equal(ARGUMENT_TYPE, descriptor.Type);
                Assert.Equal(arguments[i++], protobufObject.Data);
            }
        }
    }
}
