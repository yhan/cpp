namespace Tools;

/// <summary>
/// Tarjan : this impl
/// second possibility: Kosaraju 
/// </summary>
public class SCCTarjan0Indexed
{
    private readonly List<int>[] _g;
    private readonly int _n;
    private readonly int[] _disc;
    private readonly int[] _low;
    private readonly bool[] _onStack;
    private readonly Stack<int> _sccStack = new();
    private int _counter;

    public SCCTarjan0Indexed(List<int>[] graph, int nodes)
    {
        _g = graph;
        _n = nodes;
        _disc = new int[nodes];
        _low = new int[nodes];
        _onStack = new bool[nodes];
        Array.Fill(_disc, -1); // -1 means unvisited
    }

    public List<List<int>> DetectAll()
    {
        List<List<int>> sccs = new List<List<int>>();

        for (int start = 0; start < _n; start++)
        {
            if (_disc[start] != -1) continue;
            if (_g[start] == null) continue;
            DfsIterative(start, sccs);
        }

        return sccs;
    }

    private static bool IsNonTrivial(List<int> scc, List<int>[] g)
    {
        if (scc.Count > 1) return true;
        // Single-node SCC: check for self-loop
        int u = scc[0];
        if (g[u] == null) return false;
        foreach (int v in g[u])
            if (v == u)
                return true;
        return false;
    }

    List<List<int>> All => this.DetectAll();
    List<List<int>> NonTrivial => All.Where(s => IsNonTrivial(s, _g)).ToList();

    private void DfsIterative(int start, List<List<int>> sccs)
    {
        Stack<(int node, int nextIdx)> dfsStack = new Stack<(int, int)>();

        // First visit of start
        Visit(start);
        dfsStack.Push((start, 0));

        while (dfsStack.Count > 0)
        {
            (int v, int i) = dfsStack.Peek();
            List<int> neighbors = _g[v];

            if (neighbors != null && i < neighbors.Count)
            {
                int w = neighbors[i];
                // Advance v's iterator before potentially descending
                dfsStack.Pop();
                dfsStack.Push((v, i + 1));

                if (_disc[w] == -1) // not visited
                {
                    // Tree edge: "recurse" into w
                    Visit(w);
                    dfsStack.Push((w, 0));
                }
                else if (_onStack[w])
                {
                    // Back edge
                    _low[v] = Math.Min(_low[v], _disc[w]);
                }
                // else: cross/forward edge, ignore
            }
            else
            {
                // Finish v: all neighbors processed
                dfsStack.Pop();

                // Bubble low up to parent if any
                if (dfsStack.Count > 0)
                {
                    int parent = dfsStack.Peek().node;
                    _low[parent] = Math.Min(_low[parent], _low[v]);
                }

                // Root check: emit SCC
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
                    sccs.Add(scc);
                }
            }
        }
    }

    private void Visit(int v)
    {
        _disc[v] = _low[v] = _counter++;
        _sccStack.Push(v);
        _onStack[v] = true;
    }
}