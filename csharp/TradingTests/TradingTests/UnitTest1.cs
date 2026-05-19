using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using NFluent;
using NUnit.Framework.Constraints;

namespace TradingTests;

public class Tests
{
    [Test]
    public void reversedPQ()
    {
        // a priority queue priority is negative // OR sorted desc
        var pq = new PriorityQueue<string, long>();
        string ele = pq.EnqueueDequeue("", 0);
    }

    [Test]
    public void cas()
    {
        double a = 0;
        double supposedToBe = 0;
        double compareExchange = Interlocked.CompareExchange(ref a, 42, supposedToBe);
        Assert.That(compareExchange, Is.EqualTo(0));
        Assert.That(a, Is.EqualTo(42));
    }

    [Test]
    public void failedCAS()
    {
        double a = 0;
        double supposedToBe = 1;
        double compareExchange = Interlocked.CompareExchange(ref a, 42, supposedToBe);
        Assert.That(compareExchange, Is.EqualTo(0));
        Assert.That(a, Is.EqualTo(0));
    }

    [Test]
    public void copyToSpan()
    {
        Span<int> dest = new int[1];
        WriteToDest(dest);
        Assert.That(dest[0], Is.EqualTo(42));
    }

    private void WriteToDest(Span<int> dest)
    {
        dest[0] = 42;
    }

    [Test]
    public void MaxHeap()
    {
        var pq = new PriorityQueue<int, int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
        pq = new PriorityQueue<int, int>(new DescendantComparer());
        pq = new PriorityQueue<int, int>();
    }

    [Test]
    public void stringSortOrdinal_vs_cultureInvariant()
    {
        string[] ids = { "Z", "a", "ä", "B" };

// Ordinal: by UTF-16 codepoint
// B (0x42), Z (0x5A), a (0x61), ä (0xE4)
        Console.WriteLine(string.Join(", ", ids.OrderBy(x => x, StringComparer.Ordinal)));// B, Z, a, ä

// Culture (en-US): case-insensitive grouping, ä near a
        Console.WriteLine(string.Join(", ", ids.OrderBy(x => x)));
       // a, ä, B, Z   (typical en-US)

// Culture (sv-SE): ä after z
        ids.OrderBy(x => x); // a, B, Z, ä   (on a Swedish locale machine)
    }

    [Test]
    public void compareCultureInvariant()
    {
        string a = "a";
        string aUmlaut = "ä"; // U+00E4

        // Equality under different comparers
        Console.WriteLine($"Ordinal:           {StringComparer.Ordinal.Equals(a, aUmlaut)}");
        Console.WriteLine($"InvariantCulture:  {StringComparer.InvariantCulture.Equals(a, aUmlaut)}");
        Console.WriteLine($"InvariantCultureIgnoreCase: {StringComparer.InvariantCultureIgnoreCase.Equals(a, aUmlaut)}");

        // Compare results (0 = equal, <0 = first smaller, >0 = first larger)
        Console.WriteLine($"Ordinal compare:           {StringComparer.Ordinal.Compare(a, aUmlaut)}");
        Console.WriteLine($"InvariantCulture compare:  {StringComparer.InvariantCulture.Compare(a, aUmlaut)}");

        // Sort behavior
        string[] arr = { "z", "ä", "a", "b" };
        Array.Sort(arr, StringComparer.InvariantCulture);
        Console.WriteLine($"Invariant sort: {string.Join(",", arr)}");

        Array.Sort(arr, StringComparer.Ordinal);
        Console.WriteLine($"Ordinal sort:   {string.Join(",", arr)}");
    }

    /// <summary>
    /// he index of the specified value in the specified array,
    /// if value is found; otherwise, a negative number.
    /// If value is not found and value is less than one or more elements in array,
    /// the negative number returned is the bitwise complement of the index of the first element
    /// that is larger than value.
    ///
    /// If value is not found and value is greater than all elements in array,
    /// the negative number returned is the bitwise complement of (the index of the last element plus 1).
    /// </summary>
    [Test]
    public void longArray()
    {
        long[] qties = [500, 500, 300, 200, 100, 100];


        int first = Array.BinarySearch(qties, 0, qties.Length, 600, new LongDescendantComparer());
        Console.WriteLine("600 >> " + first); //-1
        Console.WriteLine(~first); // 0  first element which is < target

        
        first = Array.BinarySearch(qties, 0, qties.Length, 500, new LongDescendantComparer());
        Console.WriteLine("500 >> " + first);  // 0

        first = Array.BinarySearch(qties, 0, qties.Length, 300, new LongDescendantComparer());
        Console.WriteLine("300 >> "  + first);
        
        first = Array.BinarySearch(qties, 0, qties.Length, 150, new LongDescendantComparer());
        Console.WriteLine("150 >> " +  ~first); // 4  first element which is < target TODO should do -1 to find the last > target

        first = Array.BinarySearch(qties, 0, qties.Length, 50, new LongDescendantComparer());
        Console.WriteLine("50 >> " + ~first); //-
        
        Array.Reverse(qties);
    }

    [Test]
    public void longArray2()
    {
        long[] qties = [100,200,300,300,500,500];
        Array.Sort(qties);


        int first = Array.BinarySearch(qties, 0, qties.Length, 600);
        Console.WriteLine("600 >> " + first); //-5
        Console.WriteLine(~first); // 6 == length  bigger than everything 
        
        
        first = Array.BinarySearch(qties, 0, qties.Length, 500);
        Console.WriteLine("500 >> " + first); // 4 til 4
        Console.WriteLine(~first);

        first = Array.BinarySearch(qties, 0, qties.Length, 300); 
        Console.WriteLine("300 >> " +  first); // 2 til 2 

        first = Array.BinarySearch(qties, 0, qties.Length, 150);
        Console.WriteLine("150 >> " + first); // -2
        Console.WriteLine("150 >> " + ~first); // 1 // first element is lager than target 

        
        first = Array.BinarySearch(qties, 0, qties.Length, 100);
        Console.WriteLine("100 >> " + first); //0 til 0
        
        
        first = Array.BinarySearch(qties, 0, qties.Length, 50);
        Console.WriteLine("50 >> " + first); //-1
        Console.WriteLine(~first); // 0 // first index which is larger than target 
    }

    [Test]
    public void testLowBound()
    {
        int[] arr = [10,20,30];
        var lowBound = LowBound(arr, 5);
        Check.That(lowBound).IsEqualTo(0);

        lowBound = LowBound(arr, 15);
        Check.That(lowBound).IsEqualTo(1);

        lowBound = LowBound(arr, 20);
        Check.That(lowBound).IsEqualTo(1);


        lowBound = LowBound(arr, 30);
        Check.That(lowBound).IsEqualTo(2);

        lowBound = LowBound(arr, 31);
        Check.That(lowBound).IsEqualTo(arr.Length);
    }

    [Test]
    public void testHiBound()
    {
        int[] arr = [10, 20, 30];
        var lowBound = HighBound(arr, 5);
        Check.That(lowBound).IsEqualTo(-1); // first index -1

        lowBound = HighBound(arr, 10);
        Check.That(lowBound).IsEqualTo(0);


        lowBound = HighBound(arr, 15);
        Check.That(lowBound).IsEqualTo(0);

        lowBound = HighBound(arr, 31);
        Check.That(lowBound).IsEqualTo(arr.Length - 1);

    }
    
    public int LowBound(int[] arr, int lowbound) // look for index value >= lowbound
    {
        var test = Array.BinarySearch(arr, lowbound);
        if (test >= 0) return test;
        return ~test;
    }

    public int HighBound(int[] arr, int hibound) // look for index value <= hibound
    {
        var test = Array.BinarySearch(arr, hibound);
        if (test >= 0) return test;
        return ~test - 1;
    }

    [Test]
    public void draft()
    {
        int n = 100;
        int tsindex = 90;
        // Parse into oversized buffer first
        int[] tmp = new int[n + 1];
// ... fill tmp[1..tsindex] ...

// Then resize to fit
        int[] times = new int[tsindex + 1]; // 1-indexed, exactly fits
        Array.Copy(tmp, times, tsindex + 1);

    }

    /// <summary>
    /// C# type/keyword	Approximate range	Size
///    float ±1.5 x 10−45 to ±3.4 x 1038 4 bytes
///    double ±5.0 × 10−324 to ±1.7 × 10308 8 bytes
///    decimal ±1.0 x 10-28 to ±7.9228 x 1028 16 bytes
///
    /// </summary>
    [Test]
    public void decimalTest()
    {
        SortedSet<long> prices = new();
        prices.Add(100);
        prices.Add(105);
        prices.Add(110);

// Successor: smallest key strictly > 105
        SortedSet<long> successor = prices.GetViewBetween(106, long.MaxValue);
    }

    [Test]
    public void verifyRecord()
    {
        Record record = new Record("dfqsfsq", 42);
        Check.That(record.Id).IsEqualTo("dfqsfsq");
        Check.That(record.Len).IsEqualTo(42);
    }

    [Test]
    public void cloneArray()
    {
        int[] a = [1, 2, 3];
        int[] b = (int[])a.Clone();
        Check.That(b).ContainsExactly(1, 2, 3);

        StringBuilder sb = new("CYCLE ");
        bool x = false;
        var b1 = x && true;
    }

    [Test]
    public void testSorted()
    {
        SortedSet<long> quoteIds = new SortedSet<long>();
        quoteIds.Add(91);
        quoteIds.Add(1);
        quoteIds.Add(5);
        quoteIds.Add(-2);
        foreach (var VARIABLE in quoteIds)
        {
            Console.WriteLine(VARIABLE);
        }

        SortedList<long, long> quoteIdsList = new(); // is key value
        foreach (var VARIABLE in quoteIds)
        {
            Console.WriteLine(VARIABLE);
        }
    }

    private static (int[] indices, long[] values) Compress(long[] prices)
    {
        long[] sorted = (long[])prices.Clone();
        Array.Sort(sorted);

        int w = 1;
        for (int i = 1; i < sorted.Length; i++)
        {
            if (sorted[i] != sorted[i - 1])
                sorted[w++] = sorted[i];
        }

        long[] values = new long[w + 1];
        Array.Copy(sorted, 0, values, 1, w);

        int[] indices = new int[prices.Length];
        for (int i = 0; i < prices.Length; i++)
        {
            int lo = 1, hi = w;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (values[mid] < prices[i]) lo = mid + 1;
                else hi = mid;
            }

            indices[i] = lo;
        }

        return (indices, values);
    }

    public class DescComparer : IComparer<(long, long)>
    {
        
        public int Compare((long, long) x, (long, long) y)
        {
            var item1Comparison = Comparer<long>.Default.Compare(x.Item1, y.Item1);
            if (item1Comparison != 0) return item1Comparison;
            return Comparer<long>.Default.Compare(x.Item2, y.Item2);
        }
    }

    record Record(string Id, int Len);
    readonly struct Event
    {
        public readonly string Name;
        public readonly long Time;
        public readonly int U;
        public readonly int V;

        public Event(string name, long ts, int u, int v)
        {
            Name = name;
            Time = ts;
            U = u;
            V = v;
        }
    }
    
}



public class DescendantComparer : IComparer<int>
{
    public int Compare(int x, int y)
    {
        return y.CompareTo(x);
    }
}




public class LongDescendantComparer : IComparer<long>
{
    public int Compare(long x, long y)
    {
        return y.CompareTo(x);
    }
}