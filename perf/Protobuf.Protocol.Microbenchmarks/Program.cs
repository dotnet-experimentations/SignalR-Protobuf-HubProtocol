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
            _ = BenchmarkRunner.Run<StreamInvocationMessageBenchmarks>();
            _ = BenchmarkRunner.Run<StreamItemMessageBenchmarks>();
            _ = BenchmarkRunner.Run<CompletionMessageBenchmarks>();
            _ = BenchmarkRunner.Run<CancelInvocationMessageBenchmarks>();
            _ = BenchmarkRunner.Run<CloseMessageBenchmarks>();
        }
    }
}
