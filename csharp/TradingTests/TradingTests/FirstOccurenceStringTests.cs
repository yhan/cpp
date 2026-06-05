using NFluent;

namespace TradingTests;

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

    [Test]
    public void test7()
    {
        int idx = FirstOccurenceIndex("ABCABCABCD", "ABCD");
        Check.That(idx).IsEqualTo(6);
    }
    
}