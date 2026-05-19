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




public record Strat(int Id, int Cost, int Deadline, int Priority);
class Result
{
    /*
    INPUT
    4
(i c_i  d_i  p_i  k  dep_1 dep_2 ... dep_k)
id cost deadline prio k dependencies 
1 10 100 1 1 3
2 5 50 2 0
3 8 80 1 1 1
4 6 60 3 1 2

sample output
1)
CYCLE 1 3

2)
ORDER 2 4 1 3 5
MISSED 1

3)
ORDER 2 3 1
MISSED 0
*/
    public static void Main()
    {
        var fs=new FastScanner();
        int  n = fs.NextInt(); // 2 × e5
        Strat[] strats = new Strat[n+1];
        
        List<int>[] g = new List<int>[n+1]; //graph
        for(int i=1; i <=n; i++) g[i]=new List<int>();
        int[] indeg = new int[n+1]; // BUG everything id => whatever should be 1 indexed
        List<int> nodepNodes= new();
        for(int i=1; i<= n; i++)
        {
            int vid = fs.NextInt(); // 1 indexed
            strats[vid] = new Strat(vid, fs.NextInt(), fs.NextInt(), fs.NextInt());
            int k = fs.NextInt(); 
            for(int j=0; j< k; j++)
            {
                var u = fs.NextInt();
                g[u].Add(vid);
            }
            
            indeg[vid] = k;
            if(k == 0) nodepNodes.Add(vid);
        }
        
        //Step 1 : detect cycles
        // Kahn indeg from indeg where indeg value == 0, meaning no dep ... count all reachable nodes; if < n, then cycle
        // the question asks printing only SCC cycles , should not include downstream of scc
        // redo with pragmatic scc detection, two passes
        // 
        var sccDector = new SCCTarjan1Indexed(g, n);
        List<List<int>> sccs = sccDector.Detect(); // excluding only single 
        
        if(sccs.Count > 0)
        {
            StringBuilder sb =new("CYCLE");
            foreach (int nscc in sccs.SelectMany(x =>x).OrderBy(x => x))
            {
                sb.Append($" {nscc}");
            }   
            Console.WriteLine(sb);
            return;
        }
        
        // Step 2:  no cycles 
        StringBuilder orderBuilder=new("ORDER ");
        List<int> order=new();
        int missed = 0;
        PriorityQueue<Strat, (int, int, int)> pq=new();
        foreach( var nodep in nodepNodes)
        { 
            var strat = strats[nodep];
            pq.Enqueue(strat, (strat.Priority, strat.Deadline, strat.Id));
        }
        // deque from pq, and explor neighbors , use indeg to ensure dep tree
        long time=0;
        while(pq.Count > 0) 
        {
            Strat u = pq.Dequeue(); // no more dep, but check if should include in order: check its deadline
            // Bug 5: missed deadlines must still advance time and unblock downstream
            time += u.Cost;
            if(time > u.Deadline) 
            {
                missed++;
            }
            order.Add(u.Id);
            orderBuilder.Append($" {u.Id}");
            
            foreach( int v in g[u.Id] )
            {
                // v is downstream
                indeg[v]--;
                if(indeg[v] == 0) // all dep processed, v is ready now
                {
                    Strat sv = strats[v];
                    pq.Enqueue(sv, (sv.Priority, sv.Deadline, sv.Id)); 
                }
            }
        }
        
        // output   ORDER 2 3 1  then new line MISSED COUNT
        Console.WriteLine(orderBuilder);
        Console.WriteLine($"MISSED {missed}");
    }

}
class FastScanner
{
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

public class SCCTarjan1Indexed
{
    private readonly List<int>[] _g;
    private readonly int _n;
    private readonly int[] _disc;
    private readonly int[] _low;
    private readonly bool[] _onStack;
    private readonly Stack<int> _sccStack = new Stack<int>();
    private int _counter;

    /// <param name="graph">Adjacency list, 1-indexed. graph[i] is the list of successors of node i. Must be sized at least nodes+1. Null entries treated as empty.</param>
    /// <param name="nodes">Number of nodes. Valid IDs are 1..nodes.</param>
    public SCCTarjan1Indexed(List<int>[] graph, int nodes)
    {
        _g = graph;
        _n = nodes;
        _disc = new int[nodes + 1];
        _low = new int[nodes + 1];
        _onStack = new bool[nodes + 1];
        Array.Fill(_disc, -1); // -1 means unvisited
    }

    public List<List<int>> Detect()
    {
        List<List<int>> sccs = new List<List<int>>();

        for (int start = 1; start <= _n; start++)
        {
            if (_disc[start] != -1) continue;
            DfsIterative(start, sccs);
        }

        return sccs;
    }

    private void DfsIterative(int start, List<List<int>> sccs)
    {
        Stack<(int node, int nextIdx)> dfsStack = new Stack<(int, int)>();

        Visit(start);
        dfsStack.Push((start, 0));

        while (dfsStack.Count > 0)
        {
            (int v, int i) = dfsStack.Peek();
            List<int> neighbors = _g[v];

            if (neighbors != null && i < neighbors.Count)
            {
                int w = neighbors[i];
                dfsStack.Pop();
                dfsStack.Push((v, i + 1));

                if (_disc[w] == -1)
                {
                    // Tree edge: descend into w
                    Visit(w);
                    dfsStack.Push((w, 0));
                }
                else if (_onStack[w])
                {
                    // Back edge: update low-link
                    _low[v] = Math.Min(_low[v], _disc[w]);
                }
                // else: cross/forward edge into completed SCC, ignore
            }
            else
            {
                // Finished v: all neighbors processed
                dfsStack.Pop();

                if (dfsStack.Count > 0)
                {
                    int parent = dfsStack.Peek().node;
                    _low[parent] = Math.Min(_low[parent], _low[v]);
                }

                // SCC root: pop the component
                if (_low[v] == _disc[v])
                {
                    List<int> scc = new List<int>();
                    int popped;
                    do
                    {
                        popped = _sccStack.Pop();
                        _onStack[popped] = false;
                        scc.Add(popped);
                    } while (popped != v);

                    if (IsNonTrivial(scc))
                        sccs.Add(scc);
                }
            }
        }
    }

    private bool IsNonTrivial(List<int> scc)
    {
        if (scc.Count > 1) return true;
        // Single node: non-trivial only if self-loop
        int u = scc[0];
        List<int> nbrs = _g[u];
        if (nbrs == null) return false;
        foreach (int w in nbrs)
            if (w == u) return true; //self linked to inlucde
        return false;
    }

    private void Visit(int v)
    {
        _disc[v] = _low[v] = _counter++;
        _sccStack.Push(v);
        _onStack[v] = true;
    }
}
