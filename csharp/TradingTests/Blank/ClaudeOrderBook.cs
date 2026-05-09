using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

internal static class ClaudeOrderBook
{
    private sealed class Quote
    {
        public long Id;
        public long Qty;
        public long Expiry;
        public bool Cancelled;
    }

    private sealed class Level
    {
        public long ActiveQty;
        public LinkedList<Quote> Quotes = new LinkedList<Quote>();
    }

    private sealed class Locator
    {
        public char Side; // 'A' or 'B'
        public long Price;
        public LinkedListNode<Quote> Node;
    }

    private static void Main2()
    {
        TextReader reader = Console.In;
        StringBuilder output = new StringBuilder();

// Asks keyed by price ascending → natural iteration walks lowest first (BUY).
        SortedDictionary<long, Level> asks = new SortedDictionary<long, Level>();
// Bids keyed by NEGATED price → natural iteration walks highest real price first (SELL).
        SortedDictionary<long, Level> bids = new SortedDictionary<long, Level>();

        Dictionary<long, Locator> byId = new Dictionary<long, Locator>();

// Min-heap of pending expirations so each QUERY only does work proportional
// to the quotes that have just expired since the last sweep.
        PriorityQueue<(long Expiry, char Side, long Price, LinkedListNode<Quote> Node), long> expiryHeap
            = new PriorityQueue<(long, char, long, LinkedListNode<Quote>), long>();

        int n = int.Parse(reader.ReadLine()!);

        for (int i = 0; i < n; i++)
        {
            string[] parts = reader.ReadLine()!.Split(' ');

            if (parts[0] == "ADD")
            {
                long t = long.Parse(parts[1]);
                long id = long.Parse(parts[2]);
                char side = parts[3][0];
                long price = long.Parse(parts[4]);
                long qty = long.Parse(parts[5]);
                long expiry = long.Parse(parts[6]);

                if (expiry < t) continue; // born expired, never active

                SortedDictionary<long, Level> book = (side == 'A') ? asks : bids;
                long key = (side == 'A') ? price : -price;

                if (!book.TryGetValue(key, out Level? level))
                {
                    level = new Level();
                    book[key] = level;
                }

                Quote q = new Quote { Id = id, Qty = qty, Expiry = expiry, Cancelled = false };
                LinkedListNode<Quote> node = level.Quotes.AddLast(q);
                level.ActiveQty += qty;

                byId[id] = new Locator { Side = side, Price = price, Node = node };
                expiryHeap.Enqueue((expiry, side, price, node), expiry);
            }
            else if (parts[0] == "CANCEL")
            {
                long id = long.Parse(parts[2]);
                if (!byId.TryGetValue(id, out Locator? loc)) continue;
                byId.Remove(id);

                Quote q = loc.Node.Value;
                if (q.Cancelled) continue;
                q.Cancelled = true;

                SortedDictionary<long, Level> book = (loc.Side == 'A') ? asks : bids;
                long key = (loc.Side == 'A') ? loc.Price : -loc.Price;

                if (book.TryGetValue(key, out Level? level))
                {
                    level.ActiveQty -= q.Qty;
                    level.Quotes.Remove(loc.Node);
                    if (level.Quotes.Count == 0) book.Remove(key);
                }
            }
            else // QUERY
            {
                long t = long.Parse(parts[1]);
                string qSide = parts[2];
                long need = long.Parse(parts[3]);

// Sweep anything with expiry < t.
                while (expiryHeap.Count > 0 && expiryHeap.Peek().Expiry < t)
                {
                    (long exp, char s, long p, LinkedListNode<Quote> nd) = expiryHeap.Dequeue();
                    Quote qq = nd.Value;
                    if (qq.Cancelled) continue;

                    qq.Cancelled = true;
                    SortedDictionary<long, Level> bk = (s == 'A') ? asks : bids;
                    long k = (s == 'A') ? p : -p;
                    if (bk.TryGetValue(k, out Level? lvl))
                    {
                        lvl.ActiveQty -= qq.Qty;
                        lvl.Quotes.Remove(nd);
                        if (lvl.Quotes.Count == 0) bk.Remove(k);
                    }

                    byId.Remove(qq.Id);
                }

                SortedDictionary<long, Level> targetBook = (qSide == "BUY") ? asks : bids;
                long remaining = need;
                long cost = 0;

                foreach (KeyValuePair<long, Level> kv in targetBook)
                {
                    Level level = kv.Value;
                    if (level.ActiveQty <= 0) continue;
                    long realPrice = (qSide == "BUY") ? kv.Key : -kv.Key;

                    if (level.ActiveQty >= remaining)
                    {
                        cost += remaining * realPrice;
                        remaining = 0;
                        break;
                    }

                    cost += level.ActiveQty * realPrice;
                    remaining -= level.ActiveQty;
                }

                output.Append(remaining > 0 ? "-1" : cost.ToString()).Append('\n');
            }
        }

        Console.Write(output.ToString());
    }
}