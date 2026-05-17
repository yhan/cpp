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
Q14 — Shortest-Path Quote Routing with Time-Varying Edges
Difficulty: Hard | 70-90 min
A network of N pricing engines is connected by directed links. Each link u → v has a latency w and an availability window [start, end] (inclusive). A quote can flow from u to v only if it arrives at u during the window.

Input: N engines, M links (each with u, v, w, start, end), then Q queries.
Each query: s, d, t0 — start at engine s at time t0.  ************ What's the earliest time you can arrive at d? If unreachable, print -1. **************

You arrive at intermediate engine v at time t_arrive = t_depart + w. To take a link u → v with window [start, end], you must be at u at some time in [start, end - w] (so that you arrive at v by end). If you arrive at u early, you can wait (no cost to wait).
Constraints: N ≤ 5×10³, M ≤ 2×10⁴, Q ≤ 100, times ≤ 10⁹, weights ≤ 10⁶.

Edge cases:

Multiple links between the same pair with different windows

Waiting at an engine is free (no time penalty for sitting still)

A link whose end - w < start is unusable (cannot fit the latency in the window)

Hint: Modified Dijkstra. The "distance" is "earliest arrival time at v." When relaxing edge u → v with window [start, end] and weight w:

If arrival[u] > end - w: edge expired, skip
Effective departure: max(arrival[u], start)
Candidate arrival[v] = max(arrival[u], start) + w
If less than current arrival[v], relax.

Standard Dijkstra with PriorityQueue<int node, long arrivalTime>. O((N+M) log N) per query.

N M   N number of engines  M nb for directed links
u_1 v_1 w_1 start_1 end_1
u_2 v_2 w_2 start_2 end_2
...
u_M v_M w_M start_M end_M
(M lines)

Q
s_1 d_1 t0_1
s_2 d_2 t0_2
...
s_Q d_Q t0_Q

--------------+
VariableRange

N1 ≤ N ≤ 5 × 10³
M0 ≤ M ≤ 2 × 10⁴
Q1 ≤ Q ≤ 100
u, v, s, d      0 ≤ . ≤ N-1, and u ≠ v ( U V are 1 indexed  )
w1 ≤ w ≤ 10⁶              e6 * 2e4 = 2e10 goes beyond int max 2e9 <----- cumulated weights
start, end, t0 ≤ st/end ≤ 10⁹, with start ≤ end
*/


readonly struct Edge
{
    public Edge(int u, int v, long cost, int start, int end)
    {
        U = u;
        V = v;
        Cost = cost;
        Start = start;
        End = end;
    }
    public readonly int U;
    public readonly int V;
    public readonly long Cost;
    public readonly int Start;
    public readonly int End;
}
class Solution
{
    public static void Main(string[] args)
    {
        StringBuilder sb = new();
        FastScanner fs = new ();
        int N = fs.NextInt(); // N nodes
        int M = fs.NextInt(); // M pair (multi (cost, window))
        
        // State
        //-------------------------------------------
        List<Edge>[] allEdges = new List<Edge>[N]; // (a, b) => (weight, start, end) ||=> you need take in valid window the smallest weight
        bool[] registered = new bool[N];
        for (int i = 0; i < N; i++) allEdges[i] = new List<Edge>();
        
        //-------------------------------------------
        
        for(int i =0; i< M; i++) 
        {
            // build edges and nodes
            int u = fs.NextInt();
            int v = fs.NextInt();
            long weight = fs.NextLong(); // avoid int addition overflow
            int wst = fs.NextInt(); // window start
            int wend = fs.NextInt(); // window end
            if(weight + wst > wend ) continue; // end too soon, not valid edge cost
            
            allEdges[u].Add(new Edge(u, v, weight, wst, wend));
            registered[u] = true; registered[v] = true;
        }
        
        // build queries & execute query
        int Q = fs.NextInt(); // less then 100
        for( int i=0; i<Q; i++)
        {
            int st = fs.NextInt();
            int end = fs.NextInt();
            int now = fs.NextInt(); // upbound 1e9
            if(registered[st] == false || registered[end] == false)
            {
                sb.AppendLine("-1");
                continue;
            }
            
               
            long minCost = MinCost(st, end, now);
            sb.AppendLine(minCost.ToString());
        }
        
        Console.WriteLine(sb);
        
        long MinCost(int start, int end, int now ) 
        {
            PriorityQueue<int, long> pq =new();  // sort by cost so far
            long[] sf = new long[N]; // cost so far
            for(int i=0; i< N; i++) 
                sf[i] = long.MaxValue;
            
            bool[] visited= new bool[N];
        
            sf[start] = now;
            pq.Enqueue(start, sf[start]);
            while( pq.Count > 0 ) 
            {
                var x = pq.Dequeue(); // cheapest
                
                if( x == end) 
                    return sf[x];
                if(visited[x] == true) continue;
                visited[x] = true;

                var edges = allEdges[x];
                if (edges.Count == 0)
                    continue;

                foreach(Edge e in edges)
                {
                    int a = e.V; // arrival node
                    long cost= e.Cost;  int left= e.Start; int right= e.End; 
                    long depart = Math.Max(sf[x], left);
                    if(depart + cost > right ) continue; // no more valid if take this path
                        
                    long arrival = depart+ cost;
                    if(arrival < sf[a] )
                    {
                        sf[a] = arrival;
                        pq.Enqueue(a, sf[a]); // ATTENTION ENQUEUE ONLY IF A CAN HOLD IN WINDOW    
                    }
                }
            }
            
            return -1;
        }
        
    }
}

class FastScanner{
    private readonly byte[] data = new byte[1<<16];
    private int len, ptr;
    private int Read()
    {
        if(ptr>=len){
            len=Console.OpenStandardInput().Read(data, 0, data.Length);
            ptr=0;
            if(len==0) return -1;
        }
        return data[ptr++];
    }
    public string Next(){
        int c ;
        do{ c=Read();} while (c<=32 && c>=0);
        var chars= new List<char>();
        while(c >32){
            chars.Add((char)c); 
            c=Read();
        }
        return new string(chars.ToArray());
    }
    public int NextInt() =>(int)NextLong();
    public long NextLong()
    {
        int c; 
        
        do{ c=Read();} while (c<=32 && c>=0);
        long v = 0;
        while(c >32) {
            v=v*10+c-'0';
            c= Read();
        }
        return v;
    }
}
