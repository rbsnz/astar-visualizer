namespace AstarVisualizer;

/// <summary>
/// Represents the state of a vertex or edge during an A* search.
/// </summary>
public enum AState
{
    None,
    Unvisited,
    Inspecting,
    Potential,
    Eliminated,
    Success
}
