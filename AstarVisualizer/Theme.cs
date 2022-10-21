using System.Text.Json;

using SFML.Graphics;

using AstarVisualizer.Serialization;

namespace AstarVisualizer;

public class Theme
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new ColorConverter() }
    };

    public static Theme Current { get; set; } = Load(@"theme\default.json");

    public Color Background { get; set; }
    public Color VertexFill { get; set; }
    public Color EdgeFill { get; set; }
    public Color PotentialEdgeFill { get; set; }
    public Color VertexHover { get; set; }
    public Color VertexDragging { get; set; }
    public Color VertexDraggingInvalid { get; set; }

    public static Theme Load(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Theme>(json, SerializerOptions)
            ?? throw new JsonException("Failed to deserialize theme.");
    }
}
