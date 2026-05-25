namespace TradingSystem.ProducerConsumerOrderBook.MultipleReaders;

/// <summary>
///  Read-Copy-Update (RCU):
/// </summary>
public sealed class RcuBook
{
    // The current snapshot is a heap-allocated immutable object.
    private BookSnapshot _current;

    public void Publish(ReadOnlySpan<Level> bids, ReadOnlySpan<Level> asks)
    {
        // Allocate a new snapshot. Writer is the only one who allocates.
        BookSnapshot fresh = new BookSnapshot(bids, asks);

        // Atomic swap of the reference. Old snapshot becomes garbage.
        // Existing readers holding a reference to the old one continue safely;
        // the GC will reclaim it once no reader references it.
        Volatile.Write(ref _current, fresh);
    }

    public BookSnapshot Read()
    {
        // Atomic load of the current snapshot reference.
        // Reader holds it for as long as needed; GC handles cleanup.
        return Volatile.Read(ref _current);
    }
}