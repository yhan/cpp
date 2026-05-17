using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/*
Q15 — Connectivity Across Venues with Failures
Optimal: offline segment-tree-of-time + rollback DSU
Complexity: O((N + Q) * log Q * log N)
*/

readonly struct Event
{
    public readonly string Name;
    public readonly long Time;
    public readonly int U;
    public readonly int V;
    public Event(string name, long t, int u, int v)
    {
        Name = name; Time = t; U = u; V = v;
    }
}

readonly struct AliveInterval
{
    public readonly int U;
    public readonly int V;
    public readonly int QueryLo;
    public readonly int QueryHi;
    public AliveInterval(int u, int v, int lo, int hi)
    {
        U = u; V = v; QueryLo = lo; QueryHi = hi;
    }
}

class Solution
{
    private const string ADD = "ADD";
    private const string FAIL = "FAIL";
    private const string RESTORE = "RESTORE";
    private const string REACH = "REACH";

    private static long EdgeKey(int u, int v)
    {
        int a = Math.Min(u, v);
        int b = Math.Max(u, v);
        return ((long)a << 32) | (uint)b;
    }

    public static void Main(string[] args)
    {
        FastScanner fs = new FastScanner();
        StringBuilder sb = new StringBuilder();

        int n = fs.NextInt();

        // Phase 1: read events
        Event[] events = new Event[n];
        List<int> queryEventIdx = new List<int>();
        int maxNodeId = 0;

        for (int i = 0; i < n; i++)
        {
            string name = fs.Next();
            long t = fs.NextLong();
            int u = fs.NextInt();
            int v = fs.NextInt();
            events[i] = new Event(name, t, u, v);
            if (name == REACH) queryEventIdx.Add(i);
            if (u > maxNodeId) maxNodeId = u;
            if (v > maxNodeId) maxNodeId = v;
        }

        int Q = queryEventIdx.Count;
        if (Q == 0) { Console.Write(""); return; }

        // Phase 2: compute alive intervals per edge
        Dictionary<long, (bool active, int aliveSinceQuery)> edgeState
            = new Dictionary<long, (bool, int)>();// u,v Key => (active, aliveSince)
        Dictionary<long, (int u, int v)> edgeNodes = new Dictionary<long, (int, int)>();
        HashSet<int> seen = new HashSet<int>();

        List<AliveInterval> intervals = new List<AliveInterval>();
        int qNow = 0;

        for (int i = 0; i < n; i++)
        {
            Event e = events[i];

            if (e.Name == REACH)
            {
                qNow++;
                continue;
            }

            long key = EdgeKey(e.U, e.V);

            switch (e.Name)
            {
                case ADD:
                    if (!edgeState.ContainsKey(key))
                    {
                        edgeState[key] = (true, qNow);
                        edgeNodes[key] = (e.U, e.V);
                        seen.Add(e.U);
                        seen.Add(e.V);
                    }
                    break;

                case FAIL:
                    if (edgeState.TryGetValue(key, out (bool active, int since) sFail)
                        && sFail.active)
                    {
                        if (sFail.since <= qNow - 1)
                        {
                            (int eu, int ev) = edgeNodes[key];
                            intervals.Add(new AliveInterval(eu, ev, sFail.since, qNow - 1));
                        }
                        edgeState[key] = (false, -1);
                    }
                    break;

                case RESTORE:
                    if (edgeState.TryGetValue(key, out (bool active, int since) sRest)
                        && !sRest.active)
                    {
                        edgeState[key] = (true, qNow);
                    }
                    break;
            }
        }

        // Flush still-alive edges
        foreach (KeyValuePair<long, (bool active, int since)> kv in edgeState)
        {
            if (kv.Value.active && kv.Value.since <= Q - 1)
            {
                (int eu, int ev) = edgeNodes[kv.Key];
                intervals.Add(new AliveInterval(eu, ev, kv.Value.since, Q - 1));
            }
        }

        // Phase 3: build segment tree over [0, Q-1]
        List<(int u, int v)>[] tree = new List<(int, int)>[4 * Q];
        for (int i = 0; i < tree.Length; i++) tree[i] = new List<(int, int)>();

        foreach (AliveInterval iv in intervals)
        {
            InsertInterval(tree, 1, 0, Q - 1, iv.QueryLo, iv.QueryHi, iv.U, iv.V);
        }

        // Phase 4: DFS with rollback DSU
        RollbackDSU dsu = new RollbackDSU(maxNodeId + 1);
        string[] answers = new string[Q];

        (int a, int b)[] queryPairs = new (int, int)[Q];
        for (int q = 0; q < Q; q++)
        {
            Event qe = events[queryEventIdx[q]];
            queryPairs[q] = (qe.U, qe.V);
        }

        Dfs(tree, dsu, 1, 0, Q - 1, queryPairs, seen, answers);

        for (int q = 0; q < Q; q++)
        {
            sb.AppendLine(answers[q]);
        }
        Console.Write(sb);
    }

    private static void InsertInterval(
        List<(int, int)>[] tree,
        int treeIdx, int nodeLo, int nodeHi,
        int lo, int hi,
        int u, int v)
    {
        if (hi < nodeLo || nodeHi < lo) return;
        if (lo <= nodeLo && nodeHi <= hi)
        {
            tree[treeIdx].Add((u, v));
            return;
        }
        int mid = (nodeLo + nodeHi) >> 1;
        InsertInterval(tree, 2 * treeIdx,     nodeLo, mid,    lo, hi, u, v);
        InsertInterval(tree, 2 * treeIdx + 1, mid + 1, nodeHi, lo, hi, u, v);
    }

    private static void Dfs(
        List<(int, int)>[] tree,
        RollbackDSU dsu,
        int treeIdx, int nodeLo, int nodeHi,
        (int a, int b)[] queryPairs,
        HashSet<int> seen,
        string[] answers)
    {
        int unionCount = tree[treeIdx].Count;
        foreach ((int u, int v) edge in tree[treeIdx])
        {
            dsu.Union(edge.u, edge.v);
        }

        if (nodeLo == nodeHi)
        {
            (int a, int b) = queryPairs[nodeLo];
            if (!seen.Contains(a) || !seen.Contains(b))
            {
                answers[nodeLo] = "NO";
            }
            else
            {
                answers[nodeLo] = dsu.SameCluster(a, b) ? "YES" : "NO";
            }
        }
        else
        {
            int mid = (nodeLo + nodeHi) >> 1;
            Dfs(tree, dsu, 2 * treeIdx,     nodeLo, mid,    queryPairs, seen, answers);
            Dfs(tree, dsu, 2 * treeIdx + 1, mid + 1, nodeHi, queryPairs, seen, answers);
        }

        for (int i = 0; i < unionCount; i++)
        {
            dsu.Rollback();
        }
    }
}

class RollbackDSU
{
    private readonly int[] parent;
    private readonly int[] size;
    private readonly Stack<(int ry, int rx, int oldSize)> history;

    public RollbackDSU(int n)
    {
        parent = new int[n];
        size = new int[n];
        for (int i = 0; i < n; i++)
        {
            parent[i] = i;
            size[i] = 1;
        }
        history = new Stack<(int, int, int)>();
    }

    public int Find(int x)
    {
        while (parent[x] != x) x = parent[x];
        return x;
    }

    public void Union(int x, int y)
    {
        int rx = Find(x);
        int ry = Find(y);
        if (rx == ry)
        {
            history.Push((-1, -1, -1));
            return;
        }
        if (size[rx] < size[ry])
        {
            (rx, ry) = (ry, rx);
        }
        history.Push((ry, rx, size[rx]));
        parent[ry] = rx;
        size[rx] += size[ry];
    }

    public void Rollback()
    {
        (int ry, int rx, int oldSize) = history.Pop();
        if (ry == -1) return;
        parent[ry] = ry;
        size[rx] = oldSize;
    }

    public bool SameCluster(int x, int y) => Find(x) == Find(y);
}

class FastScanner
{
    private readonly byte[] data = new byte[1 << 16];
    private int len;
    private int ptr;
    private readonly Stream stream = Console.OpenStandardInput();

    private int Read()
    {
        if (ptr >= len)
        {
            len = stream.Read(data, 0, data.Length);
            ptr = 0;
            if (len == 0) return -1;
        }
        return data[ptr++];
    }

    public string Next()
    {
        int c;
        do { c = Read(); } while (c <= 32 && c >= 0);
        StringBuilder sb = new StringBuilder();
        while (c > 32) { sb.Append((char)c); c = Read(); }
        return sb.ToString();
    }

    public int NextInt() => (int)NextLong();

    public long NextLong()
    {
        int c;
        do { c = Read(); } while (c <= 32 && c >= 0);
        long sign = 1;
        if (c == '-') { sign = -1; c = Read(); }
        long v = 0;
        while (c > 32) { v = v * 10 + c - '0'; c = Read(); }
        return v * sign;
    }
}