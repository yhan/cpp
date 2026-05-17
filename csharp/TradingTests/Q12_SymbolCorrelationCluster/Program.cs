using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Text;
using System;

/*
Q12 — Symbol Correlation Cluster Builder
Difficulty: Medium | 35-45 min
A risk system tracks correlated symbols. Events arrive in non-decreasing timestamp order:

LINK t a b — symbols a and b are now considered correlated (transitively). If they were already in the same cluster, this is a no-op.
BREAK t a b — invalid event (you cannot un-correlate symbols in this simplified model); count it and ignore. No state change.
QUERY t a b — output YES if a and b are in the same correlation cluster, else NO. If either symbol has never appeared in a LINK, output NO.
LARGEST t — output the size of the biggest cluster (number of distinct symbols in it). If no clusters exist yet, output 0.
Constraints: N ≤ 2×10⁵, symbols are strings up to 16 chars, ≤ 10⁵ distinct symbols.
Edge cases:

LINK a a — symbol self-link, valid no-op
LINK between two already-linked symbols — no-op, doesn't count as invalid
QUERY on a symbol that has never been seen — NO
LARGEST before any LINK — 0 Hint: Union-Find with path compression and union by rank/size. Track the max cluster size as you union — LARGEST is then O(1). Classic application; nearly O(N) total with α(N) inverse-Ackermann.
*/
class Solution // DSU disjoint set union (Union-find )
{
    public static void Main(string[] args)
    {
        // Symbol interning: string ↔ int id
        Dictionary<string, int> _symbolToId = new(capacity: 100_001);

// DSU arrays — index by symbol id    CORE structures
        int[] _parent = new int[100_001];    
        int[] _size = new int[100_001];

// Running state
        int _symbolCount = 0; // also serves as next id to assign
        int _maxClusterSize = 0; // for LARGEST in O(1)

        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        var fs = new FastScanner();
        var result = new StringBuilder();


        int nevents = fs.NextInt();
        for (int i = 0; i < nevents; i++)
        {
            string name = fs.Next();
            switch (name)
            {
                case "LINK": // LINK 1006 GOOG NVDA
                {
                    fs.Next(); // ts not important
                    string a = fs.Next();
                    string b = fs.Next();
                    int ai = GetOrCreateId(a);
                    int bi = GetOrCreateId(b);
                    Union(ai, bi);
                }
                    break;
                case "QUERY": // QUERY 1002 AAPL GOOG
                {
                    fs.Next(); // ignore ts
                    string a = fs.Next();
                    string b = fs.Next();
                    if (_symbolToId.TryGetValue(a, out int ai) == false ||
                        _symbolToId.TryGetValue(b, out int bi) == false)
                    {
                        result.AppendLine("NO");
                        break;
                    }
                    if (Find(ai) == Find(bi)) result.AppendLine("YES");
                    else result.AppendLine("NO");
                }
                    break;
                case "LARGEST": // LARGEST 1005
                {
                    fs.Next(); // ts not important
                    result.AppendLine(_maxClusterSize.ToString());
                }
                    break;
                case "BREAK": //BREAK 1009 AAPL MSFT
                {
                    fs.Next();
                    fs.Next();
                    fs.Next();
                }
                    break;
            }
        }

        Console.Write(result);


        int Find(int x) // find the root of x & compress
        {
            // Iterative with path compression — no recursion, no stack risk on chains
            int root = x;
            while (_parent[root] != root) // parent[root] == root means reached root
                root = _parent[root];

            // Second pass: compress
            while (_parent[x] != root) // sur le chemin tous les saut, passer leur root à root trouvé en haut
            {
                int next = _parent[x];
                _parent[x] = root;
                x = next;
            }

            return root;
        }

        void Union(int x, int y)
        {
            int rootX = Find(x);
            int rootY = Find(y);
            if (rootX == rootY) return; // already same cluster

            if (_size[rootX] < _size[rootY])
                (rootX, rootY) = (rootY, rootX);

            _parent[rootY] = rootX;
            _size[rootX] += _size[rootY];

            if (_size[rootX] > _maxClusterSize)
                _maxClusterSize = _size[rootX];
        }

        int GetOrCreateId(string symbol)
        {
            if (_symbolToId.TryGetValue(symbol, out int id))
                return id;

            id = _symbolCount++;
            _symbolToId[symbol] = id;
            _parent[id] = id;
            _size[id] = 1;

            // First-ever symbol creates a cluster of size 1
            if (_maxClusterSize == 0)
                _maxClusterSize = 1;

            return id;
        }
    }
}


class FastScanner
{
    private readonly byte[] data = new byte[1 << 16];
    private int len, ptr;
    private readonly Stream stdin = Console.OpenStandardInput();
    private bool eof;

    public bool HasNext()
    {
        int c;
        do
        {
            c = Read();
        } while (c >= 0 && c <= 32);

        if (c < 0) return false;
        ptr--; // put it back
        return true;
    }

    private int Read()
    {
        if (eof) return -1;
        if (ptr >= len)
        {
            len = stdin.Read(data, 0, data.Length);
            ptr = 0;
            if (len <= 0)
            {
                eof = true;
                return -1;
            }
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

    public double NextDouble()
    {
        int c;
        do
        {
            c = Read();
        } while (c <= 32 && c >= 0);

        // Handle optional sign
        bool negative = false;
        if (c == '-')
        {
            negative = true;
            c = Read();
        }
        else if (c == '+')
        {
            c = Read();
        }

        // Integer part
        double v = 0;
        while (c > 32 && c != '.' && c != 'e' && c != 'E')
        {
            v = v * 10 + (c - '0');
            c = Read();
        }

        // Fractional part
        if (c == '.')
        {
            c = Read();
            double factor = 0.1;
            while (c > 32 && c != 'e' && c != 'E')
            {
                v += (c - '0') * factor;
                factor *= 0.1;
                c = Read();
            }
        }

        // Exponent part (e.g., 1.5e-3)
        if (c == 'e' || c == 'E')
        {
            c = Read();
            bool expNegative = false;
            if (c == '-')
            {
                expNegative = true;
                c = Read();
            }
            else if (c == '+')
            {
                c = Read();
            }

            int exp = 0;
            while (c > 32)
            {
                exp = exp * 10 + (c - '0');
                c = Read();
            }

            v *= Math.Pow(10, expNegative ? -exp : exp);
        }

        return negative ? -v : v;
    }

    public decimal NextDecimal() => decimal.Parse(Next(), CultureInfo.InvariantCulture);
}