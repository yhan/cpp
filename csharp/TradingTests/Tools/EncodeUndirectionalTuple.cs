namespace Tools;

public class EncodeUndirectionalTuple
{
    private static long Key(int u, int v)
    {
        int a = Math.Min(u, v);
        int b = Math.Max(u, v);
        return ((long)a << 32) | (uint)b;
    }
}