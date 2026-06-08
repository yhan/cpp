namespace TradingTests;

public class RollingMaxTests
{
    /// <summary>
    ///   in arr every sliding window (size=k), find the max of each window
    /// return them in an array
    /// </summary>
    /// <param name="arr"></param>
    /// <param name="k"></param>
    /// <returns></returns>
    public static int[] RollingMax(int[] arr, int k)
    {
        int resCount = arr.Length - k + 1;
        int[] res = new int[resCount];
        int[] deque = new int[arr.Length]; // index of arr, the values are monotonic increasing; at index of i, the 
        int head = 0, tail = 0;

        for (int i = 0; i < arr.Length; i++)
        {
            // remove out dated
            if (i - head >= k)
            {
                //head is out dated
                head++;
            }

            if (arr[tail] > arr[i]) tail++;
            while (arr[head] < arr[i]) // remove 
            {
                head++;
            }

            deque[i] = head;
            if (i - head >= k - 1)
            {
                res[i - k + 1] = arr[deque[]];
            }
        }

        return res;
    }
}