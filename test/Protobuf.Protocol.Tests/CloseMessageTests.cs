using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Protobuf.Protocol.Tests
{
    public class CloseMessageTests
    {
        [Fact]
        public void Protocol_Should_Handle_CancelInvocationMessage_Without_Error()
        {
            var logger = NullLogger<ProtobufHubProtocol>.Instance;
            var binder = new Mock<IInvocationBinder>();
            var protobufType = Array.Empty<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();
            var closeMessage = new CloseMessage(null);

            protobufHubProtocol.WriteMessage(closeMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultCloseMessage);

            Assert.True(result);
            Assert.NotNull(resultCloseMessage);
            Assert.IsType<CloseMessage>(resultCloseMessage);
            Assert.Null(((CloseMessage)resultCloseMessage).Error);
        }

        [Theory]
        [InlineData("Some Error")]
        [InlineData("Some bad stuff happened")]
        [InlineData("Grrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr")]
        [InlineData("##############!!!!!!!!!!!$$$$$$$$$$$$$$^^^^^^^^^^^^^^^***********")]
        public void Protocol_Should_Handle_CancelInvocationMessage_Without_Header(string error)
        {
            var logger = NullLogger<ProtobufHubProtocol>.Instance;
            var binder = new Mock<IInvocationBinder>();
            var protobufType = Array.Empty<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();
            var closeMessage = new CloseMessage(error);

            protobufHubProtocol.WriteMessage(closeMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultCloseMessage);

            Assert.True(result);
            Assert.NotNull(resultCloseMessage);
            Assert.IsType<CloseMessage>(resultCloseMessage);
            Assert.Equal(error, ((CloseMessage)resultCloseMessage).Error);
        }
    }
}
