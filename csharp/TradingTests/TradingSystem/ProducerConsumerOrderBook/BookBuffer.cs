using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace TradingSystem.ProducerConsumerOrderBook;



/// <summary>
/// One snapshot buffer. Self-contained, padded.
/// Producer writes via PublishInto; reader copies via CopyTo.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 512)]
public unsafe struct BookBuffer
{
    public const int MaxLevels = 10;
    private const int LevelSize = 16;
    private const int BidsBytes = MaxLevels * LevelSize;
    private const int AsksBytes = MaxLevels * LevelSize;

    [FieldOffset(0)]   private fixed byte _padFront[64];

    [FieldOffset(64)]  private byte _bidDepth;
    [FieldOffset(65)]  private byte _askDepth;
    // 66..71 implicit alignment pad
    [FieldOffset(72)]  private fixed byte _bids[BidsBytes];   // 72..231
    [FieldOffset(232)] private fixed byte _asks[AsksBytes];   // 232..391
    // 392..447 implicit pad
    [FieldOffset(448)] private fixed byte _padBack[64];       // 448..511

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFrom(ReadOnlySpan<Level> bids, ReadOnlySpan<Level> asks)
    {
        _bidDepth = (byte)bids.Length;
        _askDepth = (byte)asks.Length;
        bids.CopyTo(BidsSpan());
        asks.CopyTo(AsksSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadInto(Span<Level> bidsDest, Span<Level> asksDest,
                         out byte bidDepth, out byte askDepth)
    {
        bidDepth = _bidDepth;
        askDepth = _askDepth;
        BidsSpan().Slice(0, bidDepth).CopyTo(bidsDest);
        AsksSpan().Slice(0, askDepth).CopyTo(asksDest);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<Level> BidsSpan()
    {
        fixed (byte* p = _bids) return new Span<Level>(p, MaxLevels);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<Level> AsksSpan()
    {
        fixed (byte* p = _asks) return new Span<Level>(p, MaxLevels);
    }
}