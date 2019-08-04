using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
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

        private readonly Dictionary<Type, int> _protobufTypeToIndex;
        private readonly Dictionary<int, Type> _indexToProtobufType;

        private readonly int _numberOfNoProtobufObjectHandle = 4;

        public string Name => _protocolName;

        public int Version => _protocolVersion;

        public TransferFormat TransferFormat => TransferFormat.Binary;

        public ProtobufHubProtocol(IEnumerable<Type> protobufTypes, ILogger<ProtobufHubProtocol> logger)
        {
            _logger = logger;
            _messageDescriptor = new MessageDescriptor();

            var size = protobufTypes.Count();
            _protobufTypeToIndex = new Dictionary<Type, int>(size);
            _indexToProtobufType = new Dictionary<int, Type>(size);

            var enumerator = protobufTypes.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var index = _protobufTypeToIndex.Count + _numberOfNoProtobufObjectHandle + 1;
                _protobufTypeToIndex[enumerator.Current] = index;
                _indexToProtobufType[index] = enumerator.Current;
            }
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

            try
            {
                message = CreateHubMessage(serializedMessage, messageType);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogCritical($"Protobuf Type not available, did you register it ?: {ex.ToString()}");
                message = null;
                return false;
            }

            input = input.Slice(totalSize + ProtobufHubProtocolConstants.TYPE_AND_TOTAL_LENGTH_HEADER);

            return message == null ? false : true;
        }

        private HubMessage CreateHubMessage(ReadOnlySpan<byte> serializedMessage, int messageType)
        {
            var protobufMessage = _messageDescriptor.GetProtobufMessage(serializedMessage);

            var argumentsDescriptors = _messageDescriptor.GetArguments(serializedMessage);

            var arguments = DeserializeMessageArguments(argumentsDescriptors);

            switch (messageType)
            {
                case HubProtocolConstants.InvocationMessageType:
                    return CreateHubInvocationMessage(protobufMessage, arguments);
                case HubProtocolConstants.StreamItemMessageType:
                    return CreateHubStreamItemMessage(protobufMessage, arguments);
                case HubProtocolConstants.CompletionMessageType:
                    return CreateHubCompletionMessage(protobufMessage, arguments);
                case HubProtocolConstants.StreamInvocationMessageType:
                    return CreateHubStreamInvocationMessage(protobufMessage, arguments);
                case HubProtocolConstants.CancelInvocationMessageType:
                    return CreateHubCancelInvocationMessage(protobufMessage);
                case HubProtocolConstants.PingMessageType:
                    return PingMessage.Instance;
                case HubProtocolConstants.CloseMessageType:
                    return CreateHubCloseMessage(protobufMessage);
                default:
                    return null;
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

            return new CloseMessage(protobufCloseMessage.Error);
        }

        public object[] DeserializeMessageArguments(List<ArgumentDescriptor> argumentsDescriptor)
        {
            var arguments = new List<object>();

            for (var i = 0; i < argumentsDescriptor.Count; i++)
            {
                var currentDescriptor = argumentsDescriptor[i];

                if (currentDescriptor.Type <= _numberOfNoProtobufObjectHandle)
                {
                    var argument = DeserializeNotProtobufObjectArgument(argumentsDescriptor[i]);
                    arguments.Add(argument);
                }
                else
                {
                    var argument = (IMessage)Activator.CreateInstance(_indexToProtobufType[currentDescriptor.Type]);
                    argument.MergeFrom(currentDescriptor.Argument);
                    arguments.Add(argument);
                }
            }
            return arguments.ToArray();
        }

        public object DeserializeNotProtobufObjectArgument(ArgumentDescriptor argumentDescriptor)
        {
            switch (argumentDescriptor.Type)
            {
                case 2:
                    return Encoding.UTF8.GetString(argumentDescriptor.Argument);
                case 3:
                    return BitConverter.ToInt32(argumentDescriptor.Argument, 0);
                case 4:
                    return BitConverter.ToDouble(argumentDescriptor.Argument, 0);
                default:
                    return null;
            }
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

            var arguments = SerializeArguments(invocationMessage.Arguments);

            var packedMessage = _messageDescriptor.PackMessage(HubProtocolConstants.InvocationMessageType, protobufInvocationMessage.ToByteArray(), arguments);

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

            var arguments = SerializeArguments(streamInvocationMessage.Arguments);

            var packedMessage = _messageDescriptor.PackMessage(HubProtocolConstants.StreamInvocationMessageType, protobufStreamInvocationMessage.ToByteArray(), arguments);

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

            var item = DescribeArgument(streamItemMessage.Item);

            var packedMessage = _messageDescriptor.PackMessage(HubProtocolConstants.StreamItemMessageType, protobufStreamItemMessage.ToByteArray(), new List<ArgumentDescriptor> { item });

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
                result = DescribeArgument(completionMessage.Result);
            }

            var packedMessage = _messageDescriptor.PackMessage(HubProtocolConstants.CompletionMessageType, protobufCompletionMessage.ToByteArray(), new List<ArgumentDescriptor> { result });

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

            var packedMessage = _messageDescriptor.PackMessage(HubProtocolConstants.CancelInvocationMessageType, protobufCancelInvocationMessage.ToByteArray(), new List<ArgumentDescriptor>());

            output.Write(packedMessage);
        }

        private void WritePingMessage(PingMessage pingMessage, IBufferWriter<byte> output)
        {
            var packedMessage = _messageDescriptor.PackMessage(HubProtocolConstants.PingMessageType, Array.Empty<byte>(), new List<ArgumentDescriptor>());

            output.Write(packedMessage);
        }

        private void WriteCloseMessage(CloseMessage closeMessage, IBufferWriter<byte> output)
        {
            var protobufCloseMessage = new CloseMessageProtobuf
            {
                Error = closeMessage.Error
            };

            var packedMessage = _messageDescriptor.PackMessage(HubProtocolConstants.CloseMessageType, protobufCloseMessage.ToByteArray(), new List<ArgumentDescriptor>());

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
                    return new ArgumentDescriptor(2, Encoding.UTF8.GetBytes(item));
                case int item:
                    return new ArgumentDescriptor(3, BitConverter.GetBytes(item));
                case double item:
                    return new ArgumentDescriptor(4, BitConverter.GetBytes(item));
                case IMessage item:
                    return new ArgumentDescriptor(_protobufTypeToIndex[item.GetType()], item.ToByteArray());
                default:
                    return null;
            }
        }
    }
}
