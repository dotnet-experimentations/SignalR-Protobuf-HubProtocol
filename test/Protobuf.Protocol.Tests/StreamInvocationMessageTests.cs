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
    public class StreamInvocationMessageTests
    {
        public const string TARGET = "Target";
        public const string INVOCATION_ID = "123";

        [Theory]
        [InlineData("1", "FooTarget")]
        [InlineData("1234", "StreamInvocationMessageTarget")]
        [InlineData("9876543210123456789", "TestStreamInvocationMessageHubProtocolTarget")]
        [InlineData("", "[[[[#####@@@@@@@$$$$$$$$%%%%%%%%%^^^^^^^&&&&&&&&********]]]]")]
        public void Protocol_Should_Handle_InvocationMessage_Without_Argument(string invocationId, string target)
        {
            var logger = NullLogger<ProtobufHubProtocol>.Instance;
            var binder = new Mock<IInvocationBinder>();
            var protobufType = Array.Empty<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();
            var streamInvocationMessage = new StreamInvocationMessage(invocationId, target, Array.Empty<object>());

            protobufHubProtocol.WriteMessage(streamInvocationMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultStreamInvocationMessage);

            Assert.True(result);
            Assert.NotNull(resultStreamInvocationMessage);
            Assert.IsType<StreamInvocationMessage>(resultStreamInvocationMessage);
            Assert.Equal(invocationId, ((StreamInvocationMessage)resultStreamInvocationMessage).InvocationId);
            Assert.Equal(target, ((StreamInvocationMessage)resultStreamInvocationMessage).Target);
            Assert.Empty(((StreamInvocationMessage)resultStreamInvocationMessage).Arguments);
        }

        [Theory]
        [InlineData(42)]
        [InlineData(2048, 4096)]
        [InlineData(123, 123456789, 987, 987654321)]
        [InlineData(-123, 123456789, 987, -987654321)]
        [InlineData(int.MaxValue, int.MinValue)]
        [InlineData(42.3)]
        [InlineData(2048.1234, 4096.45678)]
        [InlineData(123.00000, 123456789.12344556789, 987.3, 987654321.5)]
        [InlineData(-12.123453, 123456789.34554363, 987.9, -987654321)]
        [InlineData(double.MaxValue, double.MinValue)]
        [InlineData("Single Argument")]
        [InlineData("Foo", "Bar")]
        [InlineData("### First Argument ###", "[Second] [Argument]", "%%% Third %%% Argument %%%", "$Forth-$-Argument$")]
        [InlineData("")]
        public void Protocol_Should_Handle_StreamInvocationMessage_With_Int_Or_Double_Or_String_As_Argument(params object[] arguments)
        {
            var logger = NullLogger<ProtobufHubProtocol>.Instance;
            var binder = new Mock<IInvocationBinder>();
            var protobufType = Array.Empty<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();
            var streamInvocationMessage = new StreamInvocationMessage(INVOCATION_ID, TARGET, arguments);

            protobufHubProtocol.WriteMessage(streamInvocationMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultStreamInvocationMessage);

            Assert.True(result);
            Assert.NotNull(resultStreamInvocationMessage);
            Assert.IsType<StreamInvocationMessage>(resultStreamInvocationMessage);
            Assert.Equal(INVOCATION_ID, ((StreamInvocationMessage)resultStreamInvocationMessage).InvocationId);
            Assert.Equal(TARGET, ((StreamInvocationMessage)resultStreamInvocationMessage).Target);

            var args = ((StreamInvocationMessage)resultStreamInvocationMessage).Arguments;

            Assert.NotEmpty(args);
            Assert.Equal(arguments.Length, args.Length);

            for (var i = 0; i < args.Length; i++)
            {
                Assert.Equal(arguments[i], args[i]);
            }
        }

        [Theory]
        [InlineData("Single Argument")]
        [InlineData("Foo", "Bar")]
        [InlineData("### First Argument ###", "[Second] [Argument]", "%%% Third %%% Argument %%%", "$Forth-$-Argument$")]
        [InlineData("")]
        public void Protocol_Should_Handle_StreamInvocationMessage_With_ProtobufObject_As_Argument(params string[] data)
        {
            var logger = NullLogger<ProtobufHubProtocol>.Instance;
            var binder = new Mock<IInvocationBinder>();
            var protobufType = new List<Type> { typeof(TestMessage) };

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();

            var arguments = Helpers.GetProtobufTestMessages(data);
            var streamInvocationMessage = new StreamInvocationMessage(INVOCATION_ID, TARGET, arguments);

            protobufHubProtocol.WriteMessage(streamInvocationMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultStreamInvocationMessage);

            Assert.True(result);
            Assert.NotNull(resultStreamInvocationMessage);
            Assert.IsType<StreamInvocationMessage>(resultStreamInvocationMessage);
            Assert.Equal(INVOCATION_ID, ((StreamInvocationMessage)resultStreamInvocationMessage).InvocationId);
            Assert.Equal(TARGET, ((StreamInvocationMessage)resultStreamInvocationMessage).Target);

            var args = ((StreamInvocationMessage)resultStreamInvocationMessage).Arguments;

            Assert.NotEmpty(args);
            Assert.Equal(arguments.Length, args.Length);

            for (var i = 0; i < args.Length; i++)
            {
                Assert.Equal(arguments[i], args[i]);
            }
        }

        [Theory]
        [InlineData(42.3, "some data", 23)]
        [InlineData("test", 42.42, "Foo", "Bar")]
        [InlineData(123, "123456789.12344556789", 987.3543353, double.MaxValue)]
        [InlineData(-12.123453, -123456789, "some other data to test argument", "")]
        [InlineData(double.MaxValue, int.MinValue, int.MaxValue, double.MinValue, "")]
        public void Protocol_Should_Handle_StreamInvocationMessage_With_Arguments(params object[] arguments)
        {
            var logger = NullLogger<ProtobufHubProtocol>.Instance;
            var binder = new Mock<IInvocationBinder>();
            var protobufType = Array.Empty<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();
            var streamInvocationMessage = new StreamInvocationMessage(INVOCATION_ID, TARGET, arguments);

            protobufHubProtocol.WriteMessage(streamInvocationMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultStreamInvocationMessage);

            Assert.True(result);
            Assert.NotNull(resultStreamInvocationMessage);
            Assert.IsType<StreamInvocationMessage>(resultStreamInvocationMessage);
            Assert.Equal(INVOCATION_ID, ((StreamInvocationMessage)resultStreamInvocationMessage).InvocationId);
            Assert.Equal(TARGET, ((StreamInvocationMessage)resultStreamInvocationMessage).Target);

            var args = ((StreamInvocationMessage)resultStreamInvocationMessage).Arguments;

            Assert.NotEmpty(args);
            Assert.Equal(arguments.Length, args.Length);

            for (var i = 0; i < args.Length; i++)
            {
                Assert.Equal(arguments[i], args[i]);
            }
        }

        [Theory]
        [InlineData("1")]
        [InlineData("1234", "4321")]
        [InlineData("9876543210123456789", "12", "1", "999")]
        [InlineData("")]
        public void Protocol_Should_Handle_StreamInvocationMessage_With_StreamIds(params string[] streamIds)
        {
            var logger = NullLogger<ProtobufHubProtocol>.Instance;
            var binder = new Mock<IInvocationBinder>();
            var protobufType = Array.Empty<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();
            var streamInvocationMessage = new StreamInvocationMessage(INVOCATION_ID, TARGET, new[] { "foo", "bar" }, streamIds);

            protobufHubProtocol.WriteMessage(streamInvocationMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultStreamInvocationMessage);

            Assert.True(result);
            Assert.NotNull(resultStreamInvocationMessage);
            Assert.IsType<StreamInvocationMessage>(resultStreamInvocationMessage);
            Assert.Equal(TARGET, ((StreamInvocationMessage)resultStreamInvocationMessage).Target);
            Assert.Equal(INVOCATION_ID, ((StreamInvocationMessage)resultStreamInvocationMessage).InvocationId);
            Assert.NotEmpty(((StreamInvocationMessage)resultStreamInvocationMessage).Arguments);
            Assert.Equal("bar", ((StreamInvocationMessage)resultStreamInvocationMessage).Arguments[1]);

            var ids = ((StreamInvocationMessage)resultStreamInvocationMessage).StreamIds;
            Assert.NotEmpty(ids);
            Assert.Equal(streamIds.Length, ids.Length);

            for (var i = 0; i < ids.Length; i++)
            {
                Assert.Equal(streamIds[i], ids[i]);
            }
        }

        [Theory]
        [InlineData("key", "value")]
        [InlineData("foo", "bar", "2048", "4096")]
        [InlineData("toto", "tata", "tutu", "titi", "42", "28")]
        public void Protocol_Should_Handle_StreamInvocationMessage_With_Headers(params string[] kvp)
        {
            var logger = NullLogger<ProtobufHubProtocol>.Instance;
            var binder = new Mock<IInvocationBinder>();
            var protobufType = Array.Empty<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();

            var headers = Helpers.GetHeaders(kvp);
            var streamInvocationMessage = new StreamInvocationMessage(INVOCATION_ID, TARGET, new[] { "foo", "bar" })
            {
                Headers = headers
            };

            protobufHubProtocol.WriteMessage(streamInvocationMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultStreamInvocationMessage);

            Assert.True(result);
            Assert.NotNull(resultStreamInvocationMessage);
            Assert.IsType<StreamInvocationMessage>(resultStreamInvocationMessage);
            Assert.Equal(INVOCATION_ID, ((StreamInvocationMessage)resultStreamInvocationMessage).InvocationId);
            Assert.Equal(TARGET, ((StreamInvocationMessage)resultStreamInvocationMessage).Target);
            Assert.NotEmpty(((StreamInvocationMessage)resultStreamInvocationMessage).Arguments);
            Assert.Equal("bar", ((StreamInvocationMessage)resultStreamInvocationMessage).Arguments[1]);
            var resultHeaders = ((StreamInvocationMessage)resultStreamInvocationMessage).Headers;
            Assert.NotEmpty(resultHeaders);
            Assert.Equal(resultHeaders.Count, headers.Count);
            Assert.Equal(headers, resultHeaders);
        }
    }
}
