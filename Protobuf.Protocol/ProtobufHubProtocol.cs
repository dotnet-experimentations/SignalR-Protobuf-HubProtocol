using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
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

            var messageType = (int)input.Slice(0, 1).ToArray()[0];

            message = CreateHubMessage(ref input, messageType);

            var totalSize = BitConverter.ToInt32(input.Slice(1, 4).ToArray(), 0);
            input = input.Slice(totalSize + ProtobufHubProtocolConstants.TYPE_AND_TOTAL_LENGTH_HEADER);

            return true;
        }

        private HubMessage CreateHubMessage(ref ReadOnlySequence<byte> input, int messageType)
        {
            var protobufMessageLength = BitConverter.ToInt32(input.Slice(ProtobufHubProtocolConstants.TYPE_AND_TOTAL_LENGTH_HEADER, 4).ToArray(), 0);
            var protobufInput = input.Slice(ProtobufHubProtocolConstants.MESSAGE_HEADER_LENGTH, protobufMessageLength);

            switch (messageType)
            {
                case HubProtocolConstants.InvocationMessageType:
                    return CreateHubInvocationMessage(protobufInput);
                case HubProtocolConstants.PingMessageType:
                    return PingMessage.Instance;
                default:
                    return null;
            }
        }

        private HubMessage CreateHubInvocationMessage(ReadOnlySequence<byte> protobufMessage)
        {
            var protobufInvocationMessage = new InvocationMessageProtobuf();

            protobufInvocationMessage.MergeFrom(protobufMessage.ToArray());

            return new InvocationMessage(protobufInvocationMessage.InvocationId, protobufInvocationMessage.Target, Array.Empty<object>());
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

            var packedMessage = _messageDescriptor.PackMessage(HubProtocolConstants.InvocationMessageType, protobufInvocationMessage.ToByteArray(), new List<ArgumentDescriptor>());

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
    }
}
