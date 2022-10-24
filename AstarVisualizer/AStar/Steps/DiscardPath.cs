namespace AstarVisualizer;

/// <summary>
/// Represents a step where a potential path is discarded.
/// </summary>
public class DiscardPath : AStep
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
    /// Gets the reason that the path was discarded.
    /// </summary>
    public DiscardPathReason Reason { get; }

    /// <summary>
    /// Gets the path that was discarded.
    /// </summary>
    public IReadOnlyList<Vertex> Path { get; }

    /// <summary>
    /// Constructs a new <see cref="DiscardPath"/> step with the specified from/to vertices, reason and path.
    /// </summary>
    /// <param name="from">The vertex currently being visited.</param>
    /// <param name="to">The next vertex being considered.</param>
    /// <param name="reason">The reason that the path was discarded.</param>
    /// <param name="path">The path that was discarded.</param>
    public DiscardPath(Vertex from, Vertex to, DiscardPathReason reason, IEnumerable<Vertex> path)
    {
        From = from;
        To = to;
        Reason = reason;
        Path = path.ToList().AsReadOnly();
    }
}
