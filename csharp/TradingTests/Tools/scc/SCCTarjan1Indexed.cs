namespace Tools;

/// <summary>
/// Tarjan's SCC algorithm, 1-indexed.
/// Node IDs are expected in [1, nodes]. Slot 0 is unused.
/// Returns only non-trivial SCCs (size > 1, or single node with self-loop).
/// One-shot: call Detect() once per instance.
/// </summary>
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
            if (w == u) return true;
        return false;
    }

    private void Visit(int v)
    {
        _disc[v] = _low[v] = _counter++;
        _sccStack.Push(v);
        _onStack[v] = true;
    }
}