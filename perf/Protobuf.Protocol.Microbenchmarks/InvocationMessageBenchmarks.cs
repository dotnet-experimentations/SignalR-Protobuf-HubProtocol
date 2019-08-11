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
        warmupCount: 3, targetCount: 50, id: "InvocationMessage")]
    public class InvocationMessageBenchmarks
    {
        private ProtobufHubProtocol _hubProtocol;
        private ReadOnlyMemory<byte> _serializedMessageRef;
        private InvocationMessage _invocationMessage;

        [Params(MessageArgument.NoArguments, MessageArgument.IntArguments, MessageArgument.DoubleArguments, MessageArgument.StringARguments,
                MessageArgument.ProtobufArguments, MessageArgument.FewArguments, MessageArgument.ManyArguments, MessageArgument.LargeArguments)]
        public MessageArgument InputArgument { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var logger = new NullLogger<ProtobufHubProtocol>();
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
                case MessageArgument.NoArguments:
                    _invocationMessage = new InvocationMessage("BenchmarkTarget", Array.Empty<object>());
                    break;
                case MessageArgument.IntArguments:
                    _invocationMessage = new InvocationMessage("BenchmarkTarget", new object[] { int.MinValue, 0, int.MaxValue });
                    break;
                case MessageArgument.DoubleArguments:
                    _invocationMessage = new InvocationMessage("BenchmarkTarget", new object[] { double.MinValue, 0.5d, double.MaxValue });
                    break;
                case MessageArgument.StringARguments:
                    _invocationMessage = new InvocationMessage("BenchmarkTarget", new object[] { "Foo", "Bar", new string('#', 512) });
                    break;
                case MessageArgument.ProtobufArguments:
                    _invocationMessage = new InvocationMessage("BenchmarkTarget", new object[] { data, data, data });
                    break;
                case MessageArgument.FewArguments:
                    _invocationMessage = new InvocationMessage("BenchmarkTarget", new object[] { data, "FooBar", 1 });
                    break;
                case MessageArgument.ManyArguments:
                    _invocationMessage = new InvocationMessage("BenchmarkTarget", new object[] { data, "FooBar", 1, 234.543d, data, "foo.bar@awesome.com", 4242, 21.123456d, data });
                    break;
                case MessageArgument.LargeArguments:
                    data.Data = new string('@', 4096);
                    _invocationMessage = new InvocationMessage("BenchmarkTarget", new object[] { data, new string('L', 10240) });
                    break;
            }            

            _serializedMessageRef = _hubProtocol.GetMessageBytes(_invocationMessage);
        }

        [Benchmark]
        public void Serialization()
        {
            var bytes = _hubProtocol.GetMessageBytes(_invocationMessage);
            if (bytes.Length != _serializedMessageRef.Length)
            {
                throw new InvalidOperationException("Failed to serialized invocation message");
            }
        }

        [Benchmark]
        public void Deserialization()
        {
            var serializedMessage = new ReadOnlySequence<byte>(_serializedMessageRef);

            if (!_hubProtocol.TryParseMessage(ref serializedMessage, null, out _))
            {
                throw new InvalidOperationException("Failed to deserialized invocation message");
            }
        }
    }
}
