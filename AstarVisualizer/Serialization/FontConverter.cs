using System.Text.Json;
using System.Text.Json.Serialization;

using SFML.Graphics;

namespace AstarVisualizer.Serialization;

/// <summary>
/// Allows deserializing from a string filepath to a <see cref="Font"/>.
/// </summary>
public class FontConverter : JsonConverter<Font>
{
    public override Font? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Font must be a string.");
        string path = reader.GetString()
            ?? throw new JsonException("No value specified for font.");

        return new Font(path);
    }

    public override void Write(Utf8JsonWriter writer, Font value, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}
