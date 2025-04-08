using BenchmarkDotNet.Running;

namespace Pipelink.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<PipelinkBenchmarks>();
    }
}
