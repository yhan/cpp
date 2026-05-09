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

class Event
{
    public string Name; 
    public long Ts; 
    public int Id; 
    public string Side;
    public long Price; 
    public long Qty;
    public long Expiry;
    
    public Event(string name, long ts, int id, string side, long price, long qty, long expiry)
    {
        Name=name;
        Ts=ts; Id=id; Side=side; Price=price; Qty=qty; Expiry= expiry;
    }
    public Event(string name, long ts, int id) // for cancel
    {
        Name=name;
        Ts=ts; Id=id; 
    }
    public Event(string name, long ts, string side, long qty)
    {
        Name=name;
        Ts=ts; Side=side; Qty=qty; 
    }
}
/*
 -----------------------------
 *  Retro :
 * ---------------------------------------
 * PQ signature
 * PriorityQueue<Event, long> pq = new PriorityQueue<Event, long>(); // contains quote memory sorted by expiry
pq.TryPeek( out Event expired, out long exp)
 pq.Enqueue(e, e.Expiry);

Cancelled : should decremen fenwick & remove from quotes
but PQ still contains the cancelled quote
PQ managing expiry, Cancelled events still there
should decrement only if quotes still contains it
Just dequeue it : cancelled & expiry occurs


TryPeak does not dequeue, dont forget

sign is important +1 / -1

fenwick is 1-indexed, corresponding index to price, price to index mapping should be careful

Struggnled a lot on this
pcnt is distinct price count
sortedPx is 0-indexed sorted prices ( asc )
  for(int i = 0; i< pcnt; i++)
        {
            long p = sortedPx[i]; 
            int bidi = pcnt - i;
            int aski = i + 1;
            
            bidIdxToPx[bidi] = p;// higher index has low value
            askIdxToPx[aski] = p;
            askp2i[p] = aski;
            bidp2i[p] = bidi;
        }


 * 
 */
class Solution
{
    public static void Main(string[] args) // Required: Time: O(N log P), where P is the number of distinct prices. Space: O(N + P).
    {
        // read input 
        var fs = new FastScanner();
        var n = fs.NextInt(); //nb pf events
        Event[] evts = new Event[n];
        HashSet<long> prices = new();
        for(int i=0; i< n; i++)
        {
            var evtname = fs.Next();
            long ts = fs.NextLong();
            switch (evtname)
            {
                case "ADD": // ADD t id side price qty expiry, 
                {                 
                    int id = fs.NextInt();  string side = fs.Next(); long price = fs.NextLong();
                    long qty = fs.NextLong(); long exp =  fs.NextLong();
                    evts[i] = new Event(evtname, ts, id, side, price, qty, exp);
                    prices.Add(price);   
                }
                break;
                
                case "CANCEL":
                  evts[i] = new Event(evtname, ts, fs.NextInt());
                break;
                
                case "QUERY":
                  evts[i] = new Event(evtname,  ts,  fs.Next(),  fs.NextLong());
                break;
            }
        }
        
        // fenwick bids qty, ask qties, bid notional, ask notional
        // I can know constantly for a query qty, for query QTY, till which price should i walk to 
        // I have to read all prices to get the fenwick compressed index structure
        int pcnt = prices.Count;
        Fenwick bidQties = new Fenwick(pcnt);
        Fenwick askQties = new Fenwick(pcnt);
        Fenwick bidNotionals = new Fenwick(pcnt);
        Fenwick askNotionals = new Fenwick(pcnt);
        
        long[] sortedPx = prices.OrderBy(x => x).ToArray();
        // bid 
        // sorted price
        long[] askIdxToPx = new long[pcnt + 1];
        long[] bidIdxToPx = new long[pcnt + 1];
        Dictionary<long, int> askp2i = new();
        Dictionary<long, int> bidp2i = new();
        for(int i = 0; i< pcnt; i++)
        {
            int bidi = pcnt - i; int aski = i + 1;
            long p = sortedPx[i]; 
            bidIdxToPx[bidi] = p;// higher index has low value
            askIdxToPx[aski] = p;
            askp2i[p] = aski;
            bidp2i[p] = bidi;
        }
        
        StringBuilder sb=new();
        Dictionary<int, Event> quotes = new();
        PriorityQueue<Event, long> pq = new PriorityQueue<Event, long>(); // contains quote memory sorted by expiry
        foreach (Event e in evts)
        {
            // MANAGE EXPIRY
            long now = e.Ts;
            while(pq.TryPeek( out Event expired, out long exp) && exp < now)
            {
                if(quotes.ContainsKey(expired.Id)) // if not cancelled .
                    AddOrRemove(expired, -1);
                
                pq.Dequeue();
            }
            
            switch(e.Name)
            {
                case "ADD":
                if( e.Expiry < now) break; // event expiry at arrival is ealier than now 
                 pq.Enqueue(e, e.Expiry); // just enqueued : ts == now, can be expired
                 AddOrRemove(e, +1);
                 break;
                
                case "CANCEL":
                 if(quotes.TryGetValue(e.Id, out Event quote) == false)
                 {
                    break;
                 }
                 AddOrRemove(quote, -1);
                 break;
                
                case "QUERY":
                // query qty to find in qty fenwick  price index
                 Query(e, askQties, bidQties, askNotionals, bidNotionals, askIdxToPx, bidIdxToPx, sb);
                 break;
            }
        }
        
        void AddOrRemove(Event q, int sign) // only quote 
        {
             if(sign > 0 ) 
                quotes[q.Id] = q; // fix for duplicated add event
             else
             {
                quotes.Remove(q.Id);
             }
                
             // update fenwick ...  find price , find index
             Fenwick qfen = q.Side == "B" ? bidQties : askQties;
             Fenwick nfen = q.Side == "B" ? bidNotionals : askNotionals;
             Dictionary<long, int> p2i = q.Side == "B" ? bidp2i: askp2i;
             int i = p2i[q.Price];
             qfen.Update(i, sign * q.Qty);
             nfen.Update(i, sign * q.Qty * q.Price);
        }
    }

    private static void Query(Event e, Fenwick askQties, Fenwick bidQties, Fenwick askNotionals, Fenwick bidNotionals, long[] askIdxToPx, long[] bidIdxToPx, StringBuilder sb)
    {
        long targetqty = e.Qty;
        Fenwick qtyFen = e.Side == "BUY" ? askQties : bidQties;
        Fenwick notionalFen = e.Side == "BUY" ? askNotionals : bidNotionals;
        long[] i2p = e.Side == "BUY" ? askIdxToPx: bidIdxToPx; 
                 
                 
        {
            int idx = qtyFen.LowerBound(targetqty);
            if(idx == -1 )
            {
                sb.AppendLine("-1"); // no enough liquidity
                return;
            }
            long cumqty = qtyFen.Prefix(idx);
                    
            if(targetqty == cumqty) // For each QUERY, print the minimum BUY cost or maximum SELL proceeds, or -1 if insufficient liquidity.
            {
                long cost = notionalFen.Prefix(idx); // cost
                sb.AppendLine(cost.ToString());
            }
            else // cumqty > targetqty
            {                        
                int loidx = idx - 1; // when index is 1 : nottional & qty fetched from fenwick is  0, so residual = targetqty
                long notional1 = notionalFen.Prefix(loidx);
                long qty1 = qtyFen.Prefix(loidx);
                long residual = targetqty - qty1;
                long notional2 = residual * i2p[idx];
                sb.AppendLine((notional1 + notional2).ToString());                        
            }
        }
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