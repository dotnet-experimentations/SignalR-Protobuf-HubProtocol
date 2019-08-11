using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging.Abstractions;
using SignalR.Protobuf.Protocol;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Protobuf.Protocol.Microbenchmarks
{
    [CoreJob]
    [RankColumn]
    [MemoryDiagnoser]
    public class PingSerializationBenchmarks
    {
        private ProtobufHubProtocol _hubProtocol;
        private ArrayBufferWriter<byte> _writer = new ArrayBufferWriter<byte>();

        [GlobalSetup]
        public void Setup()
        {
            var logger = new NullLogger<ProtobufHubProtocol>();
            var types = new[] { typeof(TestMessage) };
            _hubProtocol = new ProtobufHubProtocol(types, logger);
        }

        [Benchmark]
        public void Ping_Clear_Writer()
        {
            _writer.Clear();

            _hubProtocol.WriteMessage(PingMessage.Instance, _writer);
        }
    }
}
