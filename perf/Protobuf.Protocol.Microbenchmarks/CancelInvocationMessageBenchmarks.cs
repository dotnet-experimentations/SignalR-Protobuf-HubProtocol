using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Buffers;

namespace Protobuf.Protocol.Microbenchmarks
{
    [MemoryDiagnoser]
    [Config(typeof(BenchmarkConfiguration))]
    [SimpleJob(RunStrategy.Throughput, launchCount: 4,
        warmupCount: 3, targetCount: 50, id: "CancelInvocationMessage")]
    public class CancelInvocationMessageBenchmarks
    {
        private ProtobufHubProtocol _hubProtocol;
        private ReadOnlyMemory<byte> _serializedMessageRef;
        private CancelInvocationMessage _cancelInvocationMessage;

        [GlobalSetup]
        public void Setup()
        {
            var logger = NullLogger<ProtobufHubProtocol>.Instance;
            var types = Array.Empty<Type>();
            _hubProtocol = new ProtobufHubProtocol(types, logger);
            _cancelInvocationMessage = new CancelInvocationMessage("123");
            _serializedMessageRef = _hubProtocol.GetMessageBytes(_cancelInvocationMessage);
        }

        [Benchmark]
        public void Serialization()
        {
            var bytes = _hubProtocol.GetMessageBytes(_cancelInvocationMessage);
            if (bytes.Length != _serializedMessageRef.Length)
            {
                throw new InvalidOperationException("Failed to serialized cancel invocation message");
            }
        }

        [Benchmark]
        public void Deserialization()
        {
            var serializedMessage = new ReadOnlySequence<byte>(_serializedMessageRef);

            if (!_hubProtocol.TryParseMessage(ref serializedMessage, null, out _))
            {
                throw new InvalidOperationException("Failed to deserialized cancel invocation message");
            }
        }
    }
}
