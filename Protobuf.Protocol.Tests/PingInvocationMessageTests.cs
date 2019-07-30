﻿using Microsoft.AspNetCore.SignalR;
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
            var logger = new NullLogger<ProtobufHubProtocol>();
            var binder = new Mock<IInvocationBinder>();

            var protobufHubProtocol = new ProtobufHubProtocol(logger);
            var writer = new ArrayBufferWriter<byte>();

            protobufHubProtocol.WriteMessage(PingMessage.Instance, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var pingMessage);

            Assert.Equal(ProtobufHubProtocolConstants.HEADER_SIZE, writer.WrittenCount);
            Assert.True(result);
            Assert.IsType<PingMessage>(pingMessage);
            Assert.NotNull(pingMessage);
            Assert.Equal(PingMessage.Instance, pingMessage);
        }
    }
}