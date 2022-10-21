namespace AstarVisualizer;

/// <summary>
/// Calculates a heuristic value for the current vertex given the goal vertex.
/// </summary>
/// <param name="current">The current vertex.</param>
/// <param name="goal">The goal vertex.</param>
/// <returns>The heuristic value for the current vertex.</returns>
public delegate float HeuristicFunc(Vertex current, Vertex goal);