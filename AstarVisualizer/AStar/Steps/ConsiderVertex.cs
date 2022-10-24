namespace AstarVisualizer;

/// <summary>
/// Represents a step where a path to a vertex is being considered.
/// </summary>
public class ConsiderVertex : AStep
{
    /// <summary>
    /// Gets the vertex currently being visited.
    /// </summary>
    public Vertex From { get; }

    /// <summary>
    /// Gets the next vertex being considered.
    /// </summary>
    public Vertex To { get; }

    /// <summary>
    /// Gets the gScore of the next vertex.
    /// </summary>
    public float GScore { get; }

    /// <summary>
    /// Constructs a new <see cref="ConsiderVertex"/> step with the specified vertices and score.
    /// </summary>
    /// <param name="from">The vertex currently being visited.</param>
    /// <param name="to">The next vertex being considered.</param>
    /// <param name="gScore">The gScore of the next vertex.</param>
    public ConsiderVertex(Vertex from, Vertex to, float gScore)
    {
        From = from;
        To = to;
        GScore = gScore;
    }
}
