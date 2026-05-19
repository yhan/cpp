namespace Tools;

internal sealed class Fenwick
{
    private readonly long[] _tree;
    private readonly int _n;

    public Fenwick(int size)
    {
        _n = size;
        _tree = new long[size + 1]; // 1-indexed
    }

    public int Size => _n;

    // Point update: tree[i] += delta. O(log n).
    public void Update(int i, long delta) // LOG N
    {
        for (; i <= _n; i += i & -i)
            _tree[i] += delta;
    }

    // Prefix sum tree[1..i]. O(log n).
    public long Prefix(int i) // LOG N
    {
        long sum = 0;
        for (; i > 0; i -= i & -i)
            sum += _tree[i];
        return sum;
    }

    // Range sum tree[l..r] inclusive. O(log n).
    public long Range(int l, int r) // LOG N
    {
        if (r < l) return 0;
        return Prefix(r) - Prefix(l - 1);
    }

    // Fenwick descent: smallest index k such that Prefix(k) >= target.
    // Returns -1 if total sum < target. O(log n).
    // Requires all stored values to be non-negative — true here since we
    // only store active qty (cancellations zero out the entry, never go negative on a prefix).
    public int LowerBound(long target) // LOG N
    {
        if (target <= 0) return 0;

        int idx = 0;
        long acc = 0;

        // Largest power of two <= _n.
        int bit = 1;
        while ((bit << 1) <= _n) bit <<= 1;

        for (; bit > 0; bit >>= 1)
        {
            int next = idx + bit;
            if (next <= _n && acc + _tree[next] < target)
            {
                idx = next;
                acc += _tree[next];
            }
        }

        int result = idx + 1;
        return result > _n ? -1 : result;
    }
}
