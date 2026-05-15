namespace Tools;

internal class ClassicDijkstra // worst case O( E + V * log(V) )
{
    public static int Dijkstra() // compute min cost from st to end
    {
        int sz = 10000;
        List<int>[] g = new List<int>[sz];
        Dictionary<(int, int), int> weights = new Dictionary<(int, int), int>(); // v1,v2 => weight
        // need a PQ to track the min cost so far

        int st = 5;
        int end = 17;

        /// State
        // -----------------------
        //int mincost = 0;
        PriorityQueue<int, int> pq = new();
        int[] sf = new int[sz]; // cumulative cost so far
        bool[] visited = new bool[sz]; // not visited by default

        // -----------------------

        for (int i = 0; i < sz; i++)
            sf[i] = int.MaxValue;

        pq.Enqueue(st, 0);
        sf[st] = 0; /// the same node, no cost

        while (pq.Count > 0)
        {
            int x = pq.Dequeue();
            if (visited[x])
            {
                // is already the smallest
                continue;
            }

            visited[x] = true;
            if (x == end)
                return sf[x];

            List<int> adj = g[x];
            if (adj == null)
                continue;

            foreach (var a in adj)
            {
                int w = weights[(x, a)];
                if (sf[x] + w < sf[a])
                {
                    sf[a] = sf[a] + w;
                    pq.Enqueue(a, sf[a]);
                }

            }
        }

        return int.MaxValue; // not reachable !
    }
}