﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        private readonly ArgumentSerializer _argumentSerializer;

        public string Name => _protocolName;

        public int Version => _protocolVersion;

        public TransferFormat TransferFormat => TransferFormat.Binary;

        public ProtobufHubProtocol(IEnumerable<Type> protobufTypes, ILogger<ProtobufHubProtocol> logger)
        {
            _logger = logger;
            _argumentSerializer = new ArgumentSerializer(protobufTypes);
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

            var span = input.IsSingleSegment ? input.First.Span : input.ToArray();

            var totalSize = MessageDescriptor.GetTotalMessageLength(span) + ProtobufHubProtocolConstants.TYPE_AND_TOTAL_LENGTH_HEADER;

            if (input.Length < totalSize)
            {
                message = null;
                return false;
            }

            try
            {
                message = CreateHubMessage(span.Slice(0, totalSize));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogCritical($"Protobuf Type not available, did you register it ?: {ex.ToString()}");
                message = null;
                return false;
            }

            input = input.Slice(totalSize);

            return message == null ? false : true;
        }

        private HubMessage CreateHubMessage(ReadOnlySpan<byte> serializedMessage)
        {
            var messageType = MessageDescriptor.GetMessageType(serializedMessage);

            var protobufMessage = MessageDescriptor.GetProtobufMessage(serializedMessage);

            switch (messageType)
            {
                case HubProtocolConstants.InvocationMessageType:
                    return CreateHubInvocationMessage(protobufMessage, GetArguments(serializedMessage));
                case HubProtocolConstants.StreamItemMessageType:
                    return CreateHubStreamItemMessage(protobufMessage, GetArguments(serializedMessage));
                case HubProtocolConstants.CompletionMessageType:
                    return CreateHubCompletionMessage(protobufMessage, GetArguments(serializedMessage));
                case HubProtocolConstants.StreamInvocationMessageType:
                    return CreateHubStreamInvocationMessage(protobufMessage, GetArguments(serializedMessage));
                case HubProtocolConstants.CancelInvocationMessageType:
                    return CreateHubCancelInvocationMessage(protobufMessage);
                case HubProtocolConstants.PingMessageType:
                    return PingMessage.Instance;
                case HubProtocolConstants.CloseMessageType:
                    return CreateHubCloseMessage(protobufMessage);
                default:
                    return null;
            }

            object[] GetArguments(ReadOnlySpan<byte> message)
            {
                var argumentsDescriptors = MessageDescriptor.GetArguments(message);

                return _argumentSerializer.DeserializeArguments(argumentsDescriptors);
            }
        }

        private HubMessage CreateHubInvocationMessage(ReadOnlySpan<byte> protobufMessage, object[] arguments)
        {
            var protobufInvocationMessage = new InvocationMessageProtobuf();

            protobufInvocationMessage.MergeFrom(protobufMessage.ToArray());

            return new InvocationMessage(protobufInvocationMessage.InvocationId, protobufInvocationMessage.Target, arguments, protobufInvocationMessage.StreamIds.ToArray())
            {
                Headers = protobufInvocationMessage.Headers
            };
        }

        private HubMessage CreateHubStreamItemMessage(ReadOnlySpan<byte> protobufMessage, object[] arguments)
        {
            var protobufStreamItemMessage = new StreamItemMessageProtobuf();

            protobufStreamItemMessage.MergeFrom(protobufMessage.ToArray());

            return new StreamItemMessage(protobufStreamItemMessage.InvocationId, arguments.FirstOrDefault())
            {
                Headers = protobufStreamItemMessage.Headers
            };
        }

        private HubMessage CreateHubCompletionMessage(ReadOnlySpan<byte> protobufMessage, object[] arguments)
        {
            var protobufCompletionMessage = new CompletionMessageProtobuf();

            protobufCompletionMessage.MergeFrom(protobufMessage.ToArray());

            if (!string.IsNullOrEmpty(protobufCompletionMessage.Error) || arguments.FirstOrDefault() == null)
            {
                return new CompletionMessage(protobufCompletionMessage.InvocationId, protobufCompletionMessage.Error, null, false)
                {
                    Headers = protobufCompletionMessage.Headers
                };
            }
            return new CompletionMessage(protobufCompletionMessage.InvocationId, null, arguments.FirstOrDefault(), true)
            {
                Headers = protobufCompletionMessage.Headers
            };
        }

        private HubMessage CreateHubStreamInvocationMessage(ReadOnlySpan<byte> protobufMessage, object[] arguments)
        {
            var protobufStreamInvocationMessage = new StreamInvocationMessageProtobuf();

            protobufStreamInvocationMessage.MergeFrom(protobufMessage.ToArray());

            return new StreamInvocationMessage(protobufStreamInvocationMessage.InvocationId, protobufStreamInvocationMessage.Target, arguments, protobufStreamInvocationMessage.StreamIds.ToArray())
            {
                Headers = protobufStreamInvocationMessage.Headers
            };
        }

        private HubMessage CreateHubCancelInvocationMessage(ReadOnlySpan<byte> protobufMessage)
        {
            var protobufCancelInvocationMessage = new CancelInvocationMessageProtobuf();

            protobufCancelInvocationMessage.MergeFrom(protobufMessage.ToArray());

            return new CancelInvocationMessage(protobufCancelInvocationMessage.InvocationId)
            {
                Headers = protobufCancelInvocationMessage.Headers
            };
        }

        private HubMessage CreateHubCloseMessage(ReadOnlySpan<byte> protobufMessage)
        {
            var protobufCloseMessage = new CloseMessageProtobuf();

            protobufCloseMessage.MergeFrom(protobufMessage.ToArray());

            var error = string.IsNullOrEmpty(protobufCloseMessage.Error) ? null : protobufCloseMessage.Error;

            return new CloseMessage(error);
        }

        public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
        {
            try
            {
                WriteMessageCore(message, output);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogCritical($"Protobuf Type not available, did you register it ?: {ex.ToString()}");
            }
        }

        private void WriteMessageCore(HubMessage message, IBufferWriter<byte> output)
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

            if (invocationMessage.Headers != null)
            {
                protobufInvocationMessage.Headers.Add(invocationMessage.Headers);
            }

            if (invocationMessage.StreamIds != null)
            {
                protobufInvocationMessage.StreamIds.Add(invocationMessage.StreamIds.Select(id => id ?? ""));
            }

            var arguments = _argumentSerializer.SerializeArguments(invocationMessage.Arguments);

            var packedMessage = MessageDescriptor.PackMessage(HubProtocolConstants.InvocationMessageType, protobufInvocationMessage.ToByteArray(), arguments);

            output.Write(packedMessage);
        }

        private void WriteStreamInvocationMessage(StreamInvocationMessage streamInvocationMessage, IBufferWriter<byte> output)
        {
            var protobufStreamInvocationMessage = new StreamInvocationMessageProtobuf
            {
                InvocationId = streamInvocationMessage.InvocationId,
                Target = streamInvocationMessage.Target
            };

            if (streamInvocationMessage.Headers != null)
            {
                protobufStreamInvocationMessage.Headers.Add(streamInvocationMessage.Headers);
            }

            if (streamInvocationMessage.StreamIds != null)
            {
                protobufStreamInvocationMessage.StreamIds.Add(streamInvocationMessage.StreamIds.Select(id => id ?? ""));
            }

            var arguments = _argumentSerializer.SerializeArguments(streamInvocationMessage.Arguments);

            var packedMessage = MessageDescriptor.PackMessage(HubProtocolConstants.StreamInvocationMessageType, protobufStreamInvocationMessage.ToByteArray(), arguments);

            output.Write(packedMessage);
        }

        private void WriteItemMessage(StreamItemMessage streamItemMessage, IBufferWriter<byte> output)
        {
            var protobufStreamItemMessage = new StreamItemMessageProtobuf
            {
                InvocationId = streamItemMessage.InvocationId
            };

            if (streamItemMessage.Headers != null)
            {
                protobufStreamItemMessage.Headers.Add(streamItemMessage.Headers);
            }

            var item = _argumentSerializer.SerializeArgument(streamItemMessage.Item);

            var packedMessage = MessageDescriptor.PackMessage(HubProtocolConstants.StreamItemMessageType, protobufStreamItemMessage.ToByteArray(), new List<ArgumentDescriptor> { item });

            output.Write(packedMessage);
        }

        private void WriteCompletionMessage(CompletionMessage completionMessage, IBufferWriter<byte> output)
        {
            var protobufCompletionMessage = new CompletionMessageProtobuf
            {
                Error = completionMessage.Error ?? "",
                InvocationId = completionMessage.InvocationId
            };

            if (completionMessage.Headers != null)
            {
                protobufCompletionMessage.Headers.Add(completionMessage.Headers);
            }

            var result = new ArgumentDescriptor(0, Array.Empty<byte>());
            if (completionMessage.Result != null)
            {
                result = _argumentSerializer.SerializeArgument(completionMessage.Result);
            }

            var packedMessage = MessageDescriptor.PackMessage(HubProtocolConstants.CompletionMessageType, protobufCompletionMessage.ToByteArray(), new List<ArgumentDescriptor> { result });

            output.Write(packedMessage);
        }

        private void WriteCancelInvocationMessage(CancelInvocationMessage cancelInvocationMessage, IBufferWriter<byte> output)
        {
            var protobufCancelInvocationMessage = new CancelInvocationMessageProtobuf
            {
                InvocationId = cancelInvocationMessage.InvocationId
            };

            if (cancelInvocationMessage.Headers != null)
            {
                protobufCancelInvocationMessage.Headers.Add(cancelInvocationMessage.Headers);
            }

            var packedMessage = MessageDescriptor.PackMessage(HubProtocolConstants.CancelInvocationMessageType, protobufCancelInvocationMessage.ToByteArray());

            output.Write(packedMessage);
        }

        private void WritePingMessage(PingMessage pingMessage, IBufferWriter<byte> output)
        {
            var packedMessage = MessageDescriptor.PackMessage(HubProtocolConstants.PingMessageType, Array.Empty<byte>());

            output.Write(packedMessage);
        }

        private void WriteCloseMessage(CloseMessage closeMessage, IBufferWriter<byte> output)
        {
            var protobufCloseMessage = new CloseMessageProtobuf
            {
                Error = closeMessage.Error ?? ""
            };

            var packedMessage = MessageDescriptor.PackMessage(HubProtocolConstants.CloseMessageType, protobufCloseMessage.ToByteArray());

            output.Write(packedMessage);
        }
    }
}
