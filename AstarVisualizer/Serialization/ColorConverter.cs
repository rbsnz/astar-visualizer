using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SFML.Graphics;

namespace AstarVisualizer.Serialization;

public class ColorConverter : JsonConverter<Color>
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

    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Color value must be a string.");
        string color = reader.GetString()
            ?? throw new JsonException("Color value cannot be null.");
        return FromHex(color);
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
