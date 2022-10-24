namespace AstarVisualizer;

/// <summary>
/// Represents the end of the search.
/// </summary>
public class EndSearch : AStep
{
    /// <summary>
    /// Gets if a path to the goal was found.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the path to the goal, if it was found.
    /// </summary>
    public LinkedList<Vertex> Path { get; }

    /// <summary>
    /// Constructs a new <see cref="EndSearch"/> step with the specified path.
    /// </summary>
    /// <param name="path">The path to the goal, or <see langword="null"/> if the search was unsuccessful.</param>
    public EndSearch(LinkedList<Vertex>? path = null)
    {
        Success = path is not null;
        Path = path ?? new();
    }
}
