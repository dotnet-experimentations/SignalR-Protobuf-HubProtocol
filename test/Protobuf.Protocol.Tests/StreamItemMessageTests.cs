using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Protobuf.Protocol.Tests.Helper;
using SignalR.Protobuf.Protocol;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Protobuf.Protocol.Tests
{
    public class StreamItemMessageTests
    {
        public const string INVOCATION_ID = "123";

        [Theory]
        [InlineData("simple string item")]
        [InlineData("##$$%%@@**(&_}]{)&%$")]
        [InlineData("")]
        [InlineData(42)]
        [InlineData(-42)]
        [InlineData(0)]
        [InlineData(int.MaxValue)]
        [InlineData(4096.1234)]
        [InlineData(-6789.90876509)]
        [InlineData(double.MinValue)]

        public void Protocol_Should_Handle_StreamItemMessage_Without_Header(object item)
        {
            var logger = NullLogger<ProtobufHubProtocol>.Instance;
            var binder = new Mock<IInvocationBinder>();
            var protobufType = Array.Empty<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();
            var streamItemMessage = new StreamItemMessage(INVOCATION_ID, item);

            protobufHubProtocol.WriteMessage(streamItemMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultStreamItemMessage);

            Assert.True(result);
            Assert.NotNull(resultStreamItemMessage);
            Assert.IsType<StreamItemMessage>(resultStreamItemMessage);
            Assert.Equal(INVOCATION_ID, ((StreamItemMessage)resultStreamItemMessage).InvocationId);
            Assert.Equal(item, ((StreamItemMessage)resultStreamItemMessage).Item);
        }

        [Theory]
        [InlineData("simple string item")]
        [InlineData("##$$%%@@**(&_}]{)&%$")]
        [InlineData("##$$%%@@**(&_}]{)&%$qwertyuiop")]
        [InlineData("")]
        public void Protocol_Should_Handle_StreamItemMessage_With_ProtobufObject_Item_And_No_Header(string data)
        {
            var logger = NullLogger<ProtobufHubProtocol>.Instance;
            var binder = new Mock<IInvocationBinder>();
            var protobufType = new List<Type> { typeof(TestMessage) };

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();
            var item = new TestMessage { Data = data };
            var streamItemMessage = new StreamItemMessage(INVOCATION_ID, item);

            protobufHubProtocol.WriteMessage(streamItemMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultStreamItemMessage);

            Assert.True(result);
            Assert.NotNull(resultStreamItemMessage);
            Assert.IsType<StreamItemMessage>(resultStreamItemMessage);
            Assert.Equal(INVOCATION_ID, ((StreamItemMessage)resultStreamItemMessage).InvocationId);
            Assert.Equal(item, ((StreamItemMessage)resultStreamItemMessage).Item);
        }

        [Theory]
        [InlineData("key", "value")]
        [InlineData("foo", "bar", "2048", "4096")]
        [InlineData("toto", "tata", "tutu", "titi", "42", "28")]
        public void Protocol_Should_Handle_StreamItemMessage_With_Headers(params string[] kvp)
        {
            var logger = NullLogger<ProtobufHubProtocol>.Instance;
            var binder = new Mock<IInvocationBinder>();
            var protobufType = Array.Empty<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();

            var headers = Helpers.GetHeaders(kvp);
            var streamItemMessage = new StreamItemMessage(INVOCATION_ID, "foo")
            {
                Headers = headers
            };

            protobufHubProtocol.WriteMessage(streamItemMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultStreamItemMessage);

            Assert.True(result);
            Assert.NotNull(resultStreamItemMessage);
            Assert.IsType<StreamItemMessage>(resultStreamItemMessage);
            Assert.Equal(INVOCATION_ID, ((StreamItemMessage)resultStreamItemMessage).InvocationId);
            Assert.Equal("foo", ((StreamItemMessage)resultStreamItemMessage).Item);

            var resultHeaders = ((StreamItemMessage)resultStreamItemMessage).Headers;
            Assert.NotEmpty(resultHeaders);
            Assert.Equal(resultHeaders.Count, headers.Count);
            Assert.Equal(headers, resultHeaders);
        }
    }
}
