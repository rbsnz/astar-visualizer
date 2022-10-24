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
        foreach (var vertex in _vertices)
            vertex.State = AState.Unvisited;
        foreach (var edge in _edges)
            edge.State = AState.Unvisited;

        HashSet<Vertex> openSet = new() { start };
        HashSet<Vertex> visited = new();

        Dictionary<Vertex, Vertex> cameFrom = new();
        Dictionary<Vertex, float> gScore = new();
        Dictionary<Vertex, float> fScore = new();

        // Traverses the path back from the specified vertex and eliminates dead edges.
        LinkedList<Vertex> eliminatePath(Vertex current)
        {
            LinkedList<Vertex> eliminated = new();

            // Traverse back where we came from.
            while (cameFrom.ContainsKey(current))
            {
                var previous = cameFrom[current];
                eliminated.AddFirst(current);
                current.State = AState.Eliminated;
                current.GetEdge(previous).State = AState.Eliminated;
                current = previous;

                // Vertices are only eliminated if they have <= 1 open paths.
                if (current.Edges.Count(x => x.State != AState.Eliminated) > 1)
                    break;
            }
            eliminated.AddFirst(current);
            return eliminated;
        }

        gScore[start] = 0;
        fScore[start] = Heuristic(start, goal);

        yield return new BeginSearch();

        while (openSet.Count > 0)
        {
            Vertex? current = openSet.MinBy(x => fScore[x]);
            if (current is null) break;
            openSet.Remove(current);
            visited.Add(current);

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

            cameFrom.TryGetValue(current, out Vertex? cameFromCurrent);
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
                        previouslyCameFrom.GetEdge(neighbor).State = AState.Eliminated;
                        var eliminated = eliminatePath(neighbor);
                        neighbor.State = AState.Potential;
                        yield return new DiscardPath(current, neighbor, DiscardPathReason.ShorterRouteFound, eliminated);
                    }

                    float fScoreNeighbor = gScoreNeighbor + Heuristic(neighbor, goal);

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = gScoreNeighbor;
                    fScore[neighbor] = fScoreNeighbor;

                    neighbor.State = AState.Potential;
                    edge.State = AState.Potential;

                    if (openSet.Add(neighbor))
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
