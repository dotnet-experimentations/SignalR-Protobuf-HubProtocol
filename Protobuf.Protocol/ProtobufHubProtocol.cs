using System;
using System.Buffers;
using System.IO;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;

namespace Protobuf.Protocol
{
    public class ProtobufHubProtocol : IHubProtocol
    {
        private static readonly string _protocolName = "protobuf";
        private static readonly int _protocolVersion = 1;

        private readonly ILogger<ProtobufHubProtocol> _logger;

        public string Name => _protocolName;

        public int Version => _protocolVersion;

        public TransferFormat TransferFormat => TransferFormat.Binary;

        public ProtobufHubProtocol(ILogger<ProtobufHubProtocol> logger)
        {
            _logger = logger;
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
            if (input.Length < ProtobufHubProtocolConstants.MESSAGE_HEADER_SIZE)
            {
                message = null;
                return false;
            }

            var protobufMessageType = (int)input.Slice(0, 1).ToArray()[0];

            if (protobufMessageType == HubProtocolConstants.PingMessageType)
            {
                message = PingMessage.Instance;
                input = input.Slice(ProtobufHubProtocolConstants.MESSAGE_HEADER_SIZE);
                return true;
            }

            message = null;
            return true;
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
            var totalSize = GetMessageSize();

            var byteArray = ArrayPool<byte>.Shared.Rent(totalSize);
            try
            {
                using (var outputStream = new MemoryStream(byteArray))
                {
                    WriteProtocolHeader(outputStream, HubProtocolConstants.InvocationMessageType, totalSize);
                    output.Write(new ReadOnlySpan<byte>(byteArray, 0, totalSize));
                };
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteArray);
            }
        }

        private void WriteStreamInvocationMessage(StreamInvocationMessage streamInvocationMessage, IBufferWriter<byte> output)
        {
            var totalSize = GetMessageSize();

            var byteArray = ArrayPool<byte>.Shared.Rent(totalSize);
            try
            {
                using (var outputStream = new MemoryStream(byteArray))
                {
                    WriteProtocolHeader(outputStream, HubProtocolConstants.StreamInvocationMessageType, totalSize);
                    output.Write(new ReadOnlySpan<byte>(byteArray, 0, totalSize));
                };
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteArray);
            }
        }

        private void WriteItemMessage(StreamItemMessage streamItemMessage, IBufferWriter<byte> output)
        {
            var totalSize = GetMessageSize();

            var byteArray = ArrayPool<byte>.Shared.Rent(totalSize);
            try
            {
                using (var outputStream = new MemoryStream(byteArray))
                {
                    WriteProtocolHeader(outputStream, HubProtocolConstants.StreamItemMessageType, totalSize);
                    output.Write(new ReadOnlySpan<byte>(byteArray, 0, totalSize));
                };
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteArray);
            }
        }

        private void WriteCompletionMessage(CompletionMessage completionMessage, IBufferWriter<byte> output)
        {
            var totalSize = GetMessageSize();

            var byteArray = ArrayPool<byte>.Shared.Rent(totalSize);
            try
            {
                using (var outputStream = new MemoryStream(byteArray))
                {
                    WriteProtocolHeader(outputStream, HubProtocolConstants.CompletionMessageType, totalSize);
                    output.Write(new ReadOnlySpan<byte>(byteArray, 0, totalSize));
                };
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteArray);
            }
        }

        private void WriteCancelInvocationMessage(CancelInvocationMessage cancelInvocationMessage, IBufferWriter<byte> output)
        {
            var totalSize = GetMessageSize();

            var byteArray = ArrayPool<byte>.Shared.Rent(totalSize);
            try
            {
                using (var outputStream = new MemoryStream(byteArray))
                {
                    WriteProtocolHeader(outputStream, HubProtocolConstants.CancelInvocationMessageType, totalSize);
                    output.Write(new ReadOnlySpan<byte>(byteArray, 0, totalSize));
                };
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteArray);
            }
        }

        private void WritePingMessage(PingMessage pingMessage, IBufferWriter<byte> output)
        {
            var totalSize = GetMessageSize();

            var byteArray = ArrayPool<byte>.Shared.Rent(totalSize);
            try
            {
                using (var outputStream = new MemoryStream(byteArray))
                {
                    WriteProtocolHeader(outputStream, HubProtocolConstants.PingMessageType, totalSize);
                    output.Write(new ReadOnlySpan<byte>(byteArray, 0, totalSize));
                };
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteArray);
            }
        }

        private void WriteCloseMessage(CloseMessage closeMessage, IBufferWriter<byte> output)
        {
            var totalSize = GetMessageSize();

            var byteArray = ArrayPool<byte>.Shared.Rent(totalSize);
            try
            {
                using (var outputStream = new MemoryStream(byteArray))
                {
                    WriteProtocolHeader(outputStream, HubProtocolConstants.CloseMessageType, totalSize);
                    output.Write(new ReadOnlySpan<byte>(byteArray, 0, totalSize));
                };
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteArray);
            }
        }

        private void WriteProtocolHeader(Stream stream, int messageType, int totalSize)
        {
            stream.Write(new byte[] { (byte)messageType }, 0, 1);
            stream.Write(BitConverter.GetBytes(totalSize), 0, 4);
            stream.Write(BitConverter.GetBytes(1), 0, 4);
        }

        private int GetMessageSize()
        {
            return 1 // type
                   + 4 // Int for total size
                   + 4; // Int for protobuf message size
        }
    }
}
