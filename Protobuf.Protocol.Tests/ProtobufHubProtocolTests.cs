using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Buffers;
using Protobuf.Protocol;
using Xunit;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Protobuf.Protocol.Tests
{
    public class ProtobufHubProtocolTests
    {
        public HubMessage GetHubMessageFromType(int messageType)
        {
            switch (messageType)
            {
                case HubProtocolConstants.InvocationMessageType:
                    return new InvocationMessage("MyTest", new object[] { "foo", "bar" });
                case HubProtocolConstants.StreamItemMessageType:
                    return new StreamItemMessage("1", "foo");
                case HubProtocolConstants.CompletionMessageType:
                    return new CompletionMessage("1", null, "bar", true);
                case HubProtocolConstants.StreamInvocationMessageType:
                    return new StreamInvocationMessage("1", "MyTest", new object[] { "foo", "bar" });
                case HubProtocolConstants.CancelInvocationMessageType:
                    return new CancelInvocationMessage("1");
                case HubProtocolConstants.PingMessageType:
                    return PingMessage.Instance;
                case HubProtocolConstants.CloseMessageType:
                    return new CloseMessage("Some Error");
                default:
                    return null;
            }
        }

        [Theory]
        [InlineData(HubProtocolConstants.InvocationMessageType)]
        [InlineData(HubProtocolConstants.StreamItemMessageType)]
        [InlineData(HubProtocolConstants.CompletionMessageType)]
        [InlineData(HubProtocolConstants.StreamInvocationMessageType)]
        [InlineData(HubProtocolConstants.CancelInvocationMessageType)]
        [InlineData(HubProtocolConstants.PingMessageType)]
        [InlineData(HubProtocolConstants.CloseMessageType)]
        public void Protocol_Should_Write_Message_Type_At_First_Byte(int messageType)
        {
            var logger = new NullLogger<ProtobufHubProtocol>();

            var protobufHubProtocol = new ProtobufHubProtocol(logger);
            var hubMessage = GetHubMessageFromType(messageType);
            var writer = new ArrayBufferWriter<byte>();

            protobufHubProtocol.WriteMessage(hubMessage, writer);
            var encodedMessage = writer.WrittenSpan;

            Assert.True(encodedMessage.Length > 0, "At least the message type is written");
            Assert.Equal(messageType, encodedMessage[0]);
        }

        [Theory]
        [InlineData(HubProtocolConstants.InvocationMessageType)]
        [InlineData(HubProtocolConstants.StreamItemMessageType)]
        [InlineData(HubProtocolConstants.CompletionMessageType)]
        [InlineData(HubProtocolConstants.StreamInvocationMessageType)]
        [InlineData(HubProtocolConstants.CancelInvocationMessageType)]
        [InlineData(HubProtocolConstants.PingMessageType)]
        [InlineData(HubProtocolConstants.CloseMessageType)]
        // The total size of the message is written on 4 bytes after the message type
        public void Protocol_Should_Write_Message_Size(int messageType)
        {
            var logger = new NullLogger<ProtobufHubProtocol>();

            var protobufHubProtocol = new ProtobufHubProtocol(logger);
            var hubMessage = GetHubMessageFromType(messageType);
            var writer = new ArrayBufferWriter<byte>();

            protobufHubProtocol.WriteMessage(hubMessage, writer);
            var encodedMessage = writer.WrittenSpan;

            Assert.True(encodedMessage.Length > 4, "The message size is written");
            var totalSize = BitConverter.ToInt32(encodedMessage.Slice(1, 4));
            Assert.Equal(writer.WrittenCount - ProtobufHubProtocolConstants.TYPE_AND_TOTAL_LENGTH_HEADER, totalSize);
        }

        [Theory]
        [InlineData(HubProtocolConstants.InvocationMessageType)]
        [InlineData(HubProtocolConstants.StreamItemMessageType)]
        [InlineData(HubProtocolConstants.CompletionMessageType)]
        [InlineData(HubProtocolConstants.StreamInvocationMessageType)]
        [InlineData(HubProtocolConstants.CancelInvocationMessageType)]
        [InlineData(HubProtocolConstants.PingMessageType)]
        [InlineData(HubProtocolConstants.CloseMessageType)]
        // The protobuf message size is written on 4 bytes after the message size
        // It's used to know how many bytes are needed to deserialized the object
        public void Protocol_Should_Write_Protobuf_Message_Size(int messageType)
        {
            var logger = new NullLogger<ProtobufHubProtocol>();

            var protobufHubProtocol = new ProtobufHubProtocol(logger);
            var hubMessage = GetHubMessageFromType(messageType);
            var writer = new ArrayBufferWriter<byte>();

            protobufHubProtocol.WriteMessage(hubMessage, writer);
            var encodedMessage = writer.WrittenSpan;

            Assert.True(encodedMessage.Length >= ProtobufHubProtocolConstants.MESSAGE_HEADER_LENGTH, "The protobuf message size is written");
        }

        [Fact]
        public void Protocol_Should_Not_Parse_Message_If_Less_Than_Header_Size()
        {
            var logger = new NullLogger<ProtobufHubProtocol>();
            var binder = new Mock<IInvocationBinder>();

            var protobufHubProtocol = new ProtobufHubProtocol(logger);

            var encodedMessage = new ReadOnlySequence<byte>(new byte[] { 6, 0, 0, 0});
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var pingMessage);

            Assert.False(result);
            Assert.Null(pingMessage);
        }
    }
}
