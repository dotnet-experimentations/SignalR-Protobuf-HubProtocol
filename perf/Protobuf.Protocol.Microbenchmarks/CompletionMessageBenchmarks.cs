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
        warmupCount: 3, targetCount: 50, id: "CompletionMessage")]
    public class CompletionMessageBenchmarks
    {
        private ProtobufHubProtocol _hubProtocol;
        private ReadOnlyMemory<byte> _serializedMessageRef;
        private CompletionMessage _completionMessage;

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
                    _completionMessage = new CompletionMessage("123", null, int.MinValue, true);
                    break;
                case MessageArgument.DoubleArguments:
                    _completionMessage = new CompletionMessage("123", null, double.MaxValue, true);
                    break;
                case MessageArgument.StringArguments:
                    _completionMessage = new CompletionMessage("123", null, new string('#', 512), true);
                    break;
                case MessageArgument.ProtobufArguments:
                    _completionMessage = new CompletionMessage("123", null, data, true);
                    break;
                case MessageArgument.LargeArguments:
                    data.Data = new string('@', 10240);
                    _completionMessage = new CompletionMessage("123", null, data, true);
                    break;
            }

            _serializedMessageRef = _hubProtocol.GetMessageBytes(_completionMessage);
        }

        [Benchmark]
        public void Serialization()
        {
            var bytes = _hubProtocol.GetMessageBytes(_completionMessage);
            if (bytes.Length != _serializedMessageRef.Length)
            {
                throw new InvalidOperationException("Failed to serialized completion message");
            }
        }

        [Benchmark]
        public void Deserialization()
        {
            var serializedMessage = new ReadOnlySequence<byte>(_serializedMessageRef);

            if (!_hubProtocol.TryParseMessage(ref serializedMessage, null, out _))
            {
                throw new InvalidOperationException("Failed to deserialized completion message");
            }
        }
    }
}
