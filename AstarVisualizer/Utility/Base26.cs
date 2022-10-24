using System.Text;

namespace AstarVisualizer;

/// <summary>
/// A utility class to convert an integer to a string using Base26 encoding.
/// Used to generate labels for vertices.
/// </summary>
public static class Base26
{
    /// <summary>
    /// Encodes the specified integer to a base-26 string.
    /// </summary>
    /// <exception cref="ArgumentException">If the specified value is negative.</exception>
    public static string Encode(int i)
    {
        if (i < 0)
            throw new ArgumentException("The specified value must not be negative.", nameof(i));

        StringBuilder sb = new();
        while (i >= 0)
        {
            sb.Insert(0, (char)('A' + (i % 26)));
            i = i / 26 - 1;
        }
        return sb.ToString();
    }
}
