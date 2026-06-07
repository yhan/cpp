using System;

namespace TradingTests;

public class K_thQuickSelect
{
    private static readonly Random Rng = new Random();

    // Reorders arr so that arr[0..k-1] hold the k smallest elements (unordered among themselves).
    // Returns a freshly-allocated array containing those k elements.
    private static int[] KSmallest(int[] arr, int k)
    {
        if (k <= 0) return new int[0];
        if (k >= arr.Length)
        {
            int[] all = new int[arr.Length];
            Array.Copy(arr, all, arr.Length);
            return all;
        }

        QuickSelect(arr, 0, arr.Length - 1, k - 1);  // place rank k-1 at index k-1

        int[] result = new int[k];
        Array.Copy(arr, result, k);
        return result;
    }

    private static void QuickSelect(int[] arr, int lo, int hi, int k)
    {
        while (lo < hi)
        {
            int p = Partition(arr, lo, hi);
            if (p == k) return;
            else if (p < k) lo = p + 1;
            else hi = p - 1;
        }
    }

    private static int Partition(int[] arr, int lo, int hi)
    {
        int r = lo + Rng.Next(hi - lo + 1);   // random pivot to dodge O(n^2)
        Swap(arr, r, hi);

        int pivot = arr[hi];
        int i = lo;                            // arr[lo..i-1] are < pivot
        for (int j = lo; j < hi; j++)
        {
            if (arr[j] < pivot)
            {
                Swap(arr, i, j);
                i++;
            }
        }
        Swap(arr, i, hi);                      // pivot to its final position
        return i;
    }

    private static void Swap(int[] arr, int a, int b)
    {
        (arr[a], arr[b]) = (arr[b], arr[a]);
    }

}