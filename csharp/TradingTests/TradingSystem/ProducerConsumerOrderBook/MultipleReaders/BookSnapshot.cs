namespace TradingSystem.ProducerConsumerOrderBook.MultipleReaders;

public sealed class BookSnapshot // immutable
{
    public readonly Level[] Bids;
    public readonly Level[] Asks;
    public readonly byte BidDepth;
    public readonly byte AskDepth;

    public BookSnapshot(ReadOnlySpan<Level> bids, ReadOnlySpan<Level> asks)
    {
        Bids = bids.ToArray();
        Asks = asks.ToArray();
        BidDepth = (byte)bids.Length;
        AskDepth = (byte)asks.Length;
    }
}