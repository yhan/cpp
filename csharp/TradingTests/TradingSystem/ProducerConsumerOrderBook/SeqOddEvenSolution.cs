using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace TradingSystem.ProducerConsumerOrderBook;

[StructLayout(LayoutKind.Explicit, Size = 640)]
public unsafe struct BookSlot
{
    public const int MaxLevels = 10;
    private const int LevelSize = 16; // sizeof(Level)
    private const int BidsBytes = MaxLevels * LevelSize;
    private const int AsksBytes = MaxLevels * LevelSize;

    // Line 0: front padding — isolates from previous heap object
    [FieldOffset(0)] private fixed byte _padFront[64];

    // Line 1: sequence counter, alone on its line
    [FieldOffset(64)] private long _sequence;
    // (offsets 72..127 are implicit padding — nothing else mapped here)

    // Lines 2+: payload (depths + bids + asks live together)
    [FieldOffset(128)] private byte _bidDepth;
    [FieldOffset(129)] private byte _askDepth;
    // (offsets 130..135: implicit padding for 8-byte alignment of next field)

    [FieldOffset(136)] private fixed byte _bids[BidsBytes]; // 160 bytes
    [FieldOffset(296)] private fixed byte _asks[AsksBytes]; // 160 bytes
    // payload region ends at offset 456

    // Rear padding — isolates from next heap object
    [FieldOffset(512)] private fixed byte _padBack[64];

    /// <summary>
    /// Producer-only: publish a complete book snapshot.
    /// Single writer assumed — no synchronization between producers.
    /// </summary>
    public void Publish(ReadOnlySpan<Level> bids, ReadOnlySpan<Level> asks)
    {
        if (bids.Length > MaxLevels) throw new ArgumentException("Too many bid levels");
        if (asks.Length > MaxLevels) throw new ArgumentException("Too many ask levels");

        long seq = _sequence; // sole writer, plain read OK
        Volatile.Write(ref _sequence, seq + 1); // → odd, release

        _bidDepth = (byte)bids.Length;
        _askDepth = (byte)asks.Length;
        bids.CopyTo(GetBidsBuffer());
        asks.CopyTo(GetAsksBuffer());

        Volatile.Write(ref _sequence, seq + 2); // → even, release
    }

    /// <summary>
    /// Consumer-only: try to read a coherent book snapshot.
    /// Returns false if too many retries — caller should treat as transient.
    /// </summary>
    public bool TryRead(Span<Level> bidsDest, Span<Level> asksDest,
        out byte bidDepth, out byte askDepth,
        int maxAttempts = 16)
    {
        if (bidsDest.Length < MaxLevels) throw new ArgumentException("Bid dest too small");
        if (asksDest.Length < MaxLevels) throw new ArgumentException("Ask dest too small");

        SpinWait sw = default;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            long s1 = Volatile.Read(ref _sequence); // acquire
            if ((s1 & 1) == 1)
            {
                sw.SpinOnce();
                continue;
            }

            byte bd = _bidDepth;
            byte ad = _askDepth;
            GetBidsBuffer().Slice(0, bd).CopyTo(bidsDest);
            GetAsksBuffer().Slice(0, ad).CopyTo(asksDest);

            long s2 = Volatile.Read(ref _sequence); // acquire
            if (s1 == s2)
            {
                bidDepth = bd;
                askDepth = ad;
                return true;
            }

            sw.SpinOnce();
        }

        bidDepth = 0;
        askDepth = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<Level> GetBidsBuffer()
    {
        fixed (byte* p = _bids)
            return new Span<Level>(p, MaxLevels);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<Level> GetAsksBuffer()
    {
        fixed (byte* p = _asks)
            return new Span<Level>(p, MaxLevels);
    }
}