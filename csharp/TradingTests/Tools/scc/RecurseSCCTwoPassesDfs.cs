namespace Tools;

public class RecurseSCCTwoPassesDfs
{
    private readonly List<int>[] _g;      // original graph
    private readonly List<int>[] _gT;     // transpose (reversed edges)
    private readonly int _n;

    public RecurseSCCTwoPassesDfs(List<int>[] graph, int nodes)
    {
        _g = graph;
        _n = nodes;
        _gT = BuildTranspose();
    }

    // ---- PASS 1: DFS on G, push each node when it FINISHES ----
    // Goal: produce a stack ordered so the top is a node from a "source" SCC
    // of the condensation. A node's finish time is "later" if it sits higher
    // in the condensation (its SCC can reach other SCCs but isn't reached).
    // Why this works: in any DFS, the last node to finish in an SCC has the
    // largest finish time of that SCC; comparing across SCCs, the SCC with
    // no incoming inter-SCC edges has the latest finisher overall.
    private void Pass1(int v, bool[] visited, Stack<int> finishOrder)
    {
        visited[v] = true;
        foreach (int w in _g[v] ?? new List<int>())
        {
            if (!visited[w])
                Pass1(w, visited, finishOrder);
        }
        // Critical: push AFTER all descendants are done. This is the
        // post-order moment — equivalent to recording finish time.
        finishOrder.Push(v);
    }

    // ---- PASS 2: DFS on G?, popping from finishOrder ----
    // Each DFS run on G? harvests exactly one SCC. Why:
    // - The popped node is in a source SCC of G's condensation.
    // - Reversing edges turns that source into a SINK in G?.
    // - DFS from a sink can't escape its SCC (no outgoing inter-SCC edges
    //   in G? ? all reachable nodes belong to the same SCC).
    private void Pass2(int v, bool[] visited, List<int> component)
    {
        visited[v] = true;
        component.Add(v);
        foreach (int w in _gT[v] ?? new List<int>())
        {
            if (!visited[w])
                Pass2(w, visited, component);
        }
    }

    public List<List<int>> FindSCCs()
    {
        // ---- Step 1: vanilla DFS on G, recording finish order ----
        // Outer loop handles disconnected graphs — every node gets a chance
        // to be a DFS root if not yet visited.
        bool[] visited1 = new bool[_n];
        Stack<int> finishOrder = new Stack<int>();
        for (int v = 0; v < _n; v++)
        {
            if (!visited1[v] && _g[v] != null)
                Pass1(v, visited1, finishOrder);
        }

        // ---- Step 2: transpose already built in constructor ----
        // (BuildTranspose flips every edge u?w into w?u in _gT.)

        // ---- Step 3: DFS on G? in stack-pop order, one SCC per run ----
        // Pop the top of finishOrder. If unvisited, start a fresh DFS in G?;
        // every node it reaches is one SCC. Mark visited so subsequent pops
        // skip over already-claimed nodes.
        bool[] visited2 = new bool[_n];
        List<List<int>> sccs = new List<List<int>>();
        while (finishOrder.Count > 0)
        {
            int v = finishOrder.Pop();
            if (visited2[v]) continue;     // already part of a harvested SCC
            List<int> component = new List<int>();
            Pass2(v, visited2, component);
            sccs.Add(component);
        }

        return sccs;
    }

    // Build the transpose graph: for every edge u ? w in G, add w ? u in G?.
    // This is the operational core of "reverse the graph" — costs O(V+E).
    private List<int>[] BuildTranspose()
    {
        List<int>[] gT = new List<int>[_n];
        for (int i = 0; i < _n; i++)
            gT[i] = new List<int>();

        for (int u = 0; u < _n; u++)
        {
            if (_g[u] == null) continue;
            foreach (int w in _g[u])
                gT[w].Add(u);              // flip direction
        }
        return gT;
    }
}