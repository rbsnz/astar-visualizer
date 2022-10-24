namespace AstarVisualizer;

/// <summary>
/// Performs an A* search.
/// </summary>
public class AStar
{
    private readonly IEnumerator<AStep> _enumerator;

    private readonly PriorityQueue<Vertex, float> _openSet = new();
    private readonly HashSet<Vertex> _visited = new();
    private readonly Dictionary<Vertex, Vertex> _cameFrom = new();
    private readonly Dictionary<Vertex, float> _gScores = new(), _fScores = new();

    /// <summary>
    /// Gets all vertices in the search space.
    /// </summary>
    public IReadOnlyList<Vertex> Vertices { get; }
    /// <summary>
    /// Gets all edges in the search space.
    /// </summary>
    public IReadOnlyList<Edge> Edges { get; }
    /// <summary>
    /// Gets the heuristic function being used by the search.
    /// </summary>
    public HeuristicFunc Heuristic { get; }
    /// <summary>
    /// Gets the start vertex.
    /// </summary>
    public Vertex Start { get; }
    /// <summary>
    /// Gets the goal vertex.
    /// </summary>
    public Vertex Goal { get; }
    /// <summary>
    /// Gets the current step of the A* search.
    /// </summary>
    public AStep? CurrentStep { get; private set; }

    /// <summary>
    /// Constructs a new <see cref="AStar"/> search with the specified vertices, heuristic, start and goal vertices.
    /// </summary>
    /// <param name="vertices">The vertices representing the search space.</param>
    /// <param name="heuristic">The heuristic function to use.</param>
    /// <param name="start">The start vertex.</param>
    /// <param name="goal">The goal vertex.</param>
    public AStar(IEnumerable<Vertex> vertices, HeuristicFunc heuristic, Vertex start, Vertex goal)
    {
        Vertices = vertices.ToArray();
        Edges = Vertices.SelectMany(x => x.Edges).Distinct().ToArray();
        Heuristic = heuristic;
        Start = start;
        Goal = goal;

        _enumerator = Search().GetEnumerator();
    }

    /// <summary>
    /// Traverses the path back from the specified vertex and eliminates vertices with no potential paths.
    /// </summary>
    /// <param name="current">The vertex to traverse back from.</param>
    /// <returns>The path that was eliminated.</returns>
    private LinkedList<Vertex> EliminatePath(Vertex current)
    {
        LinkedList<Vertex> eliminated = new();

        // Traverse back where we came from.
        while (_cameFrom.ContainsKey(current))
        {
            var previous = _cameFrom[current];
            eliminated.AddFirst(current);
            // Mark the vertex and the edge to the previous vertex as eliminated.
            current.State = AState.Eliminated;
            current.GetEdge(previous).State = AState.Eliminated;
            // Move to the previous vertex.
            current = previous;

            // Vertices are only eliminated if they have <= 1 open paths - the path to the next vertex that was eliminated.
            if (current.Edges.Count(x => x.State != AState.Eliminated) > 1)
                break;
        }

        // Add the root vertex to the eliminated path.
        eliminated.AddFirst(current);

        // Mark the root vertex as eliminated if all its paths have been eliminated.
        if (current.Edges.All(x => x.State == AState.Eliminated))
            current.State = AState.Eliminated;

        // Return the path of vertices that were eliminated.
        return eliminated;
    }

    /// <summary>
    /// Provides an enumerator to perform the A* search step by step.
    /// </summary>
    private IEnumerable<AStep> Search()
    {
        // Reset the state of all vertices and edges.
        foreach (var vertex in Vertices)
            vertex.State = AState.Unvisited;
        foreach (var edge in Edges)
            edge.State = AState.Unvisited;

        // Mark the beginning of the search.
        yield return new BeginSearch();

        // Initialize the gScore and fScore of the start vertex.
        _gScores[Start] = 0;
        _fScores[Start] = Heuristic(Start, Goal);

        // Add the start vertex to the open set.
        _openSet.Enqueue(Start, 0);
        Start.State = AState.Potential;
        yield return new OpenVertex(Start, 0, _fScores[Start]);

        while (_openSet.Count > 0)
        {
            // Dequeue the vertex with the lowest fScore from the open set.
            Vertex current = _openSet.Dequeue();

            // Skip the vertex if it has already been visited.
            if (!_visited.Add(current))
                continue;

            // Signal that we are visiting this vertex.
            current.State = AState.Inspecting;
            yield return new VisitVertex(current);

            // If the current vertex is the goal, we have reached the destination.
            if (current == Goal)
            {
                // Reconstruct the path by traversing back to the start vertex.
                current.State = AState.Success;
                LinkedList<Vertex> path = new();
                path.AddFirst(current);
                while (_cameFrom.ContainsKey(current))
                {
                    current.GetEdge(_cameFrom[current]).State = AState.Success;
                    current = _cameFrom[current];
                    current.State = AState.Success;
                    path.AddFirst(current);
                }
                // Signal the successful end of the search and provide the resulting path.
                yield return new EndSearch(path);
                yield break;
            }

            // Consider each neighbor of the current vertex.
            foreach (Vertex neighbor in current.Connections)
            {
                // Skip the vertex if it has already been visited.
                if (_visited.Contains(neighbor)) continue;

                Edge edge = current.GetEdge(neighbor);
                AState previousState = edge.State;

                // Calculate the gScore for the neighbor.
                float gScoreNeighbor = _gScores[current] + Maths.Distance(current.Position, neighbor.Position);
                edge.State = AState.Inspecting;

                // Signal that we are considering this vertex.
                yield return new ConsiderVertex(current, neighbor, gScoreNeighbor);
                
                // If the gScore is less than any previously existing gScore, we add this vertex to the open set.
                if (gScoreNeighbor < _gScores.GetValueOrDefault(neighbor, float.PositiveInfinity))
                {
                    // If there was already a path to this vertex, we can eliminate the previous path.
                    // This is not part of the A* algorithm, it is purely for visual representation of discarded paths.
                    bool previousPathExists = _cameFrom.ContainsKey(neighbor);
                    if (_cameFrom.ContainsKey(neighbor))
                    {
                        var eliminated = EliminatePath(neighbor);
                        yield return new DiscardPath(current, neighbor, DiscardPathReason.ShorterRouteFound, eliminated);
                    }

                    // Calculate the fScore for this vertex.
                    float fScoreNeighbor = gScoreNeighbor + Heuristic(neighbor, Goal);

                    _cameFrom[neighbor] = current;
                    _gScores[neighbor] = gScoreNeighbor;
                    _fScores[neighbor] = fScoreNeighbor;

                    edge.State = AState.Potential;
                    neighbor.State = AState.Potential;

                    // Add this vertex to the open set.
                    _openSet.Enqueue(neighbor, fScoreNeighbor);

                    // Signal that we have added this vertex to the open set or updated its fScore if a shorter path was found.
                    if (!previousPathExists)
                        yield return new OpenVertex(neighbor, gScoreNeighbor, fScoreNeighbor);
                    else
                        yield return new UpdateVertex(neighbor, gScoreNeighbor, fScoreNeighbor);
                }
                else
                {
                    // If the calculated gScore is not less than the existing gScore, a shorter path already exists.
                    edge.State = AState.Eliminated;
                    // Signal that we have discarded the suboptimal path.
                    yield return new DiscardPath(current, neighbor, DiscardPathReason.ShorterRouteExists, new[] { current, neighbor });
                }
            }

            // If the current vertex is not the start vertex and there are <= 1 potential paths, this is a dead end.
            if (current != Start &&
                current.Edges.Count(x => x.State != AState.Eliminated) <= 1)
            {
                var eliminated = EliminatePath(current);
                if (eliminated.Count > 0)
                {
                    yield return new DiscardPath(current, current, DiscardPathReason.DeadEnd, eliminated);
                }
            }
            else
            // Otherwise the vertex can be marked as part of a potential path.
            {
                current.State = AState.Potential;
            }
        }

        // Mark the end of an unsuccessful search.
        yield return new EndSearch();
    }

    /// <summary>
    /// Performs a step in the A* search.
    /// </summary>
    /// <returns><see langword="true"/> if a step was taken, or <see langword="false"/> if the search has completed.</returns>
    public bool Step()
    {
        CurrentStep = _enumerator.MoveNext() ? _enumerator.Current : null;
        return CurrentStep is not null;
    }
}
