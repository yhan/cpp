using System.Runtime.InteropServices;

namespace TradingSystem.ProducerConsumerOrderBook.MultipleReaders;

public sealed class MultiReaderBook
{

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
    
    private readonly BookBuffer[] _buffers; // length = readerCount + 2
    private PaddedIndex _published;
    private PaddedIndex[] _readerClaims; // one per reader

    public MultiReaderBook(int readerCount)
    {
        _buffers = GC.AllocateArray<BookBuffer>(readerCount + 2, pinned: true);
        _published.Value = 0;
        _readerClaims = new PaddedIndex[readerCount];
        for (int i = 0; i < readerCount; i++)
            _readerClaims[i].Value = IDLE_READER;
    }

    public const int IDLE_READER = -1;

    public void Publish(ReadOnlySpan<Level> bids, ReadOnlySpan<Level> asks)
    {
        int published = _published.Value;

        // Build the forbidden set by scanning all reader claims.
        // For small readerCount, a bitmask is faster than a HashSet.
        long forbidden = 1L << published;
        for (int i = 0; i < _readerClaims.Length; i++)
        {
            int claim = Volatile.Read(ref _readerClaims[i].Value);
            if (claim != IDLE_READER) forbidden |= 1L << claim;
        }

        // Find the first free buffer.
        int target = -1;
        for (int i = 0; i < _buffers.Length; i++)
        {
            if ((forbidden & (1L << i)) == 0)
            {
                target = i;
                break;
            }
        }
        // target should always be valid if buffer count = readerCount + 2.

        _buffers[target].WriteFrom(bids, asks);
        Volatile.Write(ref _published.Value, target);
    }

    // readerId : already assigned a fix place readerId for each reader thread
    // _readerClaims[0] the first reader use slot 0 ...
    public void Read(int readerId, Span<Level> bidsDest, Span<Level> asksDest,
        out byte bidDepth, out byte askDepth)
    {
        int idx = Volatile.Read(ref _published.Value);
        Volatile.Write(ref _readerClaims[readerId].Value, idx);// dis reader avec id readerId va lire published , publishedId jumps over _buffers to fill it
        _buffers[idx].ReadInto(bidsDest, asksDest, out bidDepth, out askDepth);
        Volatile.Write(ref _readerClaims[readerId].Value, IDLE_READER);
    }

 //   public void Read(Span<Level> bidsDest, Span<Level> asksDest,
 //       out byte bidDepth, out byte askDepth)
 //   {
 //       // you should find the first place _readerClaims[index_to_be_found] == - 1
 //       int idx = Volatile.Read(ref _published.Value);
 //       Volatile.Write(ref _readerClaims[idx].Value, idx); //  Two readers claiming the same buffer share one claim slot. PROBLEM !
 //       _buffers[idx].ReadInto(bidsDest, asksDest, out bidDepth, out askDepth);
 //       Volatile.Write(ref _readerClaims[idx].Value, IDLE_READER);
 //   }
}