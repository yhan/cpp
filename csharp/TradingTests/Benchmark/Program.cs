using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using TradingSystem.Fast;

BenchmarkRunner.Run<DividerBenchmark>();

[MemoryDiagnoser]
[DisassemblyDiagnoser]
public class DividerBenchmark
{
    private const int Divisor = 7;
    private static readonly FastDivider FastDiv = new FastDivider(Divisor);

    // Use a volatile field to prevent the JIT from optimizing away the work
    private int[] _data = null!;

    [Params(1000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _data = new int[Count];
        for (int i = 0; i < Count; i++)
            _data[i] = rng.Next(1, 1_000_000);
    }

    [Benchmark(Baseline = true)]
    public int NativeDivide()
    {
        int sum = 0;
        foreach (var n in _data)
            sum += n / Divisor;
        return sum;
    }

    [Benchmark]
    public int FastDivide()
    {
        int sum = 0;
        foreach (var n in _data)
            sum += FastDiv.Divide(n);
        return sum;
    }

    [Benchmark]
    public int NativeModulo()
    {
        int sum = 0;
        foreach (var n in _data)
            sum += n % Divisor;
        return sum;
    }

    [Benchmark]
    public int FastModulo()
    {
        int sum = 0;
        foreach (var n in _data)
            sum += FastDiv.Modulo(n);
        return sum;
    }
}
