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
}