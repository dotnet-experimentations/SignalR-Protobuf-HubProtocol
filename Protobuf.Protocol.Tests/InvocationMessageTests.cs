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
    public class InvocationMessageTests
    {
        [Theory]
        [InlineData("FooTarget")]
        [InlineData("InvocationMessageTarget")]
        [InlineData("TestInvocationMessageHubProtocolTarget")]
        public void Protocol_Should_Handle_InvocationMessage_Without_Argument(string target)
        {
            var logger = new NullLogger<ProtobufHubProtocol>();
            var binder = new Mock<IInvocationBinder>();

            var protobufHubProtocol = new ProtobufHubProtocol(logger);
            var writer = new ArrayBufferWriter<byte>();
            var invocationMessage = new InvocationMessage(target, Array.Empty<object>());

            protobufHubProtocol.WriteMessage(invocationMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultInvocationMessage);

            Assert.True(result);
            Assert.NotNull(resultInvocationMessage);
            Assert.IsType<InvocationMessage>(resultInvocationMessage);
            Assert.Equal(target, ((InvocationMessage)resultInvocationMessage).Target);
        }
    }
}
