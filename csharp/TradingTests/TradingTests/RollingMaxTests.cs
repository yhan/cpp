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
    public static int[] RollingMax2(int[] arr, int k)
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

            deque[tail++] = i;
            if (i - head >= k - 1)
            {
                res[i - k + 1] = arr[deque[head]];
            }
        }

        return res;
    }

    public static int[] RollingMax(int[] a, int k)
    {
        int n = a.Length;
        int[] dq = new int[n]; // ring not even needed; n is a safe upper bound
        int head = 0, tail = 0; // [head, tail) holds indices, values decreasing
        int[] result = new int[n - k + 1];
        int r = 0;

        for (int i = 0; i < n; i++)
        {
            // drop indices outside the window
            if (head < tail && dq[head] <= i - k)
                head++;

            // drop smaller values from the back
            while (head < tail && a[dq[tail - 1]] <= a[i])
                tail--;

            dq[tail++] = i;

            if (i >= k - 1)
                result[r++] = a[dq[head]];
        }

        return result;
    }
}