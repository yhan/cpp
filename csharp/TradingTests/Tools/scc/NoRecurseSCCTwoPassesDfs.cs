namespace Tools;

public class NoRecurseSCCTwoPassesDfs
{
    private readonly List<int>[] _g;      // original graph
    private readonly List<int>[] _gT;     // transpose (reversed edges)
    private readonly int _n;

    public NoRecurseSCCTwoPassesDfs(List<int>[] graph, int nodes)
    {
        _g = graph;
        _n = nodes;
        _gT = BuildTranspose();
    }

    // ---- PASS 1 (iterative): DFS on G, push each node when it FINISHES ----
    // The recursive version had a natural post-order moment — right after the
    // foreach loop, just before returning. To replicate that iteratively, each
    // frame tracks "which neighbor index am I on?" When the index exhausts the
    // neighbor list, that's the finish event ? push to finishOrder.
    private void Pass1Iterative(int start, bool[] visited, Stack<int> finishOrder)
    {
        Stack<(int node, int nextIdx)> dfs = new Stack<(int, int)>();
        visited[start] = true;
        dfs.Push((start, 0));

        while (dfs.Count > 0)
        {
            (int v, int i) = dfs.Peek();
            List<int> neighbors = _g[v];

            if (neighbors != null && i < neighbors.Count)
            {
                // Advance v's bookmark BEFORE potentially descending — otherwise
                // when control returns to v we'd reprocess the same neighbor.
                dfs.Pop();
                dfs.Push((v, i + 1));

                int w = neighbors[i];
                if (!visited[w])
                {
                    visited[w] = true;
                    dfs.Push((w, 0));      // "recurse" into w
                }
                // else: w already visited — vanilla DFS ignores (no SCC logic here)
            }
            else
            {
                // All neighbors processed ? FINISH event.
                // This is the iterative analog of "post-order": the node leaves
                // the DFS stack exactly when its subtree is fully explored.
                dfs.Pop();
                finishOrder.Push(v);
            }
        }
    }

    // ---- PASS 2 (iterative): DFS on G?, collect every reachable node ----
    // Simpler than Pass 1 — no finish event needed. We only care WHICH nodes
    // get reached, not in what order. So we can use the simpler "push all
    // unvisited neighbors" style, or stick with the frame-based form for
    // consistency. I'll use the frame form to match.
    private void Pass2Iterative(int start, bool[] visited, List<int> component)
    {
        Stack<(int node, int nextIdx)> dfs = new Stack<(int, int)>();
        visited[start] = true;
        component.Add(start);
        dfs.Push((start, 0));

        while (dfs.Count > 0)
        {
            (int v, int i) = dfs.Peek();
            List<int> neighbors = _gT[v];

            if (neighbors != null && i < neighbors.Count)
            {
                dfs.Pop();
                dfs.Push((v, i + 1));

                int w = neighbors[i];
                if (!visited[w])
                {
                    visited[w] = true;
                    component.Add(w);      // record on first visit
                    dfs.Push((w, 0));
                }
            }
            else
            {
                dfs.Pop();
                // No finish action needed in Pass 2.
            }
        }
    }

    public List<List<int>> FindSCCs()
    {
        // ---- Step 1: iterative DFS on G, recording finish order ----
        bool[] visited1 = new bool[_n];
        Stack<int> finishOrder = new Stack<int>();
        for (int v = 0; v < _n; v++)
        {
            if (!visited1[v] && _g[v] != null)
                Pass1Iterative(v, visited1, finishOrder);
        }

        // ---- Step 2: transpose already built in constructor ----

        // ---- Step 3: iterative DFS on G? in stack-pop order ----
        bool[] visited2 = new bool[_n];
        List<List<int>> sccs = new List<List<int>>();
        while (finishOrder.Count > 0)
        {
            int v = finishOrder.Pop();
            if (visited2[v]) continue;
            List<int> component = new List<int>();
            Pass2Iterative(v, visited2, component);
            sccs.Add(component);
        }

        return sccs;
    }

    private List<int>[] BuildTranspose()
    {
        List<int>[] gT = new List<int>[_n];
        for (int i = 0; i < _n; i++)
            gT[i] = new List<int>();

        for (int u = 0; u < _n; u++)
        {
            if (_g[u] == null) continue;
            foreach (int w in _g[u])
                gT[w].Add(u);
        }
        return gT;
    }
}