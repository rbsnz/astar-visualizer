namespace AstarVisualizer;

public class AStar
{
    private readonly IEnumerator<AStep> _enumerator;

    private readonly PriorityQueue<Vertex, float> _openSet = new();
    private readonly HashSet<Vertex> _visited = new();
    private readonly Dictionary<Vertex, Vertex> _cameFrom = new();
    private readonly Dictionary<Vertex, float> _gScores = new(), _fScores = new();

    public IReadOnlyList<Vertex> Vertices { get; }
    public IReadOnlyList<Edge> Edges { get; }
    public HeuristicFunc Heuristic { get; }
    public Vertex Start { get; }
    public Vertex Goal { get; }
    public AStep? CurrentStep { get; private set; }

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

    private IEnumerable<AStep> Search()
    {
        // Reset the state of all vertices and edges.
        foreach (var vertex in Vertices)
            vertex.State = AState.Unvisited;
        foreach (var edge in Edges)
            edge.State = AState.Unvisited;

        _gScores[Start] = 0;
        _fScores[Start] = Heuristic(Start, Goal);
        _openSet.Enqueue(Start, 0);

        yield return new BeginSearch();

        while (_openSet.Count > 0)
        {
            // Take the vertex with the lowest fScore.
            Vertex current = _openSet.Dequeue();

            // Skip the vertex if it has already been visited.
            if (!_visited.Add(current))
                continue;

            current.State = AState.Inspecting;
            yield return new VisitVertex(current);

            if (current == Goal)
            {
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

                float gScoreNeighbor = _gScores[current] + Maths.Distance(current.Position, neighbor.Position);
                edge.State = AState.Inspecting;

                yield return new ConsiderVertex(current, neighbor, gScoreNeighbor);
                if (gScoreNeighbor < _gScores.GetValueOrDefault(neighbor, float.PositiveInfinity))
                {
                    // If there was already a path to this vertex, we can eliminate the previous path.
                    // This is not part of the A* algorithm, it is purely for visual representation of discarded paths.
                    if (_cameFrom.ContainsKey(neighbor))
                    {
                        var eliminated = EliminatePath(neighbor);
                        yield return new DiscardPath(current, neighbor, DiscardPathReason.ShorterRouteFound, eliminated);
                    }

                    float fScoreNeighbor = gScoreNeighbor + Heuristic(neighbor, Goal);

                    _cameFrom[neighbor] = current;
                    _gScores[neighbor] = gScoreNeighbor;
                    _fScores[neighbor] = fScoreNeighbor;

                    edge.State = AState.Potential;
                    neighbor.State = AState.Potential;

                    _openSet.Enqueue(neighbor, fScoreNeighbor);

                    if (_cameFrom.ContainsKey(neighbor))
                        yield return new OpenVertex(neighbor, gScoreNeighbor, fScoreNeighbor);
                    else
                        yield return new UpdateVertex(neighbor, gScoreNeighbor, fScoreNeighbor);
                }
                else
                {
                    edge.State = AState.Eliminated;
                    yield return new DiscardPath(current, neighbor, DiscardPathReason.ShorterRouteExists, new[] { current, neighbor });
                }
            }

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
            {
                current.State = AState.Potential;
            }
        }

        yield return new EndSearch();
    }

    public bool Step()
    {
        CurrentStep = _enumerator.MoveNext() ? _enumerator.Current : null;
        return CurrentStep is not null;
    }
}
