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

Q10 — Best-Execution Auditor with Late Acks
Difficulty: Hard | 60–80 min
For each completed order, you must verify the executed price was no worse than the best price available on any venue at the order’s send time (BUY: lowest ask; SELL: highest bid).
Events in non-decreasing timestamp order, but with a wrinkle: ACKs can arrive late (timestamp ≥ send time, up to D units later).
    •    QUOTE t venueId symbol bid ask — venue updates; previous quote replaced
    •    SEND t orderId symbol side qty — order sent at time t
    •    ACK t orderId execPrice — order executed at execPrice (the snapshot to audit is the venue state at the SEND time, not ACK time)
    •    AUDIT t — for every SEND event whose ACK has arrived and whose send-time was ≤ t - D, output orderId verdict where verdict is OK if execPrice was no worse than best, else BAD diff. Each order is reported exactly once across all AUDITs.
Edge cases: an order with no quote on any venue at send time is NO_QUOTE; D can be 0; ACKs may arrive in any order relative to other ACKs as long as their timestamp is ≥ the SEND timestamp.
Hint: snapshot best-by-symbol at SEND time using a per-symbol sorted structure (SortedDictionary or two heaps with lazy deletion); store (symbol, side, snapshotBest) per order; on AUDIT, drain a min-heap of orders keyed by sendTime + D.

*/
class Solution
{


    public static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
       var fs = new FastScanner();
       
       int D = fs.NextInt();  // the max delay to receive an ACK after a SEND 
       /*******STATE********************/
       Dictionary<string, Dictionary<int, (double, double)>> book= new(); // symbol=> innner dico int key is venueid
       
       //SEND
       PriorityQueue<(int, double?, string way), (int, int)> tracking = new(); // (orderid, spapshotprice), prio ts+ orderid( for ts tie)
       
       // ACK
       Dictionary<int, double> acks = new(); //orderid => exec price // TODO can remove once deque
       
       StringBuilder result= new();
       while(fs.HasNext())
       {
          string evt = fs.Next();
          switch(evt)
          {
            case "QUOTE":
            {
                // QUOTE t venueId symbol bid ask — venue updates; previous quote replaced
                int ts = fs.NextInt(); int venue = fs.NextInt(); string sym = fs.Next(); double bid= fs.NextDouble(); double ask =fs.NextDouble();
                if(book.TryGetValue(sym, out var venuebook) == false)
                {
                    venuebook= new Dictionary<int, (double, double)>();
                    book[sym] = venuebook; // BUG new venuebook not assinged to symbol map
                }
                venuebook[venue] = (bid, ask);
            }
            break;
            case "SEND":
            {
                int ts = fs.NextInt(); int orderid = fs.NextInt(); string sym=fs.Next();string way=fs.Next(); _ /*qty ignore*/=fs.NextInt();
                int prio = ts+D;
                // exec <= best
                double? best= BestPrice(sym, book, way);
                tracking.Enqueue((orderid, best, way), (prio, orderid));             
            }
            break;
            case "ACK": // ACK t orderId execPrice 
            {
                int t = fs.NextInt(); // not important, bcz when get order+price from tracking, ack has already arrived
                int orderid=fs.NextInt(); double exec= fs.NextDouble();
                acks[orderid] = exec;
            }
            break;
            case "AUDIT": //output orderId verdict // if AUDIT too early for all orders, no print
            {
                int now =fs.NextInt();
                while(//tracking.Count >0 // BUG redundent
                      tracking.TryPeek(out (int orderid, double? best, string way) order, out (int ackExp, int orderid) sent)
                      && now >= sent.ackExp ) //ack expectation time // BUG <= not <
                {
                    tracking.Dequeue(); //BUG
                    
                    if(order.best == null )
                    {
                        result.AppendLine($"{order.orderid} NO_QUOTE");
                        continue;
                    }
                    double exec= acks[order.orderid];
                    acks.Remove(order.orderid);
                    bool ok = order.way == "BUY" ? exec<= order.best : exec >= order.best;
                    if(ok)
                        result.AppendLine($"{order.orderid} OK");
                    else
                    {
                        var diff= Math.Abs(exec - order.best.Value);
                        result.AppendLine($"{order.orderid} BAD {diff:F2}"); // BUG BAD 0.05 if lucky, BAD 0.04999999999999716 need formatting F2
                    }                    
                }
            }
            break;
          }
       }
       /// BUG result ot printed
       Console.Write(result);
    }
    // negate best for ask, so at comparison can do exec<=best
    static double? BestPrice(string sym, Dictionary<string, Dictionary<int, (double, double)>> book, string side)
    {
        double? best=null;
        if(book.TryGetValue(sym, out var venuebook) == false)
        {
            return null;
        }
        
        switch(side)
        {
            case "BUY": // bestask
            {
                best = venuebook.Values.Min(x => x.Item2);
            }
            break;
            
            case "SELL": // bestbid
            {
                best = venuebook.Values.Max(x => x.Item1);
            }
            break;
        }
        return best;
    }
}
       // at send time, with help of below find best : put in orderid => bestbid( for sell) / bestask (for sell) O(p) price lvls
       //symbol => (venueid => bid, ask)  venueid as key to find the (bid, ask) with O(1)
       // QUOTE: 
       //  update inner dico
       
       
       // SEND
       // store PQ ((orderid, bestprice), sendtime+D as priority)
       //ACK comes back with orderid exec price keep in orderid => execprice
       
       // ACK 
       // update orderid => execprice
       
       
       // AUDIT: t 
       // purge PQ while t > dequeued sendtime+D , compare best vs exec price
       


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