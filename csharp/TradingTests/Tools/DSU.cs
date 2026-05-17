namespace Tools;

class DSU // DSU disjoint set union (Union-find )
{
    private readonly int[] parent;
    private readonly int[] size;
    
    public DSU(int n)
    {
        parent = new int[n];
        size = new int[n];
        for (int i = 0; i < n; i++)
        {
            parent[i] = i;
            size[i] = 1;
        }
    }

    public int Find(int x) // find x's root parent
    {
        while (parent[x] != x) // if root parent[x] == x
        {
            parent[x] = parent[parent[x]]; // path halving (iterative variant)
            x = parent[x];
        }

        return x;
    }

    public bool Union(int a, int b)
    {
        int ra = Find(a), rb = Find(b);
        if (ra == rb) return false;
        if (size[ra] < size[rb]) (ra, rb) = (rb, ra);
        parent[rb] = ra;
        size[ra] += size[rb];
        return true;
    }

    public int Size(int x) => size[Find(x)];
}


