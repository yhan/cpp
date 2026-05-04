namespace TradingTests;

/**
 * Max profit window
Algo trading / market microstructure
Part A
--------------
You are given an array pnl[0..n-1] of integers representing the per-minute realised PnL (in basis points) of an intraday strategy over a trading session.
You are also given an integer k.

A rebalance is triggered when the cumulative PnL over any contiguous window of exactly k minutes exceeds a threshold T. Find how many such windows exist.

Part B
------------
given that you can remove at most one minute from any window (i.e. consider windows of length k or k−1) — find the maximum achievable sum over any such window.

================================================================
Part A — count windows
Input:  pnl = [3, -1, 4, 2, -2, 5, 1, -3, 4, 2], k = 3, T = 5
Output: 4

Windows of length 3 and their sums:
  [3,-1, 4] =  6  ✓
  [-1, 4, 2] =  5  ✗  (strictly greater than T)
  [4, 2,-2] =  4  ✗
  [2,-2, 5] =  5  ✗
  [-2, 5, 1] =  4  ✗
  [5, 1,-3] =  3  ✗
  [1,-3, 4] =  2  ✗
  [-3, 4, 2] =  3  ✗

Wait — let's recount with T=4 (strictly greater):
  [3,-1, 4] =  6  ✓
  [-1, 4, 2] =  5  ✓
  [2,-2, 5] =  5  ✓
  [-2, 5, 1] =  4  ✗
  [-3, 4, 2] =  3  ✗
Output (T=4): 3
================================================================
Part B — best window (drop one minute)
Input:  pnl = [3, -1, 4, 2, -2, 5, 1, -3, 4, 2], k = 4
Output: 14

Explanation:
  Window [2,-2,5,1] has sum 6. Drop -2 → 8.
  Window [5,1,-3,4] has sum 7. Drop -3 → 12.
  Window [-1,4,2,-2] sum 3. Drop -2 → 8.
  Window [3,-1,4,2] sum 8. Drop -1 → 12.
  Window [-2,5,1,-3] drop -3 → 11.
  Window [1,-3,4,2] drop -3 → 10.

  Best window of length k=4: [-3,4,2,... ]
  Try: [4,2,-2,5] → drop -2 → 11
  Try: [-1,4,2,-2] → drop -2 → 8

  Actually best k-length window is [2,-2,5,1,-3,4,2]...
  Re-examine: k=4, best window: [3,4,2,5]?
  Indices 0..3: 3,-1,4,2 sum=8, drop -1 → 12
  Indices 5..8: 5,1,-3,4 sum=7, drop -3 → 12
  Indices 6..9: 1,-3,4,2 sum=4, drop -3 → 10

Output: 12
Constraints
1 ≤ n ≤ 105
1 ≤ k ≤ n
−104 ≤ pnl[i] ≤ 104
Part A: O(n) time required
Part B: O(n) time required — no nested loops
 */
public class PrefixSumTradingTests
{
    [Test]
    public void test()
    {
        int cnt = 5;
        int k = 3;
        int minPnl = 5;

        int[] pnls =
        [
            3, -1, 4, 2, -2, 5, 1, -3, 4, 2
        ];
        Assert.That(countGoodPnl(k, minPnl, pnls), Is.EqualTo(1));
    }

    [Test]
    public void test2()
    {
        int k = 3;
        int minPnl = 4;

        int[] pnls =
        [
            3, -1, 4, 2, -2, 5, 1, -3, 4, 2
        ];
        Assert.That(countGoodPnl( k, minPnl, pnls), Is.EqualTo(3));
    }

    [Test]
    public void test3()
    {
        int[] pnls = [10, 10, -100]; int k = 3;
        int minPnl = 5;
        Assert.That(countGoodPnl(k, minPnl, pnls), Is.EqualTo(0));
    }

    int countGoodPnl(int k, int minPnl, int[] pnls)
    {
        int cnt = pnls.Length;
        int windows = cnt - k + 1;
        int[] cumPnl = new int[windows];
        int resultCnt = 0;

        bool partial = true;
        for (int i = 0; i + k - 1 < cnt; i++)
        {
            cumPnl[i] = i - 1 < 0 ? 0 + pnls[i] : cumPnl[i - 1] + pnls[i];
            if(i >= k-1) partial = false;
            if (i >= k) {
                cumPnl[i] -= pnls[i - k];
            }

            if (!partial && cumPnl[i] > minPnl) 
                resultCnt++;
        }

        return resultCnt;
    }
} 