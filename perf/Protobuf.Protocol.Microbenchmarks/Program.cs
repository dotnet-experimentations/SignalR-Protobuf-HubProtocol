using BenchmarkDotNet.Running;

namespace Protobuf.Protocol.Microbenchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromTypes(new[] {
                typeof(PingMessageBenchmarks),
                typeof(InvocationMessageBenchmarks),
                typeof(StreamInvocationMessageBenchmarks),
                typeof(StreamItemMessageBenchmarks),
                typeof(CompletionMessageBenchmarks),
                typeof(CancelInvocationMessageBenchmarks),
                typeof(CloseMessageBenchmarks)
            }).Run(args);
            //var summary = BenchmarkRunner.Run<MessageDescriptorBenchmarks>();
        }
    }
}
