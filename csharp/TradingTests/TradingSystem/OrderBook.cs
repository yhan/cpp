using System;
using System.Runtime.CompilerServices;

namespace TradingSystem
{
    /// <summary>
    /// Fixed-capacity order book using stack-allocated spans for L2 data.
    /// No heap allocation on update — suitable for hot path.
    /// </summary>
    public sealed class OrderBook
    {
        public const int MaxLevels = 20;

        // Struct of arrays — better cache locality than array of structs
        // Bids: descending by price.  Asks: ascending by price.
        private readonly double[] _bidPrices  = new double[MaxLevels];
        private readonly long[]   _bidSizes   = new long[MaxLevels];
        private readonly double[] _askPrices  = new double[MaxLevels];
        private readonly long[]   _askSizes   = new long[MaxLevels];

        public int BidDepth { get; private set; }
        public int AskDepth { get; private set; }

        // Best bid/ask for quick access
        public double BestBid  => BidDepth > 0 ? _bidPrices[0] : 0;
        public double BestAsk  => AskDepth > 0 ? _askPrices[0] : 0;
        public double Mid      => (BestBid + BestAsk) * 0.5;
        public double Spread   => BestAsk - BestBid;
        public long   BidSize0 => BidDepth > 0 ? _bidSizes[0] : 0;
        public long   AskSize0 => AskDepth > 0 ? _askSizes[0] : 0;

        // ── Setters (called by market data feed) ──

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBid(int level, double price, long size)
        {
            _bidPrices[level] = price;
            _bidSizes[level]  = size;
            if (level >= BidDepth) BidDepth = level + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAsk(int level, double price, long size)
        {
            _askPrices[level] = price;
            _askSizes[level]  = size;
            if (level >= AskDepth) AskDepth = level + 1;
        }

        public void SetDepth(int bidDepth, int askDepth)
        {
            BidDepth = bidDepth;
            AskDepth = askDepth;
        }

        // ── Imbalance signals ──

        /// <summary>Top-of-book imbalance. O(1).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ImbalanceL1()
        {
            double b = _bidSizes[0];
            double a = _askSizes[0];
            double total = b + a;
            return total > 0 ? (b - a) / total : 0.0; // 1 heavy bid  =>  -1 heavy ask
        }

        /// <summary>Multi-level distance-weighted imbalance. O(levels).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Imbalance(int levels = 5)
        {
            int bidLevels = Math.Min(levels, BidDepth);
            int askLevels = Math.Min(levels, AskDepth);

            double wBid = 0, wAsk = 0;
            for (int i = 0; i < bidLevels; i++)
                wBid += _bidSizes[i] / (double)(i + 1);
            for (int i = 0; i < askLevels; i++)
                wAsk += _askSizes[i] / (double)(i + 1);

            double total = wBid + wAsk;
            return total > 0 ? (wBid - wAsk) / total : 0.0;
        }

        /// <summary>
        /// Volume-weighted imbalance with exponential decay by level.
        /// Deeper levels contribute less. O(levels).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ImbalanceDecay(int levels = 5, double decay = 0.7)
        {
            int bidLevels = Math.Min(levels, BidDepth);
            int askLevels = Math.Min(levels, AskDepth);

            double wBid = 0, wAsk = 0;
            double weight = 1.0;
            for (int i = 0; i < bidLevels; i++)
            {
                wBid += _bidSizes[i] * weight;
                weight *= decay;
            }
            weight = 1.0;
            for (int i = 0; i < askLevels; i++)
            {
                wAsk += _askSizes[i] * weight;
                weight *= decay;
            }

            double total = wBid + wAsk;
            return total > 0 ? (wBid - wAsk) / total : 0.0;
        }

        /// <summary>Read-only access to raw arrays for external analysis.</summary>
        public ReadOnlySpan<double> BidPrices => _bidPrices.AsSpan(0, BidDepth);
        public ReadOnlySpan<long>   BidSizes  => _bidSizes.AsSpan(0, BidDepth);
        public ReadOnlySpan<double> AskPrices => _askPrices.AsSpan(0, AskDepth);
        public ReadOnlySpan<long>   AskSizes  => _askSizes.AsSpan(0, AskDepth);

        public void Print(int levels = 5)
        {
            int show = Math.Min(levels, Math.Max(BidDepth, AskDepth));
            Console.WriteLine("  ── Order Book ──");
            for (int i = show - 1; i >= 0; i--)
            {
                string ask = i < AskDepth
                    ? $"{_askSizes[i],8} @ {_askPrices[i]:F2}"
                    : "";
                Console.WriteLine($"  ASK {i}: {ask}");
            }
            Console.WriteLine("  ─────────────────");
            for (int i = 0; i < show && i < BidDepth; i++)
            {
                Console.WriteLine($"  BID {i}: {_bidSizes[i],8} @ {_bidPrices[i]:F2}");
            }
        }
    }
}
