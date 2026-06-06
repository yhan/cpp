using NFluent;

namespace TradingTests;

/// <summary>
/// KMP "longest prefix suffix"
/// instead of rewind
///
/// index:    0 1 2 3 4 5 6
/// haystack:
///         \/
/// A B A B A B C
/// needle:
///     \/
/// A B A B C
/// </summary>
public class FirstOccurenceStringTests
{
    int FirstOccurenceIndex(string a, string sub)
    {
        int j = 0;
        int cursor = 0;
        for (int i = 0; i < a.Length;)
        {
            if (a[i] != sub[j])
            {
                i = ++cursor;
                j = 0;
                continue;
            }

            if (j == sub.Length - 1)
                return cursor;

            i++;
            j++;
        }
        return -1;
    }

    public int StrStr(string haystack, string needle)
    {
        if (needle.Length == 0) return 0;

        int m = needle.Length;
        int[] lps = new int[m]; // built over the NEEDLE
        int len = 0;
        int i = 1;
        while (i < m)
        {
            if (needle[i] == needle[len])
            {
                len++;
                lps[i] = len;
                i++;
            }
            else if (len > 0)
            {
                len = lps[len - 1];
            }
            else
            {
                lps[i] = 0;
                i++;
            }
        }

        int h = 0; // haystack pointer — never rewinds
        int k = 0; // needle pointer — rewinds via lps
        int n = haystack.Length;
        while (h < n)
        {
            if (haystack[h] == needle[k])
            {
                h++;
                k++;
                if (k == m) return h - m;
            }
            else if (k > 0)
            {
                k = lps[k - 1];
            }
            else
            {
                h++;
            }
        }

        return -1;
    }
    [Test]
    public void test7()
    {
        int idx = FirstOccurenceIndex("ABCABCABCD", "ABCD");
        Check.That(idx).IsEqualTo(6);
    }

    [Test]
    public void test4()
    {
        int idx = FirstOccurenceIndex("abcxkjlbcd", "bcd");
        Check.That(idx).IsEqualTo(7);
    }

    [Test]
    public void test()
    {
        int idx = FirstOccurenceIndex("a", "a");
        Check.That(idx).IsEqualTo(0);
    }

    [Test]
    public void test2()
    {
        int idx = FirstOccurenceIndex("abc", "a");
        Check.That(idx).IsEqualTo(0);
    }


    [Test]
    public void test3()
    {
        int idx = FirstOccurenceIndex("abc", "bc");
        Check.That(idx).IsEqualTo(1);
    }

    [Test]
    public void test5()
    {
        int idx = FirstOccurenceIndex("azertyu", "zeh");
        Check.That(idx).IsEqualTo(-1);
    }

    [Test]
    public void test6()
    {
        int idx = FirstOccurenceIndex("a", "b");
        Check.That(idx).IsEqualTo(-1);
    }
}