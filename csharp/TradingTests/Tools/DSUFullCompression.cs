namespace Tools;

class DSUFullCompression // DSU disjoint set union (Union-find )
{
    private readonly Dictionary<string, int> symbolToId = new();
    private int symbolCount = 0;

    private readonly int[] parent;
    private readonly int[] size;
    private int maxClusterSize;

    public DSUFullCompression(int n)
    {
        parent = new int[n];
        size = new int[n];
        for (int i = 0; i < n; i++)
        {
            parent[i] = i;
            size[i] = 1;
        }
    }
    // int Find(int x) // find the root of x & compress
    // {
    //     // Iterative with path compression — no recursion, no stack risk on chains
    //     int root = x;
    //     while (parent[root] != root) // parent[root] == root means reached root
    //         root = parent[root];
    //
    //     // Second pass: compress
    //     while (parent[x] != root) // sur le chemin tous les saut, passer leur root à root trouvé en haut
    //     {
    //         int next = parent[x];
    //         parent[x] = root;
    //         x = next;
    //     }
    //
    //     return root;
    // }

    private int Find(int x) // find root
    {
        int w = x;
        while (parent[w] != w)
        {
            w = parent[w];
        }
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

    void Union(int x, int y)
    {
        int rootX = Find(x);
        int rootY = Find(y);
        if (rootX == rootY) return; // already same cluster

        if (size[rootX] < size[rootY])
            (rootX, rootY) = (rootY, rootX);

        parent[rootY] = rootX;
        size[rootX] += size[rootY];

        if (size[rootX] > maxClusterSize)
            maxClusterSize = size[rootX];
    }

    int GetOrCreateId(string symbol)
    {
        if (symbolToId.TryGetValue(symbol, out int id))
            return id;

        id = symbolCount++;
        symbolToId[symbol] = id;
        parent[id] = id;
        size[id] = 1;

        // First-ever symbol creates a cluster of size 1
        if (maxClusterSize == 0)
            maxClusterSize = 1;

        return id;
    }
}