namespace AstarVisualizer;

/// <summary>
/// Represents a reason that a path is discarded.
/// </summary>
public enum DiscardPathReason
{
    /// <summary>
    /// The path leads to a dead end.
    /// </summary>
    DeadEnd,
    /// <summary>
    /// The path from the current vertex to the next vertex is shorter
    /// than the existing path, so the previous path leading to it is discarded.
    /// </summary>
    ShorterRouteFound,
    /// <summary>
    /// A shorter path to the next vertex exists, so it is not considered from the current vertex.
    /// </summary>
    ShorterRouteExists
}
