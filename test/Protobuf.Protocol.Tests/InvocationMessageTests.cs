using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SignalR.Protobuf.Protocol;
using Protobuf.Protocol.Tests.Helper;
using System;
using System.Buffers;
using System.Collections.Generic;
using Xunit;

namespace Protobuf.Protocol.Tests
{
    public class InvocationMessageTests
    {
        public const string TARGET = "Target";
        public const string INVOCATION_ID = "123";

        public object[] GetProtobufTestMessages(params string[] data)
        {
            var objects = new List<object>();

            for (var i = 0; i < data.Length; i++)
            {
                objects.Add(new TestMessage { Data = data[i] });
            }

            return objects.ToArray();
        }

        [Theory]
        [InlineData("FooTarget")]
        [InlineData("InvocationMessageTarget")]
        [InlineData("TestInvocationMessageHubProtocolTarget")]
        public void Protocol_Should_Handle_InvocationMessage_Without_Argument(string target)
        {
            var logger = new NullLogger<ProtobufHubProtocol>();
            var binder = new Mock<IInvocationBinder>();
            var protobufType = new List<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();
            var invocationMessage = new InvocationMessage(target, Array.Empty<object>());

            protobufHubProtocol.WriteMessage(invocationMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultInvocationMessage);

            Assert.True(result);
            Assert.NotNull(resultInvocationMessage);
            Assert.IsType<InvocationMessage>(resultInvocationMessage);
            Assert.Equal(target, ((InvocationMessage)resultInvocationMessage).Target);
            Assert.Empty(((InvocationMessage)resultInvocationMessage).Arguments);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("1234")]
        [InlineData("9876543210123456789")]
        [InlineData("")]
        public void Protocol_Should_Handle_InvocationMessage_With_InvocationId_And_No_Argument(string invocationId)
        {
            var logger = new NullLogger<ProtobufHubProtocol>();
            var binder = new Mock<IInvocationBinder>();
            var protobufType = new List<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();
            var invocationMessage = new InvocationMessage(invocationId, TARGET, Array.Empty<object>());

            protobufHubProtocol.WriteMessage(invocationMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultInvocationMessage);

            Assert.True(result);
            Assert.NotNull(resultInvocationMessage);
            Assert.IsType<InvocationMessage>(resultInvocationMessage);
            Assert.Equal(TARGET, ((InvocationMessage)resultInvocationMessage).Target);
            Assert.Equal(invocationId, ((InvocationMessage)resultInvocationMessage).InvocationId);
            Assert.Empty(((InvocationMessage)resultInvocationMessage).Arguments);
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
        public void Protocol_Should_Handle_InvocationMessage_With_Int_Or_Double_Or_String_As_Argument(params object[] arguments)
        {
            var logger = new NullLogger<ProtobufHubProtocol>();
            var binder = new Mock<IInvocationBinder>();
            var protobufType = new List<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();
            var invocationMessage = new InvocationMessage(TARGET, arguments);

            protobufHubProtocol.WriteMessage(invocationMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultInvocationMessage);

            Assert.True(result);
            Assert.NotNull(resultInvocationMessage);
            Assert.IsType<InvocationMessage>(resultInvocationMessage);
            Assert.Equal(TARGET, ((InvocationMessage)resultInvocationMessage).Target);

            var args = ((InvocationMessage)resultInvocationMessage).Arguments;

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
        public void Protocol_Should_Handle_InvocationMessage_With_ProtobufObject_As_Argument(params string[] data)
        {
            var logger = new NullLogger<ProtobufHubProtocol>();
            var binder = new Mock<IInvocationBinder>();
            var protobufType = new List<Type> { typeof(TestMessage) };

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();

            var arguments = GetProtobufTestMessages(data);
            var invocationMessage = new InvocationMessage(TARGET, arguments);

            protobufHubProtocol.WriteMessage(invocationMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultInvocationMessage);

            Assert.True(result);
            Assert.NotNull(resultInvocationMessage);
            Assert.IsType<InvocationMessage>(resultInvocationMessage);
            Assert.Equal(TARGET, ((InvocationMessage)resultInvocationMessage).Target);

            var args = ((InvocationMessage)resultInvocationMessage).Arguments;

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
        public void Protocol_Should_Handle_InvocationMessage_With_Arguments(params object[] arguments)
        {
            var logger = new NullLogger<ProtobufHubProtocol>();
            var binder = new Mock<IInvocationBinder>();
            var protobufType = new List<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();
            var invocationMessage = new InvocationMessage(TARGET, arguments);

            protobufHubProtocol.WriteMessage(invocationMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultInvocationMessage);

            Assert.True(result);
            Assert.NotNull(resultInvocationMessage);
            Assert.IsType<InvocationMessage>(resultInvocationMessage);
            Assert.Equal(TARGET, ((InvocationMessage)resultInvocationMessage).Target);

            var args = ((InvocationMessage)resultInvocationMessage).Arguments;

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
        public void Protocol_Should_Handle_InvocationMessage_With_StreamIds(params string[] streamIds)
        {
            var logger = new NullLogger<ProtobufHubProtocol>();
            var binder = new Mock<IInvocationBinder>();
            var protobufType = new List<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();
            var invocationMessage = new InvocationMessage("1", TARGET, new[] { "foo", "bar"}, streamIds);

            protobufHubProtocol.WriteMessage(invocationMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultInvocationMessage);

            Assert.True(result);
            Assert.NotNull(resultInvocationMessage);
            Assert.IsType<InvocationMessage>(resultInvocationMessage);
            Assert.Equal(TARGET, ((InvocationMessage)resultInvocationMessage).Target);
            Assert.Equal("1", ((InvocationMessage)resultInvocationMessage).InvocationId);
            Assert.NotEmpty(((InvocationMessage)resultInvocationMessage).Arguments);
            Assert.Equal("bar", ((InvocationMessage)resultInvocationMessage).Arguments[1]);

            var ids = ((InvocationMessage)resultInvocationMessage).StreamIds;
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
        public void Protocol_Should_Handle_InvocationMessage_With_Headers(params string[] kvp)
        {
            var logger = new NullLogger<ProtobufHubProtocol>();
            var binder = new Mock<IInvocationBinder>();
            var protobufType = new List<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();

            var headers = Helpers.GetHeaders(kvp);
            var invocationMessage = new InvocationMessage(TARGET, new[] { "foo", "bar" })
            {
                Headers = headers
            };

            protobufHubProtocol.WriteMessage(invocationMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultInvocationMessage);

            Assert.True(result);
            Assert.NotNull(resultInvocationMessage);
            Assert.IsType<InvocationMessage>(resultInvocationMessage);
            Assert.Equal(TARGET, ((InvocationMessage)resultInvocationMessage).Target);
            Assert.NotEmpty(((InvocationMessage)resultInvocationMessage).Arguments);
            Assert.Equal("bar", ((InvocationMessage)resultInvocationMessage).Arguments[1]);
            var resultHeaders = ((InvocationMessage)resultInvocationMessage).Headers;
            Assert.NotEmpty(resultHeaders);
            Assert.Equal(resultHeaders.Count, headers.Count);
            Assert.Equal(headers, resultHeaders);
        }
    }
}
