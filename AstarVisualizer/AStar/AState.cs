namespace AstarVisualizer;

/// <summary>
/// Represents the state of a vertex or edge.
/// </summary>
public enum AState
{
    None,
    Invalid,
    Unvisited,
    Inspecting,
    Potential,
    Eliminated,
    Success
}
