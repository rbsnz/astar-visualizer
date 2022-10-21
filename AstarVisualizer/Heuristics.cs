namespace AstarVisualizer;

/// <summary>
/// Defines heuristic functions between two vertices.
/// </summary>
public static class Heuristics
{
    /// <summary>
    /// Returns zero.
    /// </summary>
    public static readonly HeuristicFunc None = (current, goal) => 0;

    /// <summary>
    /// Returns the Euclidean distance between the current and goal vertices.
    /// </summary>
    public static readonly HeuristicFunc Euclidean = (current, goal) => Maths.Distance(current.Position, goal.Position);
}
