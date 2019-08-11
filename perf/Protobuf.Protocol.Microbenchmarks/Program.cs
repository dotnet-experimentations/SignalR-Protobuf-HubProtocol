using BenchmarkDotNet.Running;

namespace Protobuf.Protocol.Microbenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<MessageDescriptorBenchmarks>();
            _ = BenchmarkRunner.Run<PingMessageBenchmarks>();
            _ = BenchmarkRunner.Run<InvocationMessageBenchmarks>();
        }
    }
}
