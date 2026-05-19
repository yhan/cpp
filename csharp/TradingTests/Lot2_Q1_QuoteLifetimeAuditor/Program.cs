
using System.Text;

public class Quote {
    public long Ts; public long Id;
    public long Bid;public long BidSz;
    public long Ask;public long AskSz;
    
    public long MatchedAskSz; // cumulated
    public long MatchedBidSz; // cumulated
    
    public long RemAsk => AskSz - MatchedAskSz;
    public long RemBid => BidSz - MatchedBidSz;
    
    public bool AskCancelled;// modify qty to 0 or total cancel
    public bool BidCancelled; // modify qty to 0 or total cancel
    
    public long TTL;
    public long ExpireAt => Ts+TTL;
    public Quote(long ts, long qid, long bid, long bidsz, long ask, long asksz, long ttl) 
    {
        Ts=ts;Id=qid;Bid=bid;BidSz=bidsz; Ask=ask; AskSz=asksz; TTL =ttl;
        if(BidSz == 0) BidCancelled = true;
        if(AskSz == 0) AskCancelled = true;
    }
    
    public bool AskDead(long ts)
    {
        if(ts >= ExpireAt) return true;
        if(AskCancelled )  return true;
        return RemAsk <= 0;
    }
    
    public bool BidDead(long ts)
    {
        if(ts >= ExpireAt) return true;
        if(BidCancelled )  return true;
        return RemBid <= 0;
    }
    
    public bool Modify (long newbidSz, long newAskSz, long ts) // claude says even if Modify does nothing, a noop amend is a valid amend 
    // spec:  Invalid `AMEND` or `CANCEL` (unknown `q_id`, or quote already fully dead) is silently ignored but counted.
    {
        if(BidDead(ts) && AskDead(ts)) return false;
                
        // if one side is cancelled, meaning one side qty drops to 0
        
        if(BidDead(ts) == false && newbidSz >= MatchedBidSz)
        {
                BidSz=newbidSz; 
                if(BidSz == 0) BidCancelled = true;      
        }
        if(AskDead(ts) == false && newAskSz >= MatchedAskSz)
        {
                AskSz = newAskSz;
                if(AskSz == 0) AskCancelled = true;             
        }
        
        return true;
    }
    
    public bool Cancel(long ts)
    {
        if(AskDead(ts) && BidDead(ts)) 
           return false;
        AskCancelled = true;
        BidCancelled = true;
        return true;
    }
    
    public (long, long) MatchAsk(long tradeSz)
    {
        long matched; long unmatched;
        if(tradeSz <= RemAsk)
        {
            matched = tradeSz; // is about traded size
            unmatched = 0;
        }
        else { //trade > rem
            matched = RemAsk;
            unmatched = tradeSz - RemAsk;   
        }
        MatchedAskSz += matched;
               
        return (matched, unmatched);
    }
     
    public (long, long) MatchBid(long tradeSz)
    {
        long matched; long unmatched;
        if(tradeSz <= RemBid)
        {
            matched = tradeSz; // is about traded size
            unmatched = 0;
        }
        else { //trade > rem
            matched = RemBid;
            unmatched = tradeSz - RemBid;   
        }
        MatchedBidSz += matched;
        return (matched, unmatched);
    }
}

public class DescComparer : IComparer<(long, long)>
{
    public int Compare((long, long) x, (long, long) y)
    {        
        int tmp = y.Item1.CompareTo(x.Item1); // for bid, price higher first
        if(tmp!=0) return tmp;
        
        return x.Item2.CompareTo(y.Item2);
    }   
}

public class AscComparer : IComparer<(long, long)>
{
    public int Compare((long, long) x, (long, long) y)
    {        
        int tmp = x.Item1.CompareTo(y.Item1);
        if(tmp!=0) return tmp;
        
        return x.Item2.CompareTo(y.Item2);
    }   
}
class Result
{

    public static void Main()
    {
        var fs=new FastScanner();
        
        SortedDictionary<long, Quote> quotes= new();
        long invalidCnt = 0;//  invalid amend + cancel
        // price priority
        PriorityQueue<Quote, (long, long)> bpq = new PriorityQueue<Quote, (long, long)>(new DescComparer());
        PriorityQueue<Quote, (long, long)> apq = new PriorityQueue<Quote, (long, long)>(new AscComparer());
        
        StringBuilder sb=new();
        while(fs.HasNext())
        {
            string name = fs.Next();
            switch(name)
            {
                case "QUOTE" :
                {
                    long ts = fs.NextLong(); long qid = fs.NextLong(); long bid = fs.NextLong(); long bidsz = fs.NextLong(); long ask = fs.NextLong(); long asksz = fs.NextLong(); long ttl = fs.NextLong();
                    var q = new Quote(ts, qid, bid, bidsz, ask, asksz, ttl);
                    quotes[q.Id] = q;
                    bpq.Enqueue(q, (bid, q.Id));
                    apq.Enqueue(q, (ask, q.Id));
                    
                } break;
                case "AMEND" :
                {
                    long ts = fs.NextLong(); long qid = fs.NextLong(); long bidSz = fs.NextLong(); long askSz = fs.NextLong(); 
                    if(quotes.TryGetValue(qid, out Quote? q) == false)
                    {
                        invalidCnt++;
                        break;                    
                    }
                
                    bool ok = q.Modify(bidSz, askSz, ts);
                    if(ok == false) invalidCnt++;
                    
                } break;
                case "CANCEL" :
                {
                    long ts = fs.NextLong(); long qid = fs.NextLong();
                    if(quotes.TryGetValue(qid, out Quote? q) == false)
                    {
                        invalidCnt++;
                        break;                    
                    }
                    bool ok = q.Cancel(ts);
                    if(ok == false) invalidCnt++;
                    
                    
                } break;
                case "TRADE" :
                {
                    long ts = fs.NextLong(); string side = fs.Next(); 
                    fs.Next(); // skip price
                    long tradeSz = fs.NextLong();
                    if(side == "BUY")
                    {
                        while(apq.TryPeek(out Quote? askQuote, out var _) && askQuote.AskDead(ts) )
                        {
                            apq.Dequeue();                        
                        }
                        if(apq.Count > 0)
                        {
                            var askQuote = apq.Peek(); // i am sure ask is not dead
                            (long mat, long unmat) = askQuote.MatchAsk(tradeSz);
                            sb.AppendLine($"{askQuote.Id} {mat} {unmat}");
                            if(askQuote.RemAsk == 0)
                               apq.Dequeue();
                            
                        }
                        else {
                            // no more ask quote alive
                            sb.AppendLine($"- 0 {tradeSz}");
                        }
                    }
                    else // SELL
                    {
                        while(bpq.TryPeek(out Quote? bidQuote, out var _) && bidQuote.BidDead(ts) )
                        {
                            bpq.Dequeue();                        
                        }
                        if(bpq.Count > 0)
                        {
                            var bidQuote = bpq.Peek(); // i am sure ask is not dead
                            (long mat, long unmat) = bidQuote.MatchBid(tradeSz);
                            sb.AppendLine($"{bidQuote.Id} {mat} {unmat}");
                            if(bidQuote.RemBid == 0)
                               bpq.Dequeue();
                        }
                        else {
                            // no more ask quote alive
                            sb.AppendLine($"- 0 {tradeSz}");
                        }
                    }
                } break;
            }
        }
       
        
        // output        
        foreach( var kv in quotes)
        {
            var q = kv.Value;
            sb.AppendLine($"{q.Id} {q.MatchedAskSz} {q.MatchedBidSz}");     // bougty from me : ask size
        }
        
        // invalid count
        sb.Append($"IGNORED {invalidCnt}");
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
}