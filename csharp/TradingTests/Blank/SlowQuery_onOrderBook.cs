using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Runtime.Intrinsics.Wasm;
using System.Text;

namespace Blank;

/* A  simplified quote book receives timestamped events in non-decreasing time order. ADD creates a bid or ask quote with id, side,
price, quantity, and expiry timestamp. CANCEL removes an active quote. QUERY asks for the best hypothetical fill cost for a BUY or
SELL quantity at the current timestamp. BUY queries consume asks from lowest price upward; SELL queries consume bids from
highest price downward. Queries do not consume liquidity. Quotes with expiry time strictly less than the current timestamp are
inactive. Return -1 if there is not enough active opposite-side quantity.
Input format
• The first line contains N.
• ADD t id side price qty expiry, where side is B for bid or A for ask.
• CANCEL t id.
• QUERY t side qty, where side is BUY or SELL.
• Event timestamps are non-decreasing.
Output format
• For each QUERY, print the minimum BUY cost or maximum SELL proceeds, or -1 if insufficient liquidity.
Constraints
• 1 <= N <= 2 * 10^5
• 1 <= price, qty <= 10^9
• 1 <= id <= 10^9
• Each ADD id is unique.
• Use 64-bit arithmetic.
Input
8
ADD 1 10 A 101 50 5
ADD 2 11 A 100 30 10
QUERY 3 BUY 60
CANCEL 4 11
QUERY 4 BUY 60
ADD 6 12 B 99 40 8
ADD 6 13 B 98 50 20
QUERY 7 SELL 70
Output
6030
-1
6910
*/
public class Level
{
    public Dictionary<int, Quote> Quotes = new();
    public long CumQty;
}

public class SlowQuery_onOrderBook
{
    static void Main(string[] args)
    {
        Dictionary<long, Quote> quoteMap = new(); // from id to quote to get price, access bids/asks
        SortedDictionary<long, Level> bids = new(new BidComparer());
        SortedDictionary<long, Level> asks = new(new AskComparer());
        var fs = new FastScanner();
        int n = fs.NextInt();
        StringBuilder sb = new();
        PriorityQueue<Quote, long> pq = new PriorityQueue<Quote, long>();
        for (int i = 0; i < n; i++)
        {
            string name = fs.Next();
            long now = fs.NextLong();
            // manage expiry
            while (pq.TryPeek(out Quote expired, out long testExp) && testExp < now) // peek O(1)
            {
                pq.Dequeue(); // LOG N
                if (quoteMap.ContainsKey(expired.Id)) // not cancelled, if already cancelled, then already removed.
                {
                    RemoveQuote(expired);
                }
            }
            switch (name)
            {
                case "ADD": // ADD t id side price qty expiry
                {
                    // each ADD id is uniq
                    int id = fs.NextInt();
                    
                    string side = fs.Next(); // B A
                    long price = fs.NextLong();
                    long qty = fs.NextLong();
                    long exp = fs.NextLong();
                    if (exp < now) break; // expired before now ...
                    var quote = new Quote //  ADD t id side price qty expiry
                    {
                        Id = id,
                        Name = name,
                        Ts = now,
                        Side = side, // B A
                        Price = price,
                        Qty = qty,
                        Expiry = exp
                    };
                    
                    quoteMap[quote.Id] = quote;
                    SortedDictionary<long, Level> quotes = quote.Side == "B" ? bids : asks;
                    if (false == quotes.TryGetValue(quote.Price, out var level))
                    {
                        level = new Level { };
                        quotes[quote.Price] = level; // LOG N
                    }
                    level.CumQty += quote.Qty;
                    level.Quotes[quote.Id] = quote;
                    
                    // manage expiry
                    pq.Enqueue(quote, quote.Expiry);

                }
                    break;

                case "CANCEL":
                {
                    //  t id.
                    int id = fs.NextInt();
                    if (quoteMap.TryGetValue(id, out Quote cancel))
                    {
                        RemoveQuote(cancel);

                        // note: can't remove cancel from pq, let expiry to manage it
                    }
                }
                    break;

                case "QUERY": // little ADD REMOVE many QUERY O(n^2) <<----- very bad ! 2 * 10^5 * 2 * 10^5
                {
                    //t side qty
                    string side = fs.Next(); // BUY SELL
                    long qty = fs.NextLong();

                    SortedDictionary<long, Level> quotes = side == "BUY" ? asks : bids;
                    long cost = 0;
                    long rem = qty;
                    foreach (var kv in quotes) // O(n) (special case: all prices are different
                    {
                        long price = kv.Key;
                        Level lvl = kv.Value;
                        if (rem<= lvl.CumQty)
                        {
                            cost += rem * price;
                            sb.AppendLine(cost.ToString());
                            break;
                        }

                        cost += lvl.CumQty * price;
                        rem -= lvl.CumQty;
                    }

                    if (rem > 0) // no sufficient liquidity
                        sb.AppendLine("-1");
                }
                    break;
            }
        }

        return;

        void RemoveQuote(Quote q)
        {
            quoteMap.Remove(q.Id);
            SortedDictionary<long, Level> quotes = q.Side == "B" ? bids : asks;
            Level lvl = quotes[q.Price];
            lvl.Quotes.Remove(q.Id);
            lvl.CumQty -= q.Qty;
            if (lvl.CumQty == 0)
            {
                quotes.Remove(q.Price); // many O(1)
            }
        }
    }
   
}

internal class AskComparer : IComparer<long>
{
    public int Compare(long x, long y)
    {
        return x.CompareTo(y);
    }
}

internal class BidComparer : IComparer<long>
{
    public int Compare(long x, long y)
    {
        return y.CompareTo(x);
        
    }
}

public class Quote
{
    public string Name;
    public long Ts;
    public int Id;
    public string Side;
    public long Qty;
    public long Price;
    public long Expiry { get; set; }
}