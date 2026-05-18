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
        var fs = new FastScanner();
        int n = fs.NextInt();
        Strat[] strats = new Strat[n];

        int[][] g = new int[n][]; //graph
        int[] indeg = new int[n];
        List<int> nodepNodes = new();
        for (int i = 0; i < n; i++)
        {
            strats[i] = new Strat(fs.NextInt(), fs.NextInt(), fs.NextInt(), fs.NextInt());
            int k = fs.NextInt();
            int[] deps = new int[k]; // k

            for (int j = 0; j < k; j++)
            {
                deps[j] = fs.NextInt();
            }

            g[i] = deps;

            indeg[i] = k;
            if (k == 0) nodepNodes.Add(i);
        }

        //Step 1 : detect cycles
        // Kahn indeg from indeg where indeg value == 0, meaning no dep ... count all reachable nodes; if < n, then cycle
        int[] indeg2 = (int[])indeg.Clone(); // make copy becase indeg will used later to find lex ordered strat deps sorted order sequence
        Queue<int> q = new();
        foreach (var x in nodepNodes)
            q.Enqueue(x);
        int noCycCnt = 0;
        while (q.Count > 0)
        {
            int u = q.Dequeue(); // first nodes has at least indeq = 1
            // when deque out, the indeg val is 0 
            noCycCnt++;
            indeg[u]--;
            if (indeg[u] == 0)
            {
                q.Enqueue(u);
            }
        } // indeg2 contains val > 0 they are in cycle nodes 


        if (noCycCnt < n)
        {
            List<int> scc = new();
            for (int c = 0; c < indeg2.Length; c++)
            {
                if (indeg2[c] > 0)
                    scc.Add(c);
            }

            scc.Sort();
            Console.Write("CYCLE ");
            Console.WriteLine(string.Join(' ', scc));
            return;
        }

        // Step 2:  no cycles 
        /*
        2. From the ready set, pick the strategy with:
   - smallest `p_i`; tie-break by
   - smallest `d_i`; tie-break by
   - smallest `i`.

        THE COST IS A TIME, cumulated time
        a strat can be processed only if cumtime + its_own_cost <= deadline

OUTPUT : need count MISSED & order
ORDER 2 3 1
MISSED 0
        */
        StringBuilder orderBuilder = new();
        List<int> order = new();
        int missed = 0;
        PriorityQueue<Strat, (int, int, int)> pq = new();
        foreach (var nodep in nodepNodes)
        {
            var strat = strats[nodep];
            pq.Enqueue(strat, (strat.Priority, strat.Deadline, strat.Id));
        }

        // deque from pq, and explor neighbors , use indeg to ensure dep tree
        int time = 0;
        while (pq.Count > 0)
        {
            Strat u = pq.Dequeue(); // no more dep, but check if should include in order: check its deadline
            if (time + u.Cost > u.Deadline)
            {
                missed++;
                continue; // no more need explore downstream
            }

            order.Add(u.Id);
            orderBuilder.Append("{u} ");
            time += u.Cost;
            foreach (int v in g[u.Priority])
            {
                // v is downstream
                indeg[v]--;
                if (indeg[v] == 0) // all dep processed, v is ready now
                {
                    Strat sv = strats[v];
                    pq.Enqueue(sv, (sv.Priority, sv.Deadline, sv.Id));
                }
            }
        }

        // output   ORDER 2 3 1  then new line MISSED COUNT
        Console.WriteLine($"ORDER {orderBuilder.ToString()}");
        Console.WriteLine($"MISSED {missed}");
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
            v = v * 10 + c - '0';
            c = Read();
        }

        return v;
    }
}