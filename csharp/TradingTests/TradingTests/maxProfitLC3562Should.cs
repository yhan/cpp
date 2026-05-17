using System;
using System.Collections.Generic;

namespace TradingTests;

[TestFixture]
public class maxProfitLC3562Should
{
}


public class Solution
{
    private int[] present;
    private int[] future;
    private List<int>[] children;
    private int budget;
    private const int NEG = int.MinValue / 4;

    public int MaxProfit(int n, int[] present, int[] future, int[][] hierarchy, int budget)
    {
        this.present = present;
        this.future = future;
        this.budget = budget;

        children = new List<int>[n + 1];
        for (int i = 1; i <= n; i++) children[i] = new List<int>();
        foreach (int[] edge in hierarchy)
        {
            int u = edge[0];
            int v = edge[1];
            children[u].Add(v);
        }

// Root is employee 1, faces full price (no boss above)
        (int[] full, int[] _) = Solve(1);

        int best = 0;
        for (int b = 0; b <= budget; b++)
        {
            if (full[b] > best) best = full[b];
        }

        return best;
    }

// Returns two dp arrays for the subtree rooted at u:
// full[b] = max profit in subtree, given u faces full price (present[u])
// half[b] = max profit in subtree, given u faces half price (present[u]/2)
    private (int[] full, int[] half) Solve(int u)
    {
// Aggregate children profits under each scenario for u itself
        int[] childrenIfUNotBuy = new int[budget + 1]; // children face full price
        int[] childrenIfUBuy = new int[budget + 1]; // children face half price

        foreach (int c in children[u])
        {
            (int[] cFull, int[] cHalf) = Solve(c);
            childrenIfUNotBuy = Merge(childrenIfUNotBuy, cFull);
            childrenIfUBuy = Merge(childrenIfUBuy, cHalf);
        }

        int[] full = BuildDp(u, present[u - 1], childrenIfUNotBuy, childrenIfUBuy);
        int[] half = BuildDp(u, present[u - 1] / 2, childrenIfUNotBuy, childrenIfUBuy);
        return (full, half);
    }

// Build dp[b] for node u given its own cost and the two aggregated children arrays
    private int[] BuildDp(int u, int costU, int[] childrenNotBuy, int[] childrenBuy)
    {
        int gainU = future[u - 1] - costU;
        int[] dp = new int[budget + 1];

        for (int b = 0; b <= budget; b++)
        {
// Option 1: u does not buy ? children face full price
            int notBuy = childrenNotBuy[b];

// Option 2: u buys ? spends costU, gains gainU, children face half price
            int buy = NEG;
            if (costU <= b/* && gainU > 0 */ )
            {
                buy = childrenBuy[b - costU] + gainU;
            }

            dp[b] = notBuy > buy ? notBuy : buy;
        }

        return dp;
    }

// Combine two budget?profit arrays: c[b] = max over j of a[j] + b[b-j]
    private int[] Merge(int[] a, int[] b)
    {
        int[] c = new int[budget + 1];
        for (int i = 0; i <= budget; i++)
        {
            int ai = a[i];
            if (ai < 0 && ai <= NEG) continue;
            int limit = budget - i;
            for (int j = 0; j <= limit; j++)
            {
                int sum = ai + b[j];
                if (sum > c[i + j]) c[i + j] = sum;
            }
        }

        return c;
    }
}