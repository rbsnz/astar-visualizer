namespace AstarVisualizer;

/// <summary>
/// Represents a step where a shorter path to a vertex is found, and its score is updated.
/// </summary>
public class UpdateVertex : AStep
{
    /// <summary>
    /// Gets the vertex that was updated.
    /// </summary>
    public Vertex Vertex { get; }

    /// <summary>
    /// Gets the gScore of the vertex.
    /// </summary>
    public float GScore { get; }

    /// <summary>
    /// Gets the fScore of the vertex.
    /// </summary>
    public float FScore { get; }

    /// <summary>
    /// Constructs a new <see cref="UpdateVertex"/> step with the specified vertex and score.
    /// </summary>
    /// <param name="vertex">The vertex that was updated.</param>
    /// <param name="gScore">The gScore of the vertex.</param>
    /// <param name="fScore">The fScore of the vertex.</param>
    public UpdateVertex(Vertex vertex, float gScore, float fScore)
    {
        Vertex = vertex;
        GScore = gScore;
        FScore = fScore;
    }
}
