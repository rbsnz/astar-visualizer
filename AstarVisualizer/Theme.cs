using System.Text.Json;

using SFML.Graphics;

using AstarVisualizer.Serialization;

namespace AstarVisualizer;

/// <summary>
/// Provides a color theme for the visualizer.
/// </summary>
public class Theme
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new ColorConverter(),
            new FontConverter()
        }
    };

    /// <summary>
    /// Gets or sets the current theme.
    /// </summary>
    public static Theme Current { get; set; } = Load(@"res\theme\default.json");

    public Font Font { get; set; } = null!;

    public Color Background { get; set; }
    public Color VertexFill { get; set; }
    public Color EdgeFill { get; set; }
    public Color PotentialEdgeFill { get; set; }
    public Color VertexHover { get; set; }
    public Color VertexDragging { get; set; }
    public Color VertexDraggingInvalid { get; set; }

    public Color VertexOutline { get; set; }
    public Color VertexOutlineOpen { get; set; }
    public Color VertexOutlineClosed { get; set; }

    public Color VertexUnvisited { get; set; }
    public Color VertexInspecting { get; set; }
    public Color VertexPotential { get; set; }
    public Color VertexEliminated { get; set; }
    public Color VertexSuccess { get; set; }

    public Color EdgeStateInvalid { get; set; }
    public Color EdgeStateUnvisited { get; set; }
    public Color EdgeStatePotential { get; set; }
    public Color EdgeStateInspecting { get; set; }
    public Color EdgeStateEliminated { get; set; }
    public Color EdgeStateSuccess { get; set; }

    /// <summary>
    /// Loads a theme from the specified JSON file path.
    /// </summary>
    /// <param name="path">The path to the JSON theme file.</param>
    /// <returns>The resulting theme.</returns>
    /// <exception cref="JsonException">If the theme was unable to be deserialized.</exception>
    public static Theme Load(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Theme>(json, SerializerOptions)
            ?? throw new JsonException("Failed to deserialize theme.");
    }
}
