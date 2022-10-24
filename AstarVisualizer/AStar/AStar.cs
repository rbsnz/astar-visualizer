using System.Collections;

namespace AstarVisualizer;

public class AStar
{
    private readonly List<Vertex> _vertices;
    private readonly List<Edge> _edges;

    public HeuristicFunc Heuristic { get; }

    public AStar(IEnumerable<Vertex> vertices, HeuristicFunc heuristic)
    {
        _vertices = new(vertices);
        _edges = new(_vertices.SelectMany(x => x.Edges).Distinct());

        Heuristic = heuristic;
    }

    public IEnumerable<AStep> Search(Vertex start, Vertex goal)
    {
        // Reset the state of all vertices and edges.
        foreach (var vertex in _vertices)
            vertex.State = AState.Unvisited;
        foreach (var edge in _edges)
            edge.State = AState.Unvisited;

        PriorityQueue<Vertex, float> openSet = new();
        HashSet<Vertex> visited = new();

        Dictionary<Vertex, Vertex> cameFrom = new();
        Dictionary<Vertex, float> gScore = new();
        Dictionary<Vertex, float> fScore = new();

        // Traverses the path back from the specified vertex and eliminates vertices with no potential paths.
        LinkedList<Vertex> eliminatePath(Vertex current)
        {
            LinkedList<Vertex> eliminated = new();

            // Traverse back where we came from.
            while (cameFrom.ContainsKey(current))
            {
                var previous = cameFrom[current];
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

        gScore[start] = 0;
        fScore[start] = Heuristic(start, goal);
        openSet.Enqueue(start, 0);

        yield return new BeginSearch();

        while (openSet.Count > 0)
        {
            // Take the vertex with the lowest fScore.
            Vertex current = openSet.Dequeue();

            // Skip the vertex if it has already been visited.
            if (!visited.Add(current))
                continue;

            current.State = AState.Inspecting;
            yield return new VisitVertex(current);

            if (current == goal)
            {
                current.State = AState.Success;
                LinkedList<Vertex> path = new();
                path.AddFirst(current);
                while (cameFrom.ContainsKey(current))
                {
                    current.GetEdge(cameFrom[current]).State = AState.Success;
                    current = cameFrom[current];
                    current.State = AState.Success;
                    path.AddFirst(current);
                }
                yield return new EndSearch(path);
                yield break;
            }

            foreach (Vertex neighbor in current.Connections)
            {
                // Skip the vertex if it has already been visited.
                if (visited.Contains(neighbor)) continue;

                cameFrom.TryGetValue(neighbor, out Vertex? previouslyCameFrom);

                Edge edge = current.GetEdge(neighbor);
                AState previousState = edge.State;

                float gScoreNeighbor = gScore[current] + Maths.Distance(current.Position, neighbor.Position);
                edge.State = AState.Inspecting;

                yield return new ConsiderVertex(current, neighbor, gScoreNeighbor);
                if (gScoreNeighbor < gScore.GetValueOrDefault(neighbor, float.PositiveInfinity))
                {
                    // If there was already a path to this vertex, we can eliminate the previous path.
                    // This is not part of the A* algorithm, it is purely for visual representation of discarded paths.
                    if (previouslyCameFrom is not null)
                    {
                        var eliminated = eliminatePath(neighbor);
                        yield return new DiscardPath(current, neighbor, DiscardPathReason.ShorterRouteFound, eliminated);
                    }

                    float fScoreNeighbor = gScoreNeighbor + Heuristic(neighbor, goal);

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = gScoreNeighbor;
                    fScore[neighbor] = fScoreNeighbor;

                    edge.State = AState.Potential;
                    neighbor.State = AState.Potential;

                    openSet.Enqueue(neighbor, fScoreNeighbor);

                    if (previouslyCameFrom is null)
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

            if (current != start &&
                current.Edges.Count(x => x.State != AState.Eliminated) <= 1)
            {
                var eliminated = eliminatePath(current);
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
}
