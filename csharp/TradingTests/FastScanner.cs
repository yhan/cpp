using System;
using System.Collections.Generic;
using System.IO;

class FastScannerOpt
{
    private readonly byte[] data = new byte[1 << 16];
    private int len, ptr;

    private int Read()
    {
        if (ptr >= len)
        {
            len = Console.OpenStandardInput().Read(data, 0, data.Length);
            ptr = 0;
            if (len == 0) return -1;
        }

        return data[ptr++];
    }

    public int NextInt() => (int)NextLong();

    public long NextLong()
    {
        int c;
        do
        {
            c = Read();
        } while (c <= 32 && c >= 0);

        int sign = 1;
        if (c == '-')
        {
            sign = -1;
            c = Read();
        }

        long v = 0;
        while (c > 32)
        {
            v = v * 10 + c - '0';
            c = Read();
        }

        return v * sign;
    }
}
class FastScanner
{
    private readonly byte[] data = new byte[1 << 16];
    private int len, ptr;
    private readonly Stream stdin = Console.OpenStandardInput();
    private bool eof;

    public bool HasNext()
    {
        int c;
        do
        {
            c = Read();
        } while (c >= 0 && c <= 32);

        if (c < 0) return false;
        ptr--; // put it back
        return true;
    }

    public decimal NextDecimal() => decimal.Parse(Next(), CultureInfo.InvariantCulture);
    private int Read()
    {
        if (eof) return -1;
        if (ptr >= len)
        {
            len = stdin.Read(data, 0, data.Length);
            ptr = 0;
            if (len <= 0)
            {
                eof = true;
                return -1;
            }
        }

        return data[ptr++];
    }

    public string Next()
    {
        int c;
        do
        {
            c = Read();
        } while (c <= 32 && c >= 0);

        var chars = new List<char>();
        while (c > 32)
        {
            chars.Add((char)c);
            c = Read();
        }

        return new string(chars.ToArray());
    }

    public int NextInt() => (int)NextLong();

    public long NextLong()
    {
        int c;
        do
        {
            c = Read();
        } while (c <= 32 && c >= 0);

        long v = 0;
        while (c > 32)
        {
            v = v * 10
                + c - '0';
            c = Read();
        }

        return v;
    }
    public double NextDouble()
    {
        int c;
        do
        {
            c = Read();
        } while (c <= 32 && c >= 0);

        // Handle optional sign
        bool negative = false;
        if (c == '-')
        {
            negative = true;
            c = Read();
        }
        else if (c == '+')
        {
            c = Read();
        }

        // Integer part
        double v = 0;
        while (c > 32 && c != '.' && c != 'e' && c != 'E')
        {
            v = v * 10 + (c - '0');
            c = Read();
        }

        // Fractional part
        if (c == '.')
        {
            c = Read();
            double factor = 0.1;
            while (c > 32 && c != 'e' && c != 'E')
            {
                v += (c - '0') * factor;
                factor *= 0.1;
                c = Read();
            }
        }

        // Exponent part (e.g., 1.5e-3)
        if (c == 'e' || c == 'E')
        {
            c = Read();
            bool expNegative = false;
            if (c == '-')
            {
                expNegative = true;
                c = Read();
            }
            else if (c == '+')
            {
                c = Read();
            }

            int exp = 0;
            while (c > 32)
            {
                exp = exp * 10 + (c - '0');
                c = Read();
            }

            v *= Math.Pow(10, expNegative ? -exp : exp);
        }

        return negative ? -v : v;
    }
}