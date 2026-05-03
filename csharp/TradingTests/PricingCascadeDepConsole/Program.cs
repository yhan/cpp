/*
 *
 * Question 4 — Pricing Cascade Recalculation
Difficulty: Medium-hard | Recommended timing: 50-65 minutes
Pricing components depend on one another. A rule u v means component v uses the output of component u, so u must be
recalculated before v. Given a batch of changed components, output the lexicographically smallest valid recalculation order for
every impacted component: each changed component and every component reachable from it. If the impacted subgraph contains
a cycle, print CYCLE.
Input format
• The first line contains N and M.
• Each of the next M lines contains u v, meaning u influences v.
• The next line contains K followed by K changed component ids.
Output format
• If there is a cycle in the impacted subgraph, print CYCLE.
• Otherwise print the number of impacted components, then the recomputation order.
Constraints
• 1 <= N <= 2 * 10^5
• 0 <= M <= 2 * 10^5
• 1 <= K <= N
• 1 <= u, v <= N
Sample cases
Sample 1
Input
6 5
1 3
2 3
3 4
2 5
5 6
1 2
Output
6
1 2 3 4 5 6
Sample 2 CYCLE
Input
4 4
1 2
2 3
3 2
3 4
1 1
Edge cases to watch 
• Only the impacted subgraph matters. A cycle outside the impacted set should not affect the answer. 
• When multiple components are ready, choose the smallest id. 
• Disconnected impacted components are possible. 
• A changed component with no dependents still appears in the output.
 */

class Program
{
    static void Main(string[] args)
    {
        var fs = new FastScanner();
        int n = fs.NextInt();
        int m = fs.NextInt();
        List<int>[] g = new List<int>[n + 1];
        List<int>[] rev = new List<int>[n + 1]; // for searching all involved nodes, which are not in the initial dep path from input
        int[] indegCount = new int[n + 1]; // the position is v, the value is nb of dependencies of v
        for (int i = 0; i < m; i++)
        {
            var u = fs.NextInt();
            var v = fs.NextInt();
            if (g[u] == null) g[u] = new List<int>();
            g[u].Add(v);

            if (rev[v] == null) rev[v] = new List<int>();
            rev[v].Add(u);
            indegCount[v]++;
        }

        int k = fs.NextInt();
        HashSet<int> impactedSet = BuildImpactedSet(k, fs, g, rev);

        // run the min heap
        PriorityQueue<int, int> pq = new PriorityQueue<int, int>(); // value is node id (lexically important ), priority is node id also
        List<int> result = new List<int>();
        foreach (var ready in impactedSet)
        {
            if (indegCount[ready] == 0) // no dependency at all, should be the start of walk
                pq.Enqueue(ready, ready);
        }

        while (pq.TryDequeue(out int node, out int priority))
        {
            result.Add(node);
            if (g[node] != null)
            {
                foreach (int next in g[node])
                {
                    if (--indegCount[next] == 0)
                        pq.Enqueue(next, next);
                }
            }
        }

        bool cycled = result.Count < impactedSet.Count;
        if (cycled) Console.WriteLine("CYCLE");
        else
        {
            Console.WriteLine(result.Count);
            Console.WriteLine(string.Join(' ', result));
        }
    }
    
// bfs to find all impacted nodes
    private static HashSet<int> BuildImpactedSet(int k, FastScanner fs, List<int>[] g, List<int>[] rev)
    {
        var toolQ = new Queue<int>();
        HashSet<int> impactedSet = new HashSet<int>();
        for (int i = 0; i < k; i++)
        {
            var startImpacted = fs.NextInt();
            impactedSet.Add(startImpacted);
            toolQ.Enqueue(startImpacted);
        }

        // should add also nodes not in changedNodes 
        while (toolQ.TryDequeue(out int node))
        {
            if (g[node] != null)
            {
                foreach (var dep in g[node])
                {
                    if (impactedSet.Add(dep))
                        toolQ.Enqueue(dep);
                }
            }

            if (rev[node] != null)
            {
                foreach (var revDep in rev[node])
                {
                    if (impactedSet.Add(revDep))
                        toolQ.Enqueue(revDep);
                }
            }
        }

        return impactedSet;
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