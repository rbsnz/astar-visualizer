using System.Text;

namespace AstarVisualizer;

public static class Base26
{
    private static readonly char[] Charset = Enumerable.Range(0, 26)
        .Select(i => (char)('A' + i))
        .ToArray();

    public static string Encode(int i)
    {
        if (i < 0)
            throw new ArgumentException("The specified value must not be negative.", nameof(i));

        StringBuilder sb = new();
        while (i >= 0)
        {
            sb.Insert(0, Charset[i % 26]);
            i = i / 26 - 1;
        }
        return sb.ToString();
    }
}
