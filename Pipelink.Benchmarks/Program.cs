using BenchmarkDotNet.Running;
using Pipelink.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<MediatorBenchmarks>();
    }
}
