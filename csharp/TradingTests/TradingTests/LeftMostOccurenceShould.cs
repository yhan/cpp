using System.Collections.Generic;

namespace TradingTests;

[TestFixture]
public class LeftMostOccurenceShould
{
    private int LeftMostIndex(int target, List<int> arr)
    {
        int l = 0, r = arr.Count - 1;
        int lastEq = -1;
        while (l<=r)
        {
            int mididx = (l + r) >> 1;
            int val = arr[mididx];
            if (target < val)
            {
                r = mididx - 1;
            }
            else if (target > val) l = mididx + 1;
            else //target found 
            {
                r = mididx - 1;
                lastEq = mididx;
            }
        }

        return lastEq;
    }

    private int RightMostIndex(int target, List<int> arr)
    {
        int l = 0, r = arr.Count - 1;
        int lastEq = -1;
        while (l <= r)
        {
            int mididx = (l + r) >> 1;
            int val = arr[mididx];
            if (target < val)
            {
                r = mididx - 1;
            }
            else if (target > val) l = mididx + 1;
            else //target found 
            {
                l = mididx + 1;
                lastEq = mididx;
            }
        }

        return lastEq;
    }
}