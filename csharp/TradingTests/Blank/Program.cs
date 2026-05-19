using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Text;
using System;


class Event{
    public long T; // timestamp
    public Op Op;
    public long Id;
    public byte Side; // QUOTE B A // QUERY B S
    public long Price;
    public long Qty;
    public long Expiry;
}
enum Op : byte {Add, Cancel, Query }

struct LiveQuote {
    public byte Side;
    public int  Idx;
    public long Qty;
    public long Expiry; public long Cost;
}
class Program
{
    public void Main2(string[] args)
    {
        /*
        QUOTE BID ASK Separated
        objective: cover sided qty to get for buy min notional, for sell max notional
        need scan price with b/a order
        an indexed tree for bid, qty sorted by price desc; ask qty sorted by price asc
        ==> need compress price to index ( price value not important ) 
        
        query is inline with flow 
        you need to update fenwick updating +-qty
        
        cancel/ expiry
        cancel : need a map quoteid=> Quote ... you get qty * notional you can do neg update
        expiry : need lazy dequeue a PQ to remove Quote
        
        KEY STRUCTURE 
        fenwick: qty and notional : frontier residualqty* frontier_price ( index - 1 )
        PQ
        Map id => Quote 
        */
        FastScanner fs=new();
        StringBuilder outSb =new();
        int n = fs.NextInt();
        Event[] events= new Event[n];
        
        // collect prices
        List<long> askPrices = new List<long>();
        List<long> bidPrices = new List<long>();
        
        // collect all events
        for(int i=0; i< n; i++)
        {
            string op=fs.Next();
            Event e = new Event();
            e.T = fs.NextLong();
            
            if(op == "ADD ")
            {
                e.Op = Op.Add;
                e.Id = fs.NextLong();
                e.Side = (byte)fs.Next()[0];
                e.Price = fs.NextLong();
                e.Qty = fs.NextLong();
                e.Expiry = fs.NextLong();
                if(e.Side == (byte)'A') askPrices.Add(e.Price);
                else bidPrices.Add(e.Price);
            }
            else if(op == "CANCEL")
            {
                e.Op = Op.Cancel;
                e.Id = fs.NextLong();
            }
            else { //query
                e.Op = Op.Query;
                e.Side = (byte) (fs.Next()[0]); // B S
                e.Qty = fs.NextLong();
            }
            events[i] = e;
        }
        
        // compressed prices index => price
        long[] askIdxToPrice = Compress(askPrices, ascending: true);
        long[] bidIdxToPrice = Compress(bidPrices, ascending: false);
        
        // once i get price, from cancel or expiry, i need to remove from fenwick => i need index
        Dictionary<long, int> askPriceToIdx = BuildPriceMap(askIdxToPrice);
        Dictionary<long, int> bidPriceToIdx = BuildPriceMap(bidIdxToPrice);
        
        int cnt =askIdxToPrice.Length - 1;
        Fenwick askQty = new Fenwick(cnt);
        Fenwick askCost = new Fenwick(cnt);
        Fenwick bidQty = new Fenwick(cnt);
        Fenwick bidCost = new Fenwick(cnt);
        
        // Quote map for cancel + exp
        Dictionary<long, LiveQuote> live = new Dictionary<long, LiveQuote>();
        // min heap for purge (exp, id), if stale purge on peek
        PriorityQueue<long, long> expHeap= new();
        
        // execute all events
        for(int i=0; i< n; i++)
        {
            Event e=events[i];
            
            // Purge
            // TODO -------------- // when expired remove from live also
            PurgeExpired(e.T, live, expHeap, askQty, askCost, bidQty, bidCost);
            
            if(e.Op == Op.Add)
            {
                int idx; long cost;
                if(e.Side == (byte)'A')
                {
                    idx = askPriceToIdx[e.Price];
                    cost = e.Price * e.Qty;
                    askQty.Update(idx, e.Qty);
                    askCost.Update(idx, cost);
                } else{
                    idx = bidPriceToIdx[e.Price];
                    cost = e.Price * e.Qty;
                    bidQty.Update(idx, e.Qty);
                    bidCost.Update(idx, cost);
                }
                live[e.Id] = new LiveQuote() {  Side = e.Side, Idx = idx, Qty = e.Qty, Expiry = e.Expiry, Cost = cost};
                expHeap.Enqueue(e.Id, e.Expiry);
            }
            else if (e.Op == Op.Cancel )
            {
                if(live.TryGetValue(e.Id, out var q))
                {
                    // Remove shouuld update4 fenwicks need index to price map // purge
                    RemoveLive(e.Id, q, live, askQty, askCost, bidQty, bidCost);
                }    
            } 
            else 
            {
                // query
                // if no enough liquidity, print -1
                // compute cost, 
                Fenwick qF = e.Side == (byte)'B' ? askQty: bidQty;
                Fenwick cF = e.Side == (byte)'B' ? askCost: bidCost;
                // frontier
                long[] idxToPrice = e.Side == (byte)'B' ? askIdxToPrice :bidIdxToPrice;
                long totalActive = qF.Prefix(qF.Size);
            }
        }
        
    }
    
    // remove all expired : among them some may be cancelled
    private static void PurgeExpired(long now, Dictionary<long, LiveQuote> live, PriorityQueue<long, long> expHeap, Fenwick askQty, Fenwick askCost, Fenwick bidQty, Fenwick bidCost) 
    {
        while(expHeap.Count >0 && expHeap.TryPeek(out long id, out long exp) && exp < now)
        {
            expHeap.Dequeue();
            if(live.TryGetValue(id, out LiveQuote q) && q.Expiry == exp ) // defensive TODO 
            {
                RemoveLive(id, q, live, askQty, askCost, bidQty, bidCost );
            }
            // Removed already, dequeued
        }    
    }
    
    private static void RemoveLive(long quoteid, LiveQuote q, Dictionary<long, LiveQuote> live, Fenwick askQty, Fenwick askCost, Fenwick bidQty, Fenwick bidCost)
    {
        if(q.Side ==(byte)'A')
        {
            askQty.Update(q.Idx, -q.Qty);
            askCost.Update(q.Idx, -q.Cost);
        } else {
            
            bidQty.Update(q.Idx, -q.Qty);
            bidCost.Update(q.Idx, -q.Cost);
        }
        live.Remove(quoteid);
    }

    private static Dictionary<long, int> BuildPriceMap(long[] idx2Px)
    {
        Dictionary<long, int> m=new(idx2Px.Length);
        for(int i=1; i<= idx2Px.Length; i++) m[idx2Px[i]] = i;
        return m;        
    }
    
    private static long[] Compress(List<long> prices, bool ascending)
    {
        if(prices.Count==0) return Array.Empty<long>();
        prices.Sort();
        int w=0; //distinct prices count
        for(int r=0; r < prices.Count; r++)
        {
            if(w == 0 || prices[r] != prices[w-1]) prices[w++] = prices[r];
        }
        long[] arr = new long[w+1];
        if(ascending)
        {
            for(int i=1; i<= w; i++) arr[i] = prices[i];
        }
        else{
            for(int i=1; i<= w; i++) arr[i] = prices[w -i -i];
        }
        return arr;
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


internal sealed class Fenwick
{
    private readonly long[] _tree;
    private readonly int _n;

    public Fenwick(int size)
    {
        _n = size;
        _tree = new long[size + 1]; // 1-indexed
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

