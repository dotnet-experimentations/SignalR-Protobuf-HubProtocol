using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.SignalR.Protocol;
using Google.Protobuf;
using SignalR.Protobuf.Protocol;

namespace Protobuf.Protocol.Microbenchmarks
{
    [CoreJob]
    [RankColumn]
    [MemoryDiagnoser]
    public class MessageDescriptorBenchmarks
    {
        private Memory<byte> _packedPingMessage;

        [Params(1, 10, 100, 1000)]
        public int N;

        private List<ArgumentDescriptor> GetArgumentsDescriptors(int stringSize) =>
            new List<ArgumentDescriptor>() { new ArgumentDescriptor(2, new byte[stringSize]) };


        [GlobalSetup]
        public void Setup()
        {
            _packedPingMessage = MessageDescriptor.PackMessage(HubProtocolConstants.PingMessageType, new byte[N], new List<ArgumentDescriptor>()).ToArray();
        }

        [Benchmark]
        public byte Ping()
        {
            return MessageDescriptor.GetMessageType(_packedPingMessage.Span);
        }

        [Benchmark]
        public byte InvocationMessage()
        {
            //Allocation on purpore here for test
            var protobufInvocationMessage = new InvocationMessageProtobuf();
            var packedInvocationMessage = MessageDescriptor.PackMessage(HubProtocolConstants.InvocationMessageType, protobufInvocationMessage.ToByteArray(), GetArgumentsDescriptors(N));
            return MessageDescriptor.GetMessageType(packedInvocationMessage);
        }
    }
}