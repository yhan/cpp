using System;
using System.Collections.Generic;
using System.Text;

/**
Difficulty: Medium | 35–45 min
Trades arrive as t price qty in non-decreasing time order. After all trades, answer K offline queries of the form RANGE
t1 t2 minNotional: among trades in [t1, t2] whose individual notional (price * qty) is at least minNotional, output the
VWAP (sum of price×qty / sum of qty) rounded down, or -1 if no trade qualifies.
Edge cases: inclusive bounds on both ends; two-trade tie at boundary; integer division semantics; queries are offline so
you can pre-process.
Hint: offline, sort queries by minNotional descending, sort trades by notional descending, use a Fenwick tree indexed on
time for prefix sums of qty and qty×price.

INPUT FORMAT

trades
N      
ts  price qty  consider price qty can be int

K queries 
t1  t2 minnominal


1 ≤ N ≤ 2 × 10^5 — number of trades
1 ≤ K ≤ 2 × 10^5 — number of range queries
1 ≤ t ≤ 10^9

1 ≤ price ≤ 10^6
1 ≤ qty ≤ 10^4

1 ≤ t1 ≤ t2 ≤ 10^9
0 ≤ minNotional ≤ 10^10 

*/

class Trade {
    public int Ts; 
    public int Price; public int Qty;
    public long Notional; // BUGfix 3: int overflow : qty price both int when do long notional=qty*price the code first multiply to int then convert to long; the multiplication can overflow
                          // BUGFix: perf should be stored once
    public int TsIndex;
}

class Query{
    public int T1; public int T2; public long MinNotional; public int Idx;
}

class Solution
{
    static void Main(string[] args)
    {
        var fs = new FastScanner();
        int n = fs.NextInt();
        Trade[] trades =  new Trade[n];
        
        int[] times = new int[n + 1]; // BUGFix should be trade+1 bcz fenwick is 1 indexed
        int prevTs = 0 ;// min ts >=1
        int tscount = 0;
        for(int i=0; i< n; i++) // build timestamp => index
        {
            int ts =  fs.NextInt(); // BUGfix 1: read ts twice
            if(ts > prevTs )    // fenwick is 1 indexed, first index is 1
                tscount++;
            int px =fs.NextInt(); int qty= fs.NextInt();
            trades[i] = new Trade(){ Ts = ts, Price = px, Qty=qty, TsIndex = tscount, Notional = (long)qty*px };            
            times[tscount] = ts;
            prevTs = ts;
        }

        int k = fs.NextInt();
        Query[] queries =  new Query[k];
        for(int i=0; i <k ; i++)
        {
            queries[i] = new Query{T1= fs.NextInt(), T2= fs.NextInt(), MinNotional= fs.NextLong(), Idx= i};
        }
        // I need a fenwick qty/notional which retains ONLY query min_notional trades. Queries are sorted desc, so far query has already bigger trades ingested in fenwick. Good.
        Array.Sort(queries, (x, y )=> y.MinNotional.CompareTo(x.MinNotional));
        Array.Sort(trades, (x, y) => (x.Notional).CompareTo(y.Notional) ); // asc sorted notional

        // fenwick should be implicily timestamp sorted (asc), so compress ts to index first
        // when push trade qty price to fenwicks, should know ts's index first 
        //int nextLen = n - 1;
        //int end = n -1;
        Fenwick qtyFen = new Fenwick(tscount); // tscount is distinct timestamp
        Fenwick notionalFen = new Fenwick(tscount); // tsindex is distinct timestamp
        long[] vwaps = new long[k]; // index is query intial index // BUGFix VWAP is rounded down, so should be long not double 
        int tp = trades.Length - 1;
        for( int i=0; i< k; i++) // iterate through queries
        {
            var q = queries[i];
            long minNotional = q.MinNotional; // i want trades notional >= min_notional

            //bool atLeastOneTrade=false; BUGFix: per query, if for this query bottomed by minNontional, for this query inserted, does not mean the query can't find trades ...
            while(tp >=0 && trades[tp].Notional >= minNotional) // BUGFix >= instead of >
            {
                var trad = trades[tp];
                qtyFen.Update(trad.TsIndex, trad.Qty);
                notionalFen.Update( trad.TsIndex, trad.Notional);
                tp--;
            }
            // query t1 ..t2
            int lo = LowBound(times, q.T1, tscount); // BUGFix lo hi : can't search t1 t2 in ts map
            int hi = HighBound(times, q.T2, tscount);
            if(lo> hi)  
            {
                /* 1. both t1 t2 < min
                 * 2. both t1 t2 > max
                 * 3. window falls in a gap between consecutive distinct timestamps
                 *    ex:
                 *    times[1..4] = [10, 20, 30, 40]
                      Query: t1=22, t2=28   => lo = 3 > hi = 2 
                 */
                vwaps[q.Idx] = -1;
                continue;
            }
            long rangeQty = qtyFen.Range(lo, hi);
            
            long vwap = rangeQty == 0 ? - 1 : notionalFen.Range(lo, hi) /  qtyFen.Range(lo, hi); // BUGFix rangeqty can be 0, no trades fall into window
            vwaps[q.Idx] = vwap;
        }

        StringBuilder sb = new();
        for( int i =0; i< vwaps.Length; i++)
        {
            sb.AppendLine(vwaps[i].ToString()); 
        }
        Console.WriteLine(sb);
    }
    private static int LowBound(int[] arr, int lowbound, int timesLen) // look for index value >= lowbound
    {
        var test = Array.BinarySearch(arr, 1, timesLen, lowbound);
        if (test >= 0) return test;
        return ~test;
    }

    private static int  HighBound(int[] arr, int hibound, int timesLen) // look for index value <= hibound
    {
        var test = Array.BinarySearch(arr,  1, timesLen, hibound);
        if (test >= 0) return test;
        return ~test - 1;
    }
}

internal sealed class Fenwick
{
    private readonly long[] _tree;
    private readonly int _n;

    public Fenwick(int size)
    {
        _n = size;
        _tree = new long[size + 1];   // 1-indexed
    }

    public int Size => _n;

    // Point update: tree[i] += delta. O(log n).
    public void Update(int i, long delta) // LOG N
    {
        for (; i <= _n; i += i & -i)
            _tree[i] += delta;
    }

    // Prefix sum tree[1..i]. O(log n).
    public long Prefix(int i) // LOG N
    {
        long sum = 0;
        for (; i > 0; i -= i & -i)
            sum += _tree[i];
        return sum;
    }

    // Range sum tree[l..r] inclusive. O(log n).
    public long Range(int l, int r) // LOG N
    {
        if (r < l) return 0;
        return Prefix(r) - Prefix(l - 1);
    }

    // Fenwick descent: smallest index k such that Prefix(k) >= target.
    // Returns -1 if total sum < target. O(log n).
    // Requires all stored values to be non-negative — true here since we
    // only store active qty (cancellations zero out the entry, never go negative on a prefix).
    public int LowerBound(long target) // LOG N
    {
        if (target <= 0) return 0;

        int idx = 0;
        long acc = 0;

        // Largest power of two <= _n.
        int bit = 1;
        while ((bit << 1) <= _n) bit <<= 1;

        for (; bit > 0; bit >>= 1)
        {
            int next = idx + bit;
            if (next <= _n && acc + _tree[next] < target)
            {
                idx = next;
                acc += _tree[next];
            }
        }

        int result = idx + 1;
        return result > _n ? -1 : result;
    }
}

class FastScanner
{
    private readonly byte[] data = new byte[1 << 16];
    private int len, ptr;

    private int Read()
    {
        if (ptr >= len)
        {
            len = Console.OpenStandardInput().Read(data, 0, data.Length);
            ptr = 0;
            if (len
                == 0) return -1;
        }

        return data[ptr++];
    }

    public string Next()
    {
        int c;
        do
        {
            c = Read();
        } while (c <= 32 && c >= 0);

        var chars = new List<char>();
        while (c > 32)
        {
            chars.Add((char)c);
            c = Read();
        }

        return new string(chars.ToArray());
    }

    public int NextInt() => (int)NextLong();

    public long NextLong()
    {
        int c;
        do
        {
            c = Read();
        } while (c <= 32 && c >= 0);

        long v = 0;
        while (c > 32)
        {
            v = v * 10
                + c - '0';
            c = Read();
        }

        return v;
    }
}
