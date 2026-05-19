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

Q15 — Connectivity Across Venues with Failures
Difficulty: Medium-hard | 55-65 min
 *ADD t u v
 * FAIL t  u v
 * RESTORE t  u v
 * REACH t a b   YES if a b are connected, otherwise NO
 *

 TODO

  NEED a powerful structure (link-cut tree, Euler tour tree) or an offline trick."
 */


/*
better than brute force

 keep (u, v) => active (bool) map
when add, just grow DSU
at FAIL, mark active to false
    take the whole map, pick up active one, build DSU

at RESTORE, check map if active is false, do like ADD

@query time, dsu already built



brute force:
at query time : scan the whole map, build brand new DSU, check u,v

*/

readonly struct Key
{
    public readonly int U;
    public readonly int V;

    public Key(int u, int v)
    {
        if (u > v) (u, v) = (v, u);
        U = u;
        V = v;
    }
}

class Pragmatic
{
    private const string ADD = "ADD";
    private const string FAIL = "FAIL";
    private const string RESTORE = "RESTORE";
    private const string REACH = "REACH";

    public static void Main2(string[] args)
    {
        StringBuilder sb = new();
        FastScanner fs = new();

        // stream is played once, i need to run Queries n times have to persiste it in order
        int n = fs.NextInt(); // nb  of events
        int nbnodes = 100_000;

        Event[] events = new Event[n];
        Dictionary<Key, bool> state = new();
        DSU dsu = new DSU(nbnodes);
        HashSet<int> seen = new();
        HashSet<Key> activeEdges = new();
        for (int i = 0; i < n; i++)
        {
            string name = fs.Next();
            var e = new Event(name, fs.NextLong(), fs.NextInt(), fs.NextInt());
            events[i] = e;
            var key = new Key(e.U, e.V);
            switch (e.Name)
            {
                case ADD:
                {
                    if (state.TryGetValue(key, out bool active) == false)
                    {
                        state[key] = true;
                        dsu.Union(key.U, key.V);
                        seen.Add(key.U);
                        seen.Add(key.V);
                        activeEdges.Add(key);
                    }
                }
                    break;

                case FAIL:
                {
                    if (state.TryGetValue(key, out bool active) == false || active == false)
                    {
                        break;
                    }

                    activeEdges.Remove(key);
                    state[key] = false;
                    dsu = new DSU(nbnodes);
                    foreach (var ak in activeEdges)
                    {
                        dsu.Union(ak.U, ak.V);
                    }
                }
                    break;

                case RESTORE:
                {
                    if (state.TryGetValue(key, out bool active) && !active)
                    {
                        state[key] = true;
                        dsu.Union(key.U, key.V);
                        activeEdges.Add(key);
                    }
                }
                    break;

                case REACH:
                {
                    if (seen.Contains(key.U) == false || seen.Contains(key.V) == false)
                    {
                        sb.AppendLine("NO");
                        break;
                    }

                    bool connected = dsu.SameCluster(key.U, key.V); // dsu contains only active links
                    if (connected)
                        sb.AppendLine("YES");
                    else sb.AppendLine("NO");
                }
                    break;
            }
        }

        Console.WriteLine(sb);
    }
}

class DSU
{
    private int[] parent;
    private int[] size;

    public DSU(int cnt)
    {
        // 1e5 distinct nodes 
        parent = new int[cnt + 1];
        size = new int[cnt + 1];
        for (int i = 0; i < cnt + 1; i++)
        {
            parent[i] = i;
            size[i] = 1;
        }
    }

    private int Find(int x) // find root
    {
        // phase 1: find root
        int w = x;
        while (parent[w] != w)
        {
            w = parent[w];
        }

        // phase 2: compress 
        // w is root now
        int next = x;
        while (parent[next] != w)
        {
            /// compress all intermediate's root to w            
            int y = parent[next]; // old parent 
            parent[next] = w;
            next = y;
        }

        return w;
    }

    public bool SameCluster(int x, int y)
    {
        return Find(x) == Find(y);
    }

    public bool Union(int x, int y) // fail when already in the same cluster
    {
        int rootx = Find(x);
        int rooty = Find(y);
        if (rootx == rooty) return false; // in the same cluster
        if (size[rootx] < size[rooty])
            (rootx, rooty) = (rooty, rootx);
        parent[rooty] = rootx;
        size[rootx] += size[rooty];
        return true;
    }

    // public void Rollback(int x, int y )
    // {
    //     parent[y] = y;
    //     size[x] -= size[y];
    // }
}