using System;
using System.Runtime.InteropServices;
using System.Threading;
using TradingSystem.ProducerConsumerOrderBook;

/// <summary>
/// SPSC double-buffered order book.
/// Producer publishes a complete snapshot; consumer reads the latest one
/// with a single atomic pointer load and zero retries.
///
/// Assumes the consumer is fast enough that the producer cannot complete
/// two publishes during one consumer read. For a fast pricing loop reading
/// a 10-level book, this is comfortably satisfied. If you need formal
/// guarantees against this hazard, extend to triple-buffering.
/// </summary>
public sealed class DoubleBufferedBook
{
    // Two buffers held in pinned heap memory so addresses are stable
    // and cache-aligned access patterns are preserved.
    private readonly BookBuffer[] _buffers;

    // Index of the buffer currently published (0 or 1).
    // Producer-only modifies via PaddedAtomicIndex; consumer reads atomically.
    private PaddedIndex _currentIndex;

    public DoubleBufferedBook()
    {
        // Pinned object heap: GC will not relocate this array, preserving
        // cache alignment and any address assumptions across collections.
        _buffers = GC.AllocateArray<BookBuffer>(2, pinned: true);
        _currentIndex.Value = 0;       // start with buffer 0 as published
    }

    /// <summary>
    /// Producer-only: write a new book snapshot, then atomically publish it.
    /// </summary>
    public void Publish(ReadOnlySpan<Level> bids, ReadOnlySpan<Level> asks)
    {
        if (bids.Length > BookBuffer.MaxLevels) throw new ArgumentException("Too many bid levels");
        if (asks.Length > BookBuffer.MaxLevels) throw new ArgumentException("Too many ask levels");

        // 1. Determine which buffer is currently scratch (the one NOT published).
        //    Producer is the sole writer, so a plain read of _currentIndex.Value is fine.
        int scratch = _currentIndex.Value ^ 1;

        // 2. Write the new book fully into the scratch buffer.
        _buffers[scratch].WriteFrom(bids, asks);

        // 3. Atomic release: flip _currentIndex to point at the freshly-written buffer.
        //    Volatile.Write provides the release barrier — all writes above must be
        //    visible to other threads before this store becomes visible.
        Volatile.Write(ref _currentIndex.Value, scratch);
    }

    /// <summary>
    /// Consumer: read the latest published book snapshot. Never retries.
    /// </summary>
    public void Read(Span<Level> bidsDest, Span<Level> asksDest,
                     out byte bidDepth, out byte askDepth)
    {
        if (bidsDest.Length < BookBuffer.MaxLevels) throw new ArgumentException("Bid dest too small");
        if (asksDest.Length < BookBuffer.MaxLevels) throw new ArgumentException("Ask dest too small");

        // Atomic acquire: this load pairs with the producer's release in Publish.
        // After this, the producer's writes to _buffers[idx] are guaranteed visible.
        int idx = Volatile.Read(ref _currentIndex.Value);

        _buffers[idx].ReadInto(bidsDest, asksDest, out bidDepth, out askDepth);
    }

    /// <summary>
    /// Padded wrapper around the current-buffer index, so the index sits on
    /// its own cache line and is not invalidated alongside other unrelated state.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 128)]
    private struct PaddedIndex
    {
        [FieldOffset(64)] public int Value;
        // 64 bytes of front pad and 60 bytes of trailing pad (with adjacent-line
        // prefetch in mind, total 128) isolate the index from neighbors.
    }
}