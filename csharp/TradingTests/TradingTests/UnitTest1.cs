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
}