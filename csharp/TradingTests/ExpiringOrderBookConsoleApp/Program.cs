/*
 * Question 5 — Expiring Quote Book Queries
Difficulty: Hard | Recommended timing: 75-100 minutes
A simplified quote book receives timestamped events in non-decreasing time order. ADD creates a bid or ask quote with id, side,
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
Sample cases
Sample 1
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
6030 -1
6910

Edge cases to watch
• Expiry is strict: expiry < current time means inactive; expiry == current time is still active.
• CANCEL for an inactive or unknown quote has no effect.
• Queries are hypothetical and must not reduce book quantity.
• Prices must be compressed before Fenwick tree indexing.
 */

using System.Text;

public class Program
{
    private const string BID = "B";
    private const string ASK = "A";
    private const string ADD = "ADD";
    private const string CANCEL = "CANCEL";
    private const string QUERY = "QUERY";
    private const string BUY = "BUY";
    private const string SELL = "SELL";


    public static void Main(string[] args)
    {
        FastScanner fs = new FastScanner();
        int n = fs.NextInt(); // n events total
        var (allEvents, prices) = AllEvents(n, fs);
        int pricesCount = prices.Count; // the fenwick internal long array contains at most pricesCount slots
        
        PriorityQueue<int, long> pq = new PriorityQueue<int, long>(Comparer<long>.Default);
        
        Fenwick askQtyFen = new Fenwick(pricesCount); // sum of qty at ask price levels [1..i]
        Fenwick askNotionalFen = new Fenwick(pricesCount); // sum of (qty × price) at ask price levels [1..i]

        Fenwick bidQtyFen = new Fenwick(pricesCount); // sum of qty at bid price levels [1..i] (reversed)
        Fenwick bidNotionalFen = new Fenwick(pricesCount); // sum of (qty × price) at bid price levels [1..i] (reversed)

        // for BUY query (with qty) compute the lowest cost ( should look at ASK side of book)
        // build Fenwick structure, Update it when 1 ADD/CANCEL comes in; on each QUERY, lazy manage Expiry ( should be sorted nearest to farest )
        // the Fenwick has P+2 long slots, P is distinct price
        // Bid fenwik higher price lower index, ASK fenwick lower price lower index
        // build price to index dico, two dicos 
        var (bidIdx2Price, bidPxToIdx, askIdx2Price, askPxToIdx ) = BuildPriceToIndex(prices);
        StringBuilder sb = new StringBuilder();
        Dictionary<int, Event> eventsMap = new Dictionary<int, Event>();
        for (int i = 0; i < allEvents.Length; i++) // treate all event sequentially, so query get "at that moment" info
        {
            Event evt = allEvents[i];
            ManageExpiry(pq, evt, eventsMap, bidQtyFen, bidNotionalFen, askQtyFen, askNotionalFen, bidPxToIdx, askPxToIdx);

            switch (evt.EventType)
            {
                case ADD:
                {
                    long now = evt.Timestamp;
                    if (evt.Expiry < now) break; // dead on arrival, skip Fenwick update too
                    eventsMap[evt.QuoteId] = evt;
                    int idx = 0;
                    switch (evt.Side)
                    {
                        case BID:
                            idx = bidPxToIdx[evt.Price];
                            bidQtyFen.Update(idx, evt.Qty);
                            bidNotionalFen.Update(idx, evt.Qty * evt.Price);
                            break;
                        case ASK:
                            idx = askPxToIdx[evt.Price];
                            askQtyFen.Update(idx, evt.Qty);
                            askNotionalFen.Update(idx, evt.Qty * evt.Price);
                            break;
                    }

                    pq.Enqueue(evt.QuoteId, evt.Expiry);
                    break;
                }
                case CANCEL:
                {
                    // cancel on inactive or non existing should not break
                    if (eventsMap.TryGetValue(evt.QuoteId, out Event? cancelled))
                    {
                        RemoveExpiredOrCancelled(eventsMap, cancelled, bidPxToIdx, bidQtyFen, bidNotionalFen, askPxToIdx, askQtyFen, askNotionalFen);
                    }

                    break;
                }
                // objective is to find the cumCost
                case QUERY:
                {
                    Fenwick qtyFen = evt.Side == BUY ? askQtyFen : bidQtyFen;
                    Fenwick notionalFen = evt.Side == BUY ? askNotionalFen : bidNotionalFen;
                    // index at which cumqty >= query qty
                    var queryQty = evt.Qty;
                    long totalQty = qtyFen.QueryByIndex(pricesCount);
                    var (marginalPriceIndex, accQty) = qtyFen.LowerBound(evt.Qty); // marginalPriceIndex is the lowerest index which covers query qty
                    if (totalQty < queryQty) sb.AppendLine("-1");
                    else
                    {
                        long residualQty = queryQty - accQty;
                        long notional = notionalFen.QueryByIndex(marginalPriceIndex - 1);
                        Dictionary<int, long> idx2Px = evt.Side == BUY ? askIdx2Price : bidIdx2Price;
                        notional += residualQty * idx2Px[marginalPriceIndex];
                        sb.AppendLine(notional.ToString());
                    }

                    break;
                }
            }
        }

        Console.Write(sb);
    }

    private static void ManageExpiry(PriorityQueue<int, long> pq, Event evt, Dictionary<int, Event> eventsMap,
        Fenwick bidQtyFen, Fenwick bidNotionalFen, 
        Fenwick askQtyFen, Fenwick askNotionalFen, 
        Dictionary<long, int> bidPxToIdx,
        Dictionary<long, int> askPxToIdx)
    {
        long now = evt.Timestamp;
        while (pq.TryPeek(out int quoteId, out long expiry) && expiry < now)
        {
            int expiredQuoteId = pq.Dequeue();
            if (eventsMap.TryGetValue(expiredQuoteId, out Event? expired))
            {
                RemoveExpiredOrCancelled(eventsMap, expired, bidPxToIdx, bidQtyFen, bidNotionalFen, askPxToIdx, askQtyFen, askNotionalFen);
            }
        }
    }

    private static (Dictionary<int, long> bidIdx2Price, Dictionary<long, int> bidPxToIdx, Dictionary<int, long> askIdx2Price, Dictionary<long, int> askPxToIdx) BuildPriceToIndex(HashSet<long> prices) // build price to index in fenwick
    {
        Dictionary<long, int> askPxToIdx = new Dictionary<long, int>();
        Dictionary<long, int> bidPxToIdx = new Dictionary<long, int>();
        Dictionary<int, long> bidIdx2Price = new Dictionary<int, long>();
        Dictionary<int, long> askIdx2Price = new Dictionary<int, long>();
        int index = 1;
        foreach (var p in prices.OrderBy(x => x))
        {
            int askIdx = index;
            int bidIdx = prices.Count - index + 1; // 1 based array, the first slot 0 is not used
            askPxToIdx.Add(p, askIdx);
            bidPxToIdx.Add(p, bidIdx);

            askIdx2Price.Add(askIdx, p);
            bidIdx2Price.Add(bidIdx, p);

            index++;
        }

        return (bidIdx2Price, bidPxToIdx, askIdx2Price, askPxToIdx);
    }

    private static void RemoveExpiredOrCancelled(Dictionary<int, Event> eventsMap, Event cancelled, Dictionary<long, int> bidPxToIdx, Fenwick bidQtyFen, Fenwick bidNotionalFen, Dictionary<long, int> askPxToIdx, Fenwick askQtyFen, Fenwick askNotionalFen)
    {
        int idx;
        if (cancelled.Side == BID)
        {
            idx = bidPxToIdx[cancelled.Price];
            bidQtyFen.Update(idx, -cancelled.Qty);
            bidNotionalFen.Update(idx, -cancelled.Qty * cancelled.Price);
        }
        else if (cancelled.Side == ASK)
        {
            idx = askPxToIdx[cancelled.Price];
            askQtyFen.Update(idx, -cancelled.Qty);
            askNotionalFen.Update(idx, -cancelled.Qty * cancelled.Price);
        }

        eventsMap.Remove(cancelled.QuoteId);
    }

    private static (Event[] events, HashSet<long>) AllEvents(int n, FastScanner fs)
    {
        Event[] events = new Event[n];
        HashSet<long> prices = new();
        // need also cancel event dico, from quoteid => quote => then you can get the price & price*qty; then you should remove the value from fenwick
        for (int i = 0; i < n; i++)
        {
            var eventType = fs.Next();
            Event evt = new Event(eventType, timestamp: fs.NextLong());

            if (eventType == "ADD")
            {
                evt.QuoteId = fs.NextInt();
                evt.Side = fs.Next();
                evt.Price = fs.NextLong();
                evt.Qty = fs.NextLong();
                evt.Expiry = fs.NextLong();

                prices.Add(evt.Price);
            }
            else if (eventType == "CANCEL")
            {
                evt.QuoteId = fs.NextInt();
            }
            else if (eventType == "QUERY")
            {
                evt.Side = fs.Next();
                evt.Qty = fs.NextLong();
            }

            events[i] = evt;
        }

        return (events, prices);
    }
}

public class Fenwick
{
    public Fenwick(int p) // p is distinct count of prices
    {
        n = p;
        tree = new long[p + 1];
        highestPow2 = 1;
        while ((highestPow2 << 1) <= n)
            highestPow2 <<= 1;
    }

    private long[] tree;
    private int n;

    private readonly int highestPow2; // highestPow2 is the largest power of 2 that is ≤ n

    
    public void Update(int index, long delta)
    {
        for (int i = index; i <= n; i += i & -i)
            tree[i] += delta;
    }

    public (int marginalIdx, long fullQtyBefore) LowerBound(long target)
    {
        int idx = 0;
        long acc = 0;
        for (int d = highestPow2; d > 0; d >>= 1)
        {
            int next = idx + d;
            if (next <= n && acc + tree[next] < target)
            {
                idx = next;
                acc += tree[next];
            }
        }

        return (idx + 1, acc); // marginalIndex is where you need its partial qty
    }

    public long QueryByIndex(int index)
    {
        long sum = 0;
        for (int i = index; i > 0; i -= i & -i)
            sum += tree[i];
        return sum;
    }
}

public class Event
{
    public string EventType { get; }
    public long Timestamp { get; }
    public int QuoteId { get; set; }
    public string Side { get; set; }
    public long Price { get; set; }
    public long Qty { get; set; }
    public long Expiry { get; set; }

    public Event(string eventType, long timestamp)
    {
        EventType = eventType;
        Timestamp = timestamp;
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