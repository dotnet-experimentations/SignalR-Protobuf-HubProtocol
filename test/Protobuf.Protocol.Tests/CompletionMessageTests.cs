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
    public class CompletionMessageTests
    {
        [Theory]
        [InlineData("3")]
        [InlineData("123")]
        [InlineData("9876543210123456789")]
        [InlineData("##############!!!!!!!!!!!$$$$$$$$$$$$$$^^^^^^^^^^^^^^^***********")]
        public void Protocol_Should_Handle_CompletionMessage_Without_Result_Or_Error(string invocationId)
        {
            var logger = new NullLogger<ProtobufHubProtocol>();
            var binder = new Mock<IInvocationBinder>();
            var protobufType = new List<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();
            var completionMessage = new CompletionMessage(invocationId, null, null, true);

            protobufHubProtocol.WriteMessage(completionMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultCompletionMessage);

            Assert.True(result);
            Assert.NotNull(resultCompletionMessage);
            Assert.IsType<CompletionMessage>(resultCompletionMessage);
            Assert.Equal(invocationId, ((CompletionMessage)resultCompletionMessage).InvocationId);
            Assert.False(((CompletionMessage)resultCompletionMessage).HasResult, "Completation message does have result");
        }

        [Theory]
        [InlineData("Some Error")]
        [InlineData("Some bad stuff happened")]
        [InlineData("Grrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr")]
        [InlineData("##############!!!!!!!!!!!$$$$$$$$$$$$$$^^^^^^^^^^^^^^^***********")]
        public void Protocol_Should_Handle_CompletionMessage_With_An_Error(string error)
        {
            var logger = new NullLogger<ProtobufHubProtocol>();
            var binder = new Mock<IInvocationBinder>();
            var protobufType = new List<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();
            var completionMessage = new CompletionMessage("123", error, null, false);

            protobufHubProtocol.WriteMessage(completionMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultCompletionMessage);
            Assert.True(result);
            Assert.NotNull(resultCompletionMessage);
            Assert.IsType<CompletionMessage>(resultCompletionMessage);
            Assert.Equal("123", ((CompletionMessage)resultCompletionMessage).InvocationId);
            Assert.Equal(error, ((CompletionMessage)resultCompletionMessage).Error);
            Assert.Null(((CompletionMessage)resultCompletionMessage).Result);
            Assert.False(((CompletionMessage)resultCompletionMessage).HasResult, "Completation message does have result");
        }

        [Theory]
        [InlineData("Some Result")]
        [InlineData("A super result is here")]
        [InlineData("YAIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII")]
        [InlineData("##############!!!!!!!!!!!$$$$$$$$$$$$$$^^^^^^^^^^^^^^^***********")]
        public void Protocol_Should_Handle_CompletionMessage_With_A_Result(string completionResult)
        {
            var logger = new NullLogger<ProtobufHubProtocol>();
            var binder = new Mock<IInvocationBinder>();
            var protobufType = new List<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();
            var completionMessage = new CompletionMessage("123", null, completionResult, true);

            protobufHubProtocol.WriteMessage(completionMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultCompletionMessage);
            Assert.True(result);
            Assert.NotNull(resultCompletionMessage);
            Assert.IsType<CompletionMessage>(resultCompletionMessage);
            Assert.Equal("123", ((CompletionMessage)resultCompletionMessage).InvocationId);
            Assert.Equal(completionResult, ((CompletionMessage)resultCompletionMessage).Result);
            Assert.True(((CompletionMessage)resultCompletionMessage).HasResult, "Completation message doesn't have result");
        }

        [Theory]
        [InlineData("key", "value")]
        [InlineData("foo", "bar", "2048", "4096")]
        [InlineData("toto", "tata", "tutu", "titi", "42", "28")]
        public void Protocol_Should_Handle_CompletionMessage_With_Headers_And_Result(params string[] kvp)
        {
            var logger = new NullLogger<ProtobufHubProtocol>();
            var binder = new Mock<IInvocationBinder>();
            var protobufType = new List<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();

            var headers = Helpers.GetHeaders(kvp);
            var completionMessage = new CompletionMessage("123", null, "completion", true)
            {
                Headers = headers
            };

            protobufHubProtocol.WriteMessage(completionMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultCompletionMessage);

            Assert.True(result);
            Assert.NotNull(resultCompletionMessage);
            Assert.IsType<CompletionMessage>(resultCompletionMessage);
            Assert.Equal("123", ((CompletionMessage)resultCompletionMessage).InvocationId);
            Assert.Equal("completion", ((CompletionMessage)resultCompletionMessage).Result);
            Assert.True(((CompletionMessage)resultCompletionMessage).HasResult, "Completation message doesn't have result");
            var resultHeaders = ((CompletionMessage)resultCompletionMessage).Headers;
            Assert.NotEmpty(resultHeaders);
            Assert.Equal(resultHeaders.Count, headers.Count);
            Assert.Equal(headers, resultHeaders);
        }

        [Theory]
        [InlineData("key", "value")]
        [InlineData("foo", "bar", "2048", "4096")]
        [InlineData("toto", "tata", "tutu", "titi", "42", "28")]
        public void Protocol_Should_Handle_CompletionMessage_With_Headers_And_Error(params string[] kvp)
        {
            var logger = new NullLogger<ProtobufHubProtocol>();
            var binder = new Mock<IInvocationBinder>();
            var protobufType = new List<Type>();

            var protobufHubProtocol = new ProtobufHubProtocol(protobufType, logger);
            var writer = new ArrayBufferWriter<byte>();

            var headers = Helpers.GetHeaders(kvp);
            var completionMessage = new CompletionMessage("123", "Error", null, false)
            {
                Headers = headers
            };

            protobufHubProtocol.WriteMessage(completionMessage, writer);
            var encodedMessage = new ReadOnlySequence<byte>(writer.WrittenSpan.ToArray());
            var result = protobufHubProtocol.TryParseMessage(ref encodedMessage, binder.Object, out var resultCompletionMessage);

            Assert.True(result);
            Assert.NotNull(resultCompletionMessage);
            Assert.IsType<CompletionMessage>(resultCompletionMessage);
            Assert.Equal("123", ((CompletionMessage)resultCompletionMessage).InvocationId);
            Assert.Equal("Error", ((CompletionMessage)resultCompletionMessage).Error);
            Assert.Null(((CompletionMessage)resultCompletionMessage).Result);
            Assert.False(((CompletionMessage)resultCompletionMessage).HasResult, "Completation message does have result");
            var resultHeaders = ((CompletionMessage)resultCompletionMessage).Headers;
            Assert.NotEmpty(resultHeaders);
            Assert.Equal(resultHeaders.Count, headers.Count);
            Assert.Equal(headers, resultHeaders);
        }
    }
}
