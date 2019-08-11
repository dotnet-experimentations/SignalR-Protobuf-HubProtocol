using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using SignalR.Protobuf.Protocol;
using System;
using System.Buffers;

namespace Protobuf.Protocol.Microbenchmarks
{
    [MemoryDiagnoser]
    [Config(typeof(BenchmarkConfiguration))]
    [SimpleJob(RunStrategy.Throughput, launchCount: 4,
        warmupCount: 3, targetCount: 50, id: "StreamItemMessage")]
    public class StreamItemMessageBenchmarks
    {
        private ProtobufHubProtocol _hubProtocol;
        private ReadOnlyMemory<byte> _serializedMessageRef;
        private StreamItemMessage _streamItemMessage;

        [Params(MessageArgument.IntArguments, MessageArgument.DoubleArguments, MessageArgument.StringArguments,
                MessageArgument.ProtobufArguments, MessageArgument.LargeArguments)]
        public MessageArgument InputArgument { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var logger = NullLogger<ProtobufHubProtocol>.Instance;
            var types = new[] { typeof(BenchMessage) };
            _hubProtocol = new ProtobufHubProtocol(types, logger);

            var data = new BenchMessage
            {
                Email = "foo.bar@awesome.com",
                Data = new string('@', 512),
                Length = 256,
                Price = 23451.5436d,
                Time = new Google.Protobuf.WellKnownTypes.Timestamp() { Seconds = DateTime.UtcNow.Second }
            };

            switch (InputArgument)
            {
                case MessageArgument.IntArguments:
                    _streamItemMessage = new StreamItemMessage("123", int.MinValue);
                    break;
                case MessageArgument.DoubleArguments:
                    _streamItemMessage = new StreamItemMessage("123", double.MaxValue);
                    break;
                case MessageArgument.StringArguments:
                    _streamItemMessage = new StreamItemMessage("123", new string('#', 512));
                    break;
                case MessageArgument.ProtobufArguments:
                    _streamItemMessage = new StreamItemMessage("123", data);
                    break;
                case MessageArgument.LargeArguments:
                    data.Data = new string('@', 10240);
                    _streamItemMessage = new StreamItemMessage("123", data);
                    break;
            }

            _serializedMessageRef = _hubProtocol.GetMessageBytes(_streamItemMessage);
        }

        [Benchmark]
        public void Serialization()
        {
            var bytes = _hubProtocol.GetMessageBytes(_streamItemMessage);
            if (bytes.Length != _serializedMessageRef.Length)
            {
                throw new InvalidOperationException("Failed to serialized stream item message");
            }
        }

        [Benchmark]
        public void Deserialization()
        {
            var serializedMessage = new ReadOnlySequence<byte>(_serializedMessageRef);

            if (!_hubProtocol.TryParseMessage(ref serializedMessage, null, out _))
            {
                throw new InvalidOperationException("Failed to deserialized stream item message");
            }
        }
    }
}
