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
    public class PingInvocationMessageTests
    {
        [Fact]
        public void Protocol_Should_Handle_PingMessage()
        {
            var logger = NullLogger<ProtobufHubProtocol>.Instance;
            var binder = new Mock<IInvocationBinder>();
            var protobufType = Array.Empty<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();

            protobufHubProtocol.WriteMessage(PingMessage.Instance, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultPingMessage);

            Assert.Equal(ProtobufHubProtocolConstants.MESSAGE_HEADER_LENGTH, writer.WrittenCount);
            Assert.True(result);
            Assert.IsType<PingMessage>(resultPingMessage);
            Assert.NotNull(resultPingMessage);
            Assert.Equal(PingMessage.Instance, resultPingMessage);
        }
    }
}