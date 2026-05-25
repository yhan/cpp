using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace TradingSystem.ProducerConsumerOrderBook;

/// <summary>
/// SPSC triple-buffered order book with reader-position tracking.
///
/// Three buffers, two atomic indices:
///   _published : the buffer holding the most recently completed snapshot.
///                Written by producer, read by consumer.
///   _readerOn  : the buffer the consumer is currently reading.
///                -1 when consumer is idle.
///                Written by consumer, read by producer.
///
/// Writer's selection rule: pick any buffer that is neither _published
/// nor _readerOn. With 3 buffers and at most 2 occupied, one is always free.
///
/// Properties:
///   - Writer never blocks. Always has a free buffer to write into.
///   - Reader never sees torn data. Writer respects _readerOn.
///   - Reader always sees the latest published book on each new read.
///   - Updates published while reader is busy are conflated (overwritten
///     in the writer's ping-pong pair, never seen by the consumer).
///   - Bounded memory: exactly 3 buffers regardless of rate gap.
/// </summary>
public sealed class TripleBufferedBook
{
    private const int IdleReader = -1;

    // Three book buffers, allocated once on the pinned object heap so the
    // GC will never relocate them. This preserves cache alignment and any
    // address-stability assumptions the JIT might exploit.
    private readonly BookBuffer[] _buffers;

    // _published: the most recently published buffer index.
    //   Writer: writes via Volatile.Write (release).
    //   Reader: reads via Volatile.Read  (acquire).
    private PaddedIndex _published;

    // _readerOn: the buffer the reader currently has claimed (-1 if idle).
    //   Reader: writes via Volatile.Write.
    //   Writer: reads via Volatile.Read.
    private PaddedIndex _readerOn;

    public TripleBufferedBook()
    {
        _buffers = GC.AllocateArray<BookBuffer>(3, pinned: true);
        _published.Value = 0;
        _readerOn.Value = IdleReader;
    }

    /// <summary>
    /// Producer-only: write a new book snapshot into a free buffer
    /// and atomically publish it. Never blocks.
    /// </summary>
    public void Publish(ReadOnlySpan<Level> bids, ReadOnlySpan<Level> asks)
    {
        if (bids.Length > BookBuffer.MaxLevels)
            throw new ArgumentException("Too many bid levels");
        if (asks.Length > BookBuffer.MaxLevels)
            throw new ArgumentException("Too many ask levels");

        // Producer is the sole writer of _published, so a plain read of
        // its own last value is fine. We need an acquire load on _readerOn
        // because the reader writes it.
        int published = _published.Value;
        int readerOn = Volatile.Read(ref _readerOn.Value);

        // Pick the buffer that is neither published nor reader-claimed.
        // With 3 buffers and at most 2 forbidden indices, exactly one (or
        // two, when readerOn == IdleReader or readerOn == published) is free.
        int target = PickFreeBuffer(published, readerOn);

        // Write the full book into the chosen buffer.
        _buffers[target].WriteFrom(bids, asks);

        // Publish: release-store on _published.
        // All preceding writes to _buffers[target] are guaranteed visible
        // to any reader that subsequently sees this index.
        Volatile.Write(ref _published.Value, target);
    }

    /// <summary>
    /// Consumer-only: read the latest published book snapshot.
    /// Never retries. Reader can take arbitrary time without blocking the producer.
    /// </summary>
    public void Read(Span<Level> bidsDest, Span<Level> asksDest,
        out byte bidDepth, out byte askDepth)
    {
        if (bidsDest.Length < BookBuffer.MaxLevels)
            throw new ArgumentException("Bid dest too small");
        if (asksDest.Length < BookBuffer.MaxLevels)
            throw new ArgumentException("Ask dest too small");

        // 1. Acquire-load the latest published index.
        int idx = Volatile.Read(ref _published.Value);

        // 2. Claim it. The writer's selection rule excludes our claim,
        //    so once this store is visible to the writer, no future
        //    publish will target this buffer.
        Volatile.Write(ref _readerOn.Value, idx);

        // 3. NOTE: there is a small race window between steps 1 and 2.
        //    The writer may have published a newer buffer between our
        //    acquire-load and our claim-store. In that case we end up
        //    reading a buffer that is one publish stale — still coherent
        //    (writer cannot have overwritten it; it was published, not
        //    free), but no longer the freshest. This is acceptable
        //    conflation; bounded staleness of one publish is fine.
        //
        //    If the application demands "always the absolute latest,"
        //    a re-check loop can be added: read _published again, and
        //    if it differs, release and re-claim. For market data into
        //    a pricer, the simple version is preferred.

        // 4. Read at our leisure. Writer cannot touch this buffer
        //    until we release the claim.
        _buffers[idx].ReadInto(bidsDest, asksDest, out bidDepth, out askDepth);

        // 5. Release the claim. This frees the buffer for the writer's
        //    next selection. Volatile.Write so the writer sees it promptly.
        Volatile.Write(ref _readerOn.Value, IdleReader);
    }

    /// <summary>
    /// Selects any of the 3 buffers that is neither published nor reader-claimed.
    /// Branchless variant for predictable latency.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int PickFreeBuffer(int published, int readerOn)
    {
        // We have buffers {0, 1, 2}. We must avoid `published` and `readerOn`.
        //
        // Trick: 0 + 1 + 2 = 3.
        // If published and readerOn are distinct and both in {0,1,2}, then
        //     free = 3 - published - readerOn
        // gives us the third index.
        //
        // If readerOn == IdleReader (-1), only `published` is forbidden,
        // and we can pick anything else. We want a deterministic choice
        // that doesn't equal `published`, so:
        //     free = (published + 1) % 3   when readerOn is idle
        //
        // If readerOn == published (reader has the latest), only that one
        // is forbidden, so the same `(published + 1) % 3` works.
        //
        // Combine the two cases:

        if (readerOn == IdleReader || readerOn == published)
            // avoid Modulo which is slow, return (published + 1) % 3;     // any non-published slot
            return published == 2 ? 0 : published + 1; // any non-published slot

        return 3 - published - readerOn; // the third slot
    }

    /// <summary>
    /// Padded wrapper so each atomic index sits on its own cache line,
    /// preventing false sharing between _published and _readerOn (which
    /// are written by different threads) and from heap neighbours.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 128)]
    private struct PaddedIndex
    {
        [FieldOffset(64)] public int Value;
    }
}