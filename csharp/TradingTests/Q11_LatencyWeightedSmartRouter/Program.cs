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

/*


Q11 — Latency-Weighted Smart Router with Live Reweighting
Difficulty: Hard | 70–90 min
This one is closest in spirit to Q1+Q5 combined.
A router routes child orders. Events:

VENUE venueId latency capacity unitCost — register or update venue properties (capacity is resettable to a new value; latency and unitCost may also change)
ROUTE t qty maxLatency — route qty units across venues with latency ≤ maxLatency, minimizing total cost. 
      Output totalCost and the per-venue allocations sorted by venueId.
    If full quantity cannot be filled, output IMPOSSIBLE and do not consume any capacity.
RESTORE t venueId qty — return qty capacity to a venue (e.g. order rejected upstream)

After each successful ROUTE, used capacity is consumed and not returned automatically.

Edge cases:
venue updates while it has consumed capacity → only the available capacity is affected by the new total (clamped at 0); 
RESTORE for a venue beyond its original capacity is allowed (overflows into a "reserve" that future updates respect);
ties broken by latency then venueId; 
multiple ROUTEs at the same timestamp processed in input order.

Hint: SortedDictionary keyed by (unitCost, latency, venueId);
 on ROUTE, walk the sorted set filtering by latency;
  on update, remove and re-insert the venue. Be very careful that a failed ROUTE leaves zero side-effects — build the allocation plan first, commit only if total qty achievable.

*/
class Solution
{
    class Venue
    {
        public int Id;     
        public int Latency; 
        public long Capacity;  // at restore, += restore_fill_qty, 
        public int UnitCost; 
        public long Consumed; // can accumulate route consumed qty
        public long Available => Math.Max(0, Capacity - Consumed );
        public Venue(int id, int lat, long cap, int cost)
        {
            Id = id; Latency=lat; Capacity=cap; UnitCost=cost;
        }
    }

    public static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
       var fs = new FastScanner();
       var result=new StringBuilder();
        Dictionary<int, Venue> vmap = new (); //venueid => (cost, lat, vid)
        SortedDictionary<(int, int, int), Venue> avaiMap= new(); // (cost, lat, vid) => avai
       while( fs.HasNext() )
       {
         string name=fs.Next();
         switch(name) 
         {
            case "VENUE":  // VENUE venueId latency capacity unitCost
            {
                int id= fs.NextInt(); int lat= fs.NextInt(); long cap= fs.NextLong(); int cost= fs.NextInt();
                
                (int, int, int) oldKey= default;
                if( vmap.TryGetValue(id, out Venue? v) == false)
                { 
                    v = new Venue(id, lat, cap, cost);
                    vmap[v.Id] = v;                
                }
                else {  // remove from sorted map oldkey, add newkey
                    oldKey = (v.UnitCost, v.Latency, v.Id);
                    v.Latency=lat; v.Capacity=cap; v.UnitCost=cost; // vmap is updated
                    avaiMap.Remove(oldKey);
                }
                (int, int, int) newKey = (v.UnitCost, v.Latency, v.Id);
                avaiMap[newKey] = v;                
                
            } break;
            
            case "ROUTE": // t qty maxLatency  // Output totalCost and the per-venue allocations sorted by venueId. If full quantity cannot be filled, output IMPOSSIBLE and do not consume any capacity.
            {
                int ts = fs.NextInt(); long qty = fs.NextLong(); int maxlat = fs.NextInt();
                // will consume Consumed from venues 
                long residual= qty;
                long totcost = 0;
                List<(Venue v, long take)> alloc = new(); // venue id => takeqty
                //bool enough = avaiMa p.Values.Sum(x => x.Available) >=qty; // // BUG counted all venues
                // phase 1: build path
                foreach(var kv in avaiMap)
                {
                    Venue venue = kv.Value;
                    int cost = kv.Key.Item1; int lat = kv.Key.Item2; int vid = kv.Key.Item3;
                    if(lat <= maxlat && kv.Value.Available > 0) // BUG <= not < 
                    {
                        long take=Math.Min(residual, kv.Value.Available);
                        residual -= take;
                        totcost += (long)cost * take; // BUG int overflow 
                        alloc.Add( (venue, take));
                        if(residual == 0) 
                        {
                            break;
                        }
                    }
                }
                
                // phase 2, check and commit
                if(residual > 0)
                {
                    result.AppendLine("IMPOSSIBLE");
                }
                else
                {
                    result.Append(totcost);
                    int cnt = alloc.Count;
                    alloc.Sort((x, y ) => x.v.Id.CompareTo(y.v.Id)); // no allocation sort
                    foreach(var a in alloc) // BUG not sorted by venueid
                    {
                        a.v.Consumed += a.take; // commit !
                        result.Append(' ').Append(a.v.Id).Append('=').Append(a.take); // LESSON LEAERNT: space put on the head
                    }
                    // BUG should append new line, the flow continue
                    result.AppendLine();
                }
            } break;
            
            case "RESTORE": 
            { //RESTORE t venueId qty
                int ts= fs.NextInt(); // not used just fwd reading stream 
                int vid = fs.NextInt();
                long addqty = fs.NextLong();                
                // need just update venue in vmap, the avaiMap points to the same venue
                if(vmap.TryGetValue(vid, out Venue? v)) 
                {
                    // BUG loose RESERVED QTY v.Capacity += addqty;
                    v.Consumed -= addqty;
                }
            } break;
         }
       }
       Console.Write(result);
       
    }
}       


class FastScanner
{
    private readonly byte[] data = new byte[1 << 16];
    private int len, ptr;
    private readonly Stream stdin = Console.OpenStandardInput();
    private bool eof;

    public bool HasNext()
    {
        int c;
        do
        {
            c = Read();
        } while (c >= 0 && c <= 32);

        if (c < 0) return false;
        ptr--; // put it back
        return true;
    }

    private int Read()
    {
        if (eof) return -1;
        if (ptr >= len)
        {
            len = stdin.Read(data, 0, data.Length);
            ptr = 0;
            if (len <= 0)
            {
                eof = true;
                return -1;
            }
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
    public double NextDouble()
    {
        int c;
        do
        {
            c = Read();
        } while (c <= 32 && c >= 0);

        // Handle optional sign
        bool negative = false;
        if (c == '-')
        {
            negative = true;
            c = Read();
        }
        else if (c == '+')
        {
            c = Read();
        }

        // Integer part
        double v = 0;
        while (c > 32 && c != '.' && c != 'e' && c != 'E')
        {
            v = v * 10 + (c - '0');
            c = Read();
        }

        // Fractional part
        if (c == '.')
        {
            c = Read();
            double factor = 0.1;
            while (c > 32 && c != 'e' && c != 'E')
            {
                v += (c - '0') * factor;
                factor *= 0.1;
                c = Read();
            }
        }

        // Exponent part (e.g., 1.5e-3)
        if (c == 'e' || c == 'E')
        {
            c = Read();
            bool expNegative = false;
            if (c == '-')
            {
                expNegative = true;
                c = Read();
            }
            else if (c == '+')
            {
                c = Read();
            }

            int exp = 0;
            while (c > 32)
            {
                exp = exp * 10 + (c - '0');
                c = Read();
            }

            v *= Math.Pow(10, expNegative ? -exp : exp);
        }

        return negative ? -v : v;
    }
    
    public decimal NextDecimal() => decimal.Parse(Next(), CultureInfo.InvariantCulture);
}