using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
enum Status
{
    NONE,
    PENDING, // new but before ack/REJECT
    LIVE, // after ack ( can have partial fill but not cancelled)
    REJECTED, // 
    DONE,  // fully filled
    CANCELLED // cancelled
}
class Solution
{
    static void Main(string[] args) // required Time O(N + A log A), where A is the number of active orders printed. Space: O(U), where U is the number of distinct order ids. 
    {
        var fs= new FastScanner();
        int n = fs.NextInt();// nb of events

// output requires : 
        int invalid = 0; // max 2e5
        Dictionary<string, int> qties = new();// order id => remaining qty, keep only live orders
        Dictionary<string, Status> all = new(); // all orders status

        for(int i =0; i<n;i++)
        {
            string evtName =fs.Next();
            string orderId = fs.Next();

            Status status;
            switch (evtName)
            {
                case "NEW":
                int newQty = fs.NextInt();
                if(all.TryGetValue(orderId, out status))
                    invalid++;
                else
                {
                    // add order
                    all[orderId]= Status.PENDING;
                    qties[orderId] = newQty;
                }
                break;
                
                case "ACK":
                if(all.TryGetValue(orderId, out status) == false || status != Status.PENDING)
                    invalid++;
                else
                {
                    all[orderId] = Status.LIVE;
                }
                 
                break;
                
                case "REJECT":
                if(all.TryGetValue(orderId, out status) == false || status != Status.PENDING)
                    invalid++;
                else
                {
                    all[orderId] = Status.REJECTED;
                    qties.Remove(orderId);
                }                 

                break;
                
                case "FILL":
                int fillQty = fs.NextInt();
                if(all.TryGetValue(orderId, out status) == false || status != Status.LIVE)
                {
                    invalid++;
                }
                else
                {
                    int rem = qties[orderId]; // remaining qty
                    if(fillQty < 1 || fillQty > rem) {
                        invalid++; break;
                    }
                    rem -= fillQty;
                    if(rem == 0)
                    { // remove from live order
                        qties.Remove(orderId);
                        all[orderId] = Status.DONE;
                    }
                    else{
                        qties[orderId] = rem;
                    }
                    
                }                 

                break;
                
                case "CANCEL":
                if(all.TryGetValue(orderId, out status) == false || status != Status.LIVE)
                    invalid++;
                else
                {
                    all[orderId] = Status.CANCELLED;
                    qties.Remove(orderId);
                }                 

                break;
            }
        }

        // manage output
        // nb of invalid events
        Console.WriteLine(invalid);
        // nb of live orders
        Console.WriteLine(qties.Count);
        // live orders sorted by order id: orderid remainingqty
        StringBuilder sb = new();
        foreach(var kv in qties.OrderBy(x => x.Key, StringComparer.Ordinal)) // can be non ASCII
        {
            sb.AppendLine($"{kv.Key} {kv.Value}");
        }
        Console.Write(sb);
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
            if (len == 0) return -1;
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
