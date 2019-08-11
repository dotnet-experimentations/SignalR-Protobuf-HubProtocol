using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using SignalR.Protobuf.Protocol;
using System;
using System.Buffers;

namespace Protobuf.Protocol.Microbenchmarks
{
    [MemoryDiagnoser]
    [Config(typeof(Config))]
    [SimpleJob(RunStrategy.Throughput, launchCount: 4,
        warmupCount: 3, targetCount: 100, id: "Serialization")]
    public class SerializationBenchmarks
    {
        private ProtobufHubProtocol _hubProtocol;
        private ArrayBufferWriter<byte> _writer = new ArrayBufferWriter<byte>();

        private InvocationMessage _invocationMessageNoArgs;
        private InvocationMessage _invocationMessageStringArgs;
        private InvocationMessage _invocationMessageIntArgs;
        private InvocationMessage _invocationMessageDoubleArgs;
        private InvocationMessage _invocationMessageProtobufArgs;
        private InvocationMessage _invocationMessage;

        private class Config : ManualConfig
        {
            public Config()
            {
                Add(StatisticColumn.P80,
                    StatisticColumn.P85,
                    StatisticColumn.P90,
                    StatisticColumn.P95,
                    StatisticColumn.P100);
            }
        }

        [Params(20 , 50, 100)]
        public int TargetLength { get; set; }

        [Params(100, 300, 500, 1000, 5000, 10000)]
        public int ArgumentLength { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            Console.WriteLine("Setup");
            var logger = new NullLogger<ProtobufHubProtocol>();
            var types = new[] { typeof(BenchMessage) };
            _hubProtocol = new ProtobufHubProtocol(types, logger);
            _writer.Clear();

            var target = new string('#', TargetLength);
            var data = new string('@', ArgumentLength);
            var protobufObject = new BenchMessage
            {
                Data = data,
                Email = "foo.bar@topmail.com",
                Length = ArgumentLength,
                Price = 2456123.6547893,
                Time = new Google.Protobuf.WellKnownTypes.Timestamp() { Seconds = DateTime.UtcNow.Second }
            };

            _invocationMessageNoArgs = new InvocationMessage(target, Array.Empty<object>());
            _invocationMessageStringArgs = new InvocationMessage(target, new object[] { data, data, data });
            _invocationMessageIntArgs = new InvocationMessage(target, new object[] { ArgumentLength, ArgumentLength, ArgumentLength });
            _invocationMessageDoubleArgs = new InvocationMessage(target, new object[] { ArgumentLength, ArgumentLength, ArgumentLength });
            _invocationMessageProtobufArgs = new InvocationMessage(target, new object[] { protobufObject, protobufObject, protobufObject });
            _invocationMessage = new InvocationMessage(target, new object[] { data, protobufObject, ArgumentLength });
        }

        [Benchmark]
        public void Ping()
        {
            _writer.Clear();
            _hubProtocol.WriteMessage(PingMessage.Instance, _writer);
        }

        [Benchmark(Baseline = true)]
        public void InvocationMessageNoArgument()
        {
            _writer.Clear();
            _hubProtocol.WriteMessage(_invocationMessageNoArgs, _writer);
        }

        [Benchmark]
        public void InvocationMessageStringArguments()
        {
            _writer.Clear();
            _hubProtocol.WriteMessage(_invocationMessageStringArgs, _writer);
        }

        [Benchmark]
        public void InvocationMessageIntArguments()
        {
            _writer.Clear();
            _hubProtocol.WriteMessage(_invocationMessageIntArgs, _writer);
        }

        [Benchmark]
        public void InvocationMessageDoubleArguments()
        {
            _writer.Clear();
            _hubProtocol.WriteMessage(_invocationMessageDoubleArgs, _writer);
        }

        [Benchmark]
        public void InvocationMessageProtobufArguments()
        {
            _writer.Clear();
            _hubProtocol.WriteMessage(_invocationMessageProtobufArgs, _writer);
        }

        [Benchmark]
        public void InvocationMessage()
        {
            _writer.Clear();
            _hubProtocol.WriteMessage(_invocationMessage, _writer);
        }
    }
}
