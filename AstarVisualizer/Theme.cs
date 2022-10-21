using System.Globalization;

using SFML.Graphics;

namespace AstarVisualizer;

internal static class Theme
{
    static Color FromHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            throw new ArgumentException("Value cannot be empty.", nameof(hex));

        if (hex.StartsWith('#'))
            hex = hex[1..];

        if ((hex.Length != 6 && hex.Length != 8) ||
            !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value))
        {
            throw new ArgumentException("Not a valid hex color.", nameof(hex));
        }

        if (hex.Length == 6)
            value = value << 8 | 0xFF;

        return new Color(value);
    }

    public static readonly Color Background = FromHex("1B2430");
    public static readonly Color VertexFill = FromHex("D6D5A8");
    public static readonly Color EdgeFill = FromHex("29C7AC");
    public static readonly Color PotentialEdgeFill = FromHex("29C7AC");

    public static readonly Color VertexHover = FromHex("FFFFFF55");
    public static readonly Color VertexDragging = FromHex("FFFFFF88");
    public static readonly Color VertexDraggingInvalid = FromHex("FF880055");
}
