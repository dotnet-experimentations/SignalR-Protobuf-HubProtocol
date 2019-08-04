using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Google.Protobuf;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using SignalR.Protobuf.Protocol;

namespace Protobuf.Protocol
{
    public class ProtobufHubProtocol : IHubProtocol
    {
        private static readonly string _protocolName = "protobuf";
        private static readonly int _protocolVersion = 1;

        private readonly ILogger<ProtobufHubProtocol> _logger;
        private readonly MessageDescriptor _messageDescriptor;

        public string Name => _protocolName;

        public int Version => _protocolVersion;

        public TransferFormat TransferFormat => TransferFormat.Binary;

        public ProtobufHubProtocol(ILogger<ProtobufHubProtocol> logger)
        {
            _logger = logger;
            _messageDescriptor = new MessageDescriptor();
        }

        public bool IsVersionSupported(int version)
        {
            return version == Version;
        }

        public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
        {
            return HubProtocolExtensions.GetMessageBytes(this, message);
        }

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
        {
            if (input.Length < ProtobufHubProtocolConstants.MESSAGE_HEADER_LENGTH)
            {
                message = null;
                return false;
            }


            var totalSize = BitConverter.ToInt32(input.Slice(1, 4).ToArray(), 0);

            var serializedMessage = input.Slice(0, totalSize + ProtobufHubProtocolConstants.TYPE_AND_TOTAL_LENGTH_HEADER).ToArray();

            var messageType = _messageDescriptor.GetMessageType(serializedMessage);

            message = CreateHubMessage(serializedMessage, messageType);

            input = input.Slice(totalSize + ProtobufHubProtocolConstants.TYPE_AND_TOTAL_LENGTH_HEADER);

            return true;
        }

        private HubMessage CreateHubMessage(ReadOnlySpan<byte> serializedMessage, int messageType)
        {
            switch (messageType)
            {
                case HubProtocolConstants.InvocationMessageType:
                    return CreateHubInvocationMessage(serializedMessage);
                case HubProtocolConstants.PingMessageType:
                    return PingMessage.Instance;
                default:
                    return null;
            }
        }

        private HubMessage CreateHubInvocationMessage(ReadOnlySpan<byte> serializedMessage)
        {
            var protobufMessage = _messageDescriptor.GetProtobufMessage(serializedMessage);

            var argumentsDescriptors = _messageDescriptor.GetArguments(serializedMessage);

            var arguments = DeserializeMessageArguments(argumentsDescriptors);

            var protobufInvocationMessage = new InvocationMessageProtobuf();

            protobufInvocationMessage.MergeFrom(protobufMessage.ToArray());

            return new InvocationMessage(protobufInvocationMessage.InvocationId, protobufInvocationMessage.Target, arguments);
        }

        public object[] DeserializeMessageArguments(List<ArgumentDescriptor> argumentsDescriptor)
        {
            var arguments = new List<object>();

            for (var i = 0; i < argumentsDescriptor.Count; i++)
            {
                var argument = Encoding.UTF8.GetString(argumentsDescriptor[i].Argument);

                arguments.Add(argument);
            }
            return arguments.ToArray();
        }

        public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
        {
            switch (message)
            {
                case InvocationMessage invocationMessage:
                    WriteInvocationMessage(invocationMessage, output);
                    break;
                case StreamInvocationMessage streamInvocationMessage:
                    WriteStreamInvocationMessage(streamInvocationMessage, output);
                    break;
                case StreamItemMessage streamItemMessage:
                    WriteItemMessage(streamItemMessage, output);
                    break;
                case CompletionMessage completionMessage:
                    WriteCompletionMessage(completionMessage, output);
                    break;
                case CancelInvocationMessage cancelInvocationMessage:
                    WriteCancelInvocationMessage(cancelInvocationMessage, output);
                    break;
                case PingMessage pingMessage:
                    WritePingMessage(pingMessage, output);
                    break;
                case CloseMessage closeMessage:
                    WriteCloseMessage(closeMessage, output);
                    break;
                default:
                    _logger.LogCritical($"Unexpected message type: {message.GetType().Name}");
                    break;
            }
        }

        private void WriteInvocationMessage(InvocationMessage invocationMessage, IBufferWriter<byte> output)
        {
            var protobufInvocationMessage = new InvocationMessageProtobuf
            {
                InvocationId = invocationMessage.InvocationId ?? "",
                Target = invocationMessage.Target
            };

            var arguments = SerializeArguments(invocationMessage.Arguments);

            var packedMessage = _messageDescriptor.PackMessage(HubProtocolConstants.InvocationMessageType, protobufInvocationMessage.ToByteArray(), arguments);

            output.Write(packedMessage);
        }

        private void WriteStreamInvocationMessage(StreamInvocationMessage streamInvocationMessage, IBufferWriter<byte> output)
        {
            var packedMessage = _messageDescriptor.PackMessage(HubProtocolConstants.StreamInvocationMessageType, Array.Empty<byte>(), new List<ArgumentDescriptor>());

            output.Write(packedMessage);
        }

        private void WriteItemMessage(StreamItemMessage streamItemMessage, IBufferWriter<byte> output)
        {
            var packedMessage = _messageDescriptor.PackMessage(HubProtocolConstants.StreamItemMessageType, Array.Empty<byte>(), new List<ArgumentDescriptor>());

            output.Write(packedMessage);
        }

        private void WriteCompletionMessage(CompletionMessage completionMessage, IBufferWriter<byte> output)
        {
            var packedMessage = _messageDescriptor.PackMessage(HubProtocolConstants.CompletionMessageType, Array.Empty<byte>(), new List<ArgumentDescriptor>());

            output.Write(packedMessage);
        }

        private void WriteCancelInvocationMessage(CancelInvocationMessage cancelInvocationMessage, IBufferWriter<byte> output)
        {
            var packedMessage = _messageDescriptor.PackMessage(HubProtocolConstants.CancelInvocationMessageType, Array.Empty<byte>(), new List<ArgumentDescriptor>());

            output.Write(packedMessage);
        }

        private void WritePingMessage(PingMessage pingMessage, IBufferWriter<byte> output)
        {
            var packedMessage = _messageDescriptor.PackMessage(HubProtocolConstants.PingMessageType, Array.Empty<byte>(), new List<ArgumentDescriptor>());

            output.Write(packedMessage);
        }

        private void WriteCloseMessage(CloseMessage closeMessage, IBufferWriter<byte> output)
        {
            var packedMessage = _messageDescriptor.PackMessage(HubProtocolConstants.CloseMessageType, Array.Empty<byte>(), new List<ArgumentDescriptor>());

            output.Write(packedMessage);
        }

        private List<ArgumentDescriptor> SerializeArguments(object[] arguments)
        {
            var argumentsDescriptors = new List<ArgumentDescriptor>(arguments.Length);

            for (var i = 0; i < arguments.Length; i++)
            {
                var argumentDescriptor = DescribeArgument(arguments[i]);
                argumentsDescriptors.Add(argumentDescriptor);
            }

            return argumentsDescriptors;
        }

        private ArgumentDescriptor DescribeArgument(object argument)
        {
            switch (argument)
            {
                case string item:
                    return new ArgumentDescriptor(-2, Encoding.UTF8.GetBytes(item));
                default:
                    return null;
            }
        }
    }
}
