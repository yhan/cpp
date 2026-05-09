using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TradingSystem.Fast;

namespace TradingTests;

public class Tests
{
    [Test]
    public void testFastDiv()
    {
        var fastDivider = new FastDivider(7);
        int divide = fastDivider.Divide(10);
        Assert.That(divide, Is.EqualTo(10/7));
    }

    [Test]
    public void testFastMod()
    {
        var fastDivider = new FastDivider(7);
        int mod = fastDivider.Modulo(22);
        Assert.That(mod, Is.EqualTo(22 % 7));

        StringBuilder sb = new StringBuilder();
        sb.Append(-1);
    }

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