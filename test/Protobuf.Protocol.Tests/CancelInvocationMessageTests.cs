using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Protobuf.Protocol.Tests.Helper;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Protobuf.Protocol.Tests
{
    public class CancelInvocationMessageTests
    {
        [Theory]
        [InlineData("1")]
        [InlineData("1234")]
        [InlineData("9876543210123456789")]
        [InlineData("")]
        public void Protocol_Should_Handle_CancelInvocationMessage_Without_Header(string invocationId)
        {
            var logger = NullLogger<ProtobufHubProtocol>.Instance;
            var binder = new Mock<IInvocationBinder>();
            var protobufType = Array.Empty<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();
            var cancelInvocationMessage = new CancelInvocationMessage(invocationId);

            protobufHubProtocol.WriteMessage(cancelInvocationMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultCancelInvocationMessage);

            Assert.True(result);
            Assert.NotNull(resultCancelInvocationMessage);
            Assert.IsType<CancelInvocationMessage>(resultCancelInvocationMessage);
            Assert.Equal(invocationId, ((CancelInvocationMessage)resultCancelInvocationMessage).InvocationId);
        }

        [Theory]
        [InlineData("key", "value")]
        [InlineData("foo", "bar", "2048", "4096")]
        [InlineData("toto", "tata", "tutu", "titi", "42", "28")]
        public void Protocol_Should_Handle_CancelInvocationMessage_With_Header(params string[] kvp)
        {
            var logger = NullLogger<ProtobufHubProtocol>.Instance;
            var binder = new Mock<IInvocationBinder>();
            var protobufType = Array.Empty<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();

            var headers = Helpers.GetHeaders(kvp);
            var cancelInvocationMessage = new CancelInvocationMessage("123")
            {
                Headers = headers
            };

            protobufHubProtocol.WriteMessage(cancelInvocationMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultCancelInvocationMessage);

            Assert.True(result);
            Assert.NotNull(resultCancelInvocationMessage);
            Assert.IsType<CancelInvocationMessage>(resultCancelInvocationMessage);
            Assert.Equal("123", ((CancelInvocationMessage)resultCancelInvocationMessage).InvocationId);
            var resultHeaders = ((CancelInvocationMessage)resultCancelInvocationMessage).Headers;
            Assert.NotEmpty(resultHeaders);
            Assert.Equal(resultHeaders.Count, headers.Count);
            Assert.Equal(headers, resultHeaders);
        }
    }
}
