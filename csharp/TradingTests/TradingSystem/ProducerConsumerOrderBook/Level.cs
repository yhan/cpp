using System.Runtime.InteropServices;

namespace TradingSystem.ProducerConsumerOrderBook;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct Level
{
    public long Qty;
    public double Price;
}