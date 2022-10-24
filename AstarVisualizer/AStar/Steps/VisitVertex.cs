namespace AstarVisualizer;

/// <summary>
/// Represents a step where a vertex is visited.
/// </summary>
public class VisitVertex : AStep
{
    /// <summary>
    /// Gets the vertex being visited.
    /// </summary>
    public Vertex Vertex { get; }

    /// <summary>
    /// Constructs a new <see cref="VisitVertex"/> step with the specified vertex.
    /// </summary>
    /// <param name="vertex">The vertex being visited.</param>
    public VisitVertex(Vertex vertex) => Vertex = vertex;
}
