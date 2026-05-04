using System;
using System.Collections.Generic;
using System.Text;

class Solution
{
    const double EPS = 1e-9;
    static void Main(string[] args)
    {
/*
        INPUT:
        read M (int) = ccy pair and their rate
        next M lines contains [FROM TO double_value( is the forex rate)]
        find a path where arbitrage opportunity exists

        OUTPUT: 
        the arbitrage path : ex: EUR JPY GBX EUR

        // IMPORTANT : the start CCY is not important, we need just a cycle
*/
        Edge[] fromInput = BuildEdges();
        List<Edge> edges = ReverseBuild(fromInput);
    
        int[] pred = new int[Edge.CcyCount]; // each vertex's previous vertex (using its integer symbol)
        double[] dist = new double[Edge.CcyCount]; // lowest cost to reach v   
        Array.Fill(dist, 0.0); // objective is minimize dist
        Array.Fill(pred, -1); // no previous
        for(int i= 0; i < Edge.CcyCount - 1; i++) // vertex COUNT should be used
        {
            bool updated = false;
            foreach (var e in edges)
            {
                int u = e.From; int v = e.To;
                if( dist[u] +  e.Rate < dist[v] - EPS ) 
                {
                    updated = true;
                    dist[v] = dist[u] +  e.Rate;
                    pred[v] = u;
                }
            } 
            if(updated == false) break;        
        }
        // run again if can still improve, then there is a cycle arbitrable
        int cycleNode = -1;
        foreach (var e in edges)
        {
            int u = e.From; int v = e.To;
            if( dist[u] +  e.Rate < dist[v] - EPS) 
            {              
                dist[v] =  dist[u] +  e.Rate;
                pred[v] = u; // for walkback works...
                cycleNode = v;    // the question asks only if at least one arbitrage opportunity  exist ...  
    
                break;
            }
        }
        // Check opportunity does not exist
        if(cycleNode == -1) {
            Console.WriteLine("NO CYCLE"); return;
        }

        // Locate a node in the cycle
        int node = cycleNode;
        for(int i = 0;  i< Edge.CcyCount; i++)
        {
            node = pred[node]; // make sure entered into cycle   
        }

        List<int> rev = new List<int>(); // reversed path
        int walk = node;
        do {
            rev.Add(walk); // the last of rev is cycle start
            walk = pred[walk] ;            
        } while (pred[walk] != node);
        rev.Add(node);

// build final : 
        StringBuilder sb = new StringBuilder();
        for(int i = rev.Count - 1; i>=0; i--)
        {
            sb.Append(Edge.AsSymbol(rev[i]));   
            if(i > 0)
                sb.Append(' ');  
        }
        Console.WriteLine(sb);
    }

    private static List<Edge> ReverseBuild(Edge[] edges) // Should manage bidiretional ccy pair in already in INPUT
    {
        HashSet<(int,  int)> uniq = new HashSet<(int, int)>();
        List<Edge> all = new List<Edge>();
        foreach(var e in edges)
        {
            if(uniq.Add((e.From, e.To)))
            {
                all.Add(e);   
            }
            if(uniq.Add((e.To, e.From)))
            {
                all.Add(e.Reverse());
            }
        }
        return all;      
    }

    private static Edge[] BuildEdges()
    {
        var fs = new FastScanner();
        int m = fs.NextInt(); // m lines
        Edge[] edges = new Edge[m];
        for(int i=0; i<m; i++)
        {
            edges[i] = new Edge(fs.Next(), fs.Next(), fs.NextDouble());
        }
        return edges;
    }
}
class Edge {
    public readonly int From;
    public readonly int To;
    public readonly double Rate;
    private readonly double  OriginalRate;
    private static Dictionary<string, int> ccyToIndex = new Dictionary<string, int>();
    private static Dictionary<int, string> idxToCcy = new Dictionary<int, string>();
    public static int CcyCount => ccyToIndex.Count;

    public Edge Reverse() //-log(1/r) = +log(r) = -(-log(r)))
    {
        return new Edge(this.To, this.From, 1/this.OriginalRate);
    }

    public static string AsSymbol(int v)
    {
        return idxToCcy[v];
    }

    int GetIndex(string ccy)
    {
        if (!ccyToIndex.TryGetValue(ccy, out int idx))
        {
            idx = ccyToIndex.Count;
            ccyToIndex[ccy] = idx;
            idxToCcy[idx] = ccy;
        }
        return idx;
    }

    public Edge(int from, int to, double rate){  // rate is the forex rate

        From = from; To = to; Rate= -Math.Log(rate);   
        OriginalRate = rate;
    }
    // rate is the initial forex rate, should convert to -log(rate)
    public Edge(string from, string to, double rate) {
        From=GetIndex(from); To=GetIndex(to); Rate= -Math.Log(rate);   
        OriginalRate = rate;
    }
}