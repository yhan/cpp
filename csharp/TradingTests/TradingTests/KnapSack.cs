using NFluent;

namespace TradingTests;

[TestFixture]
public class MaxProfitShould
{
    public int MaximumProfit(int[] present, int[] future, int budget)
    {

        int n = present.Length;
        int[] dp = new int[budget + 1];
        for (int i = 0; i < n; i++)
        {
            int cost = present[i];
            int gain = future[i] - present[i];
            if (gain <= 0) continue;

            for (int b = budget; b >= cost; b--)
            {
                int take = dp[b - cost] + gain;
                if (take > dp[b])
                    dp[b] = take;
            }
        }

        return dp[budget];
    }

    [Test]
    public void test()
    {
        int pro = MaximumProfit([5, 3, 3], [10, 7, 7], 6);
        Check.That(pro).IsEqualTo(8);
    }
}