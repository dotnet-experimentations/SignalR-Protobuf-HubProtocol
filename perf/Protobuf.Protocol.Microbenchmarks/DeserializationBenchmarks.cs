using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using SignalR.Protobuf.Protocol;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Protobuf.Protocol.Microbenchmarks
{
    [MemoryDiagnoser]
    [Config(typeof(Config))]
    [SimpleJob(RunStrategy.Throughput, launchCount: 4,
        warmupCount: 3, targetCount: 100, id: "Deserialization")]
    public class DeserializationBenchmarks
    {
        private ProtobufHubProtocol _hubProtocol;
        private byte[] _pingMessage;
        private byte[] _invocationMessageNoArgs;
        private byte[] _invocationMessageStringArgs;
        private byte[] _invocationMessageIntArgs;
        private byte[] _invocationMessageDoubleArgs;
        private byte[] _invocationMessageProtobufArgs;
        private byte[] _invocationMessageArgs;
        private ArrayBufferWriter<byte> _writer = new ArrayBufferWriter<byte>();

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

        [Params(/*20 , 50,*/ 100)]
        public int TargetLength { get; set; }

        [Params(/*100, 300, 500, 1000, 5000,*/ 10000)]
        public int ArgumentLength { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            var logger = new NullLogger<ProtobufHubProtocol>();
            var types = new[] { typeof(BenchMessage) };
            _hubProtocol = new ProtobufHubProtocol(types, logger);

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

            var invocationMessageNoArgs = new InvocationMessage(target, Array.Empty<object>());
            var invocationMessageStringArgs = new InvocationMessage(target, new object[] { data, data, data });
            var invocationMessageIntArgs = new InvocationMessage(target, new object[] { ArgumentLength, ArgumentLength, ArgumentLength });
            var invocationMessageDoubleArgs = new InvocationMessage(target, new object[] { ArgumentLength, ArgumentLength, ArgumentLength });
            var invocationMessageProtobufArgs = new InvocationMessage(target, new object[] { protobufObject, protobufObject, protobufObject });
            var invocationMessage = new InvocationMessage(target, new object[] { data, protobufObject, ArgumentLength });


            _writer.Clear();
            _hubProtocol.WriteMessage(PingMessage.Instance, _writer);
            _pingMessage = _writer.WrittenSpan.ToArray();
            _writer.Clear();
            _hubProtocol.WriteMessage(invocationMessageNoArgs, _writer);
            _invocationMessageNoArgs = _writer.WrittenSpan.ToArray();
            _writer.Clear();
            _hubProtocol.WriteMessage(invocationMessageStringArgs, _writer);
            _invocationMessageStringArgs = _writer.WrittenSpan.ToArray();
            _writer.Clear();
            _hubProtocol.WriteMessage(invocationMessageIntArgs, _writer);
            _invocationMessageIntArgs = _writer.WrittenSpan.ToArray();
            _writer.Clear();
            _hubProtocol.WriteMessage(invocationMessageDoubleArgs, _writer);
            _invocationMessageDoubleArgs = _writer.WrittenSpan.ToArray();
            _writer.Clear();
            _hubProtocol.WriteMessage(invocationMessageProtobufArgs, _writer);
            _invocationMessageProtobufArgs = _writer.WrittenSpan.ToArray();
            _writer.Clear();
            _hubProtocol.WriteMessage(invocationMessage, _writer);
            _invocationMessageArgs = _writer.WrittenSpan.ToArray();
        }

        [Benchmark]
        public void Ping()
        {
            var message = new ReadOnlySequence<byte>(_pingMessage);
            _hubProtocol.TryParseMessage(ref message, null, out var _);
        }

        [Benchmark(Baseline = true)]
        public void InvocationMessageNoArgs()
        {
            var message = new ReadOnlySequence<byte>(_invocationMessageNoArgs);
            _hubProtocol.TryParseMessage(ref message, null, out var _);
        }

        [Benchmark]
        public void InvocationMessageStringArgs()
        {
            var message = new ReadOnlySequence<byte>(_invocationMessageStringArgs);
            _hubProtocol.TryParseMessage(ref message, null, out var _);
        }

        [Benchmark]
        public void InvocationMessageIntArgs()
        {
            var message = new ReadOnlySequence<byte>(_invocationMessageIntArgs);
            _hubProtocol.TryParseMessage(ref message, null, out var _);
        }

        [Benchmark]
        public void InvocationMessageDoubleArgs()
        {
            var message = new ReadOnlySequence<byte>(_invocationMessageDoubleArgs);
            _hubProtocol.TryParseMessage(ref message, null, out var _);
        }

        [Benchmark]
        public void InvocationMessageProtobufArgs()
        {
            var message = new ReadOnlySequence<byte>(_invocationMessageProtobufArgs);
            _hubProtocol.TryParseMessage(ref message, null, out var _);
        }

        [Benchmark]
        public void InvocationMessageArgs()
        {
            var message = new ReadOnlySequence<byte>(_invocationMessageArgs);
            _hubProtocol.TryParseMessage(ref message, null, out var _);
        }
    }
}
