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
Q6 — Cross-Venue Stale Quote Detector
Difficulty: Medium | 35–45 min
A consolidated feed receives QUOTE venueId symbol bid ask timestamp events in non-decreasing timestamp order. 
A quote is stale at time t if the same venue+symbol pair has not emitted an update in the last S time units (i.e. t - lastUpdate > S).
 Process a stream of QUOTE events and CHECK t queries. For each CHECK, output the count of (venue, symbol) pairs that are currently stale among pairs that have ever been seen.
Edge cases: a pair seen exactly once becomes stale at firstSeen + S + 1; CHECK at the same timestamp as a QUOTE on that pair → not stale.
Hint: lazy heap by lastUpdate + S, plus a generation counter per pair.

INPUT
first line nb_ofevents S
next nb_ofevents lines are quotes or checks
QUOTE bats vod.l 0 0 4
QUOTE bats vod.l 0 0 5
QUOTE bats gle.pa 0 0 6
CHECK 10

OUTPUT
total nb of (venue, stock ) are stale 

*/
class Solution
{
    /// QUOTE venueId symbol bid ask timestamp
    // OUTPUT 
    // count of (venue, symbol) pairs that are currently stale among pairs that have ever been seen.
    /*
*/
    public static void Main(string[] args)
    {
       var fs = new FastScanner();
       int n = fs.NextInt();
       int s = fs.NextInt();
       
       HashSet<(string, string )> everSeenSym =new();
       PriorityQueue<(string, string), long> pq = new();
       Dictionary<(string, string), int> freshCounter= new(); // hypothetical fresh counter, updated when CHECK arrives
       StringBuilder sb=new ();
       for(int i=0; i< n; i++)
       {
          switch( fs.Next() )
          {
            case "QUOTE":
            {
                string venue = fs.Next();
                string symbol = fs.Next();
                fs.NextInt();  fs.NextInt();  // bid ask no use
                long ts = fs.NextLong(); // s min 0 if ts=0 not stale can enqueu,  even if already stale is ok , check will handle
                var pair = (venue, symbol);
                everSeenSym.Add(pair);
                
                pq.Enqueue(pair, ts);
                if(freshCounter.ContainsKey(pair) == false)
                { 
                    freshCounter[pair] = 1 ; 
                }
                else
                {
                    freshCounter[pair]++;
                }
                
            } break;
            
            case "CHECK":
            {
                long now = fs.NextLong();
                
                while(pq.TryPeek(out var pair, out long testTs) && (now - testTs) > s) // LOG N
                {
                    pq.Dequeue();
                    freshCounter[pair]--;
                    if(freshCounter[pair] == 0)
                        freshCounter.Remove(pair);
                }
                
                int stale = everSeenSym.Count - freshCounter.Count;
                sb.AppendLine(stale.ToString());
                
            } break;
          }
       }
       Console.WriteLine(sb);
       /* edge cases
         first event is CHECK
       */
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