using System;
using System.Collections.Generic;
using System.Linq;

namespace TradingTests;

/// <summary>
/// Top K plus grand/ plus petit /plus fréqeunt
/// 
/// </summary>
public class TopKElementTests
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="arr"> is not sorted </param>
    /// <param name="k"></param>
    /// <returns>
    /// does not required to be sorted,
    /// allow duplicates
    /// </returns>
    private static int[] TopKSmallest(int[] arr, int k)
    {
        if (k >= arr.Length) return arr;
        PriorityQueue<int, int> maxHeap = new(new DescComparer());
        for (int i = 0; i < arr.Length; i++)
        {
            int ele = arr[i];
            if (i < k)
            {
                maxHeap.Enqueue(ele, ele);
                continue;
            }

            if (maxHeap.Peek() > ele)
            {
                maxHeap.DequeueEnqueue(ele, ele);
            }
        }

        int[] result = new int[k];
        int c = 0;
        foreach (var e in maxHeap.UnorderedItems)
        {
            result[c] = e.Element;
            c++;
        }

        return result;
    }

    private static int[] TopKSmallestNoDuplicates(int[] arr, int k)
    {
        if (k >= arr.Length) return arr.Distinct().ToArray();
        PriorityQueue<int, int> maxHeap = new(new DescComparer());
        HashSet<int> mem = new();
        int counterEnqueued = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            int ele = arr[i];
            if (!mem.Add(ele)) continue;
            if (counterEnqueued < k)
            {
                maxHeap.Enqueue(ele, ele);
                counterEnqueued++;
                continue;
            }

            if (maxHeap.Peek() > ele)
            {
                maxHeap.DequeueEnqueue(ele, ele);
            }
        }

        int[] result = new int[counterEnqueued]; // if really distinct k elements, then counterEnqueued == k; otherwise, counterEnqueued < k
        int c = 0;
        foreach (var e in maxHeap.UnorderedItems)
        {
            result[c] = e.Element;
            c++;
        }

        return result;
    }

    private static int[] TopKMostFrequent(int[] arr, int k) // O(n + m log(k))
    {
        //     Dictionary<int, int> counterMap = arr.GroupBy(x => x)
        //         .ToDictionary(x => x.Key, x => x.Count()); // 2 passes
        Dictionary<int, int> counterMap = new(); // 1 pass
        foreach (int x in arr)
        {
            counterMap.TryGetValue(x, out int v);
            counterMap[x] = v + 1;
        }

        int sz = Math.Min(counterMap.Count, k);
        PriorityQueue<(int, int), int> maxheap = new PriorityQueue<(int, int), int>();
        int c = 0;
        foreach (var kv in counterMap)
        {
            if (c < sz)
            {
                maxheap.Enqueue((kv.Key, kv.Value), kv.Value);
                c++;
                continue;
            }

            (int, int) top = maxheap.Peek();
            if (top.Item2 < kv.Value
                || (top.Item2 == kv.Value && top.Item1 < kv.Key)) // tie breaker : equal freq - smaller number is kept
            {
                maxheap.DequeueEnqueue((kv.Key, kv.Value), kv.Value); // minheap size won't change 
            }
            
        }

        int[] r = new int[sz];
        int counter = 0;
        while (maxheap.TryDequeue(out var ele, out var freq))
        {
            r[counter++] = ele.Item1;
        }

        return r;
    }

    private static int[] TopKMostFrequent_bucketSortByIndex(int[] arr, int k) // O(n + m log(k))
    {
        //     Dictionary<int, int> counterMap = arr.GroupBy(x => x)
        //         .ToDictionary(x => x.Key, x => x.Count()); // 2 passes
        Dictionary<int, int> counterMap = new(); // 1 pass
        foreach (int x in arr)
        {
            counterMap.TryGetValue(x, out int v);
            counterMap[x] = v + 1;
        }
        // range stuff into a bucket array, index being the frequency, value being the numeric value
        // you have at most n slot of bucket int[n+1]
        // fill the bucket and collect from te end

        int n = arr.Length;
        List<int>[] freqBucket = new List<int>[n + 1];
        foreach (var kv in counterMap)
        {
            if (freqBucket[kv.Value] == null)
                freqBucket[kv.Value] = new List<int>();
            freqBucket[kv.Value].Add(kv.Key);
        }
        // collect reversely : Top freq first, equal freq will consume k
        int size = Math.Min(k, counterMap.Count);
        int[] result = new int[size];
        int c = 0;
        for (int i = freqBucket.Length - 1; i > 0 && c < size; i--)
        {
            if (freqBucket[i] != null)
            {
                foreach (var num in freqBucket[i])
                {
                    if (c < size) result[c++] = num;
                }
            }
        }

        return result;
    }
}

internal class DescComparer : IComparer<int>
{
    public int Compare(int x, int y)
    {
        return y.CompareTo(x);
    }
}