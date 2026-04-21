namespace TradingSystem.Fast;

public struct FastDivider
{
    private readonly ulong _magic;
    private readonly int _shift;
    private readonly int _divisor;

    public FastDivider(int divisor)
    {
        _divisor = divisor;
        _shift = 32;
        _magic = ((1UL << _shift) + (ulong)divisor - 1) / (ulong)divisor;
    }

    public int Divide(int n) => (int)((ulong)n * _magic >> _shift);
    public int Modulo(int n) => n - Divide(n) * _divisor;
}