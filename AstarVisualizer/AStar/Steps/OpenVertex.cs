namespace AstarVisualizer;

/// <summary>
/// Represents a step where a vertex is added to the open set.
/// </summary>
public class OpenVertex : AStep
{
    /// <summary>
    /// Gets the vertex that was added to the open set.
    /// </summary>
    public Vertex Vertex { get; }

    /// <summary>
    /// Get the gScore of the vertex.
    /// </summary>
    public float GScore { get; }

    /// <summary>
    /// Gets the fScore of the vertex.
    /// </summary>
    public float FScore { get; }

    /// <summary>
    /// Constructs a new <see cref="OpenVertex"/> step with the specified vertex and score.
    /// </summary>
    /// <param name="vertex">The vertex that was added to the open set.</param>
    /// <param name="gScore">The gScore of the vertex.</param>
    /// <param name="fScore">The fScore of the vertex.</param>
    public OpenVertex(Vertex vertex, float gScore, float fScore)
    {
        Vertex = vertex;
        GScore = gScore;
        FScore = fScore;
    }
}
