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

    public IEnumerable Search(Vertex start, Vertex goal)
    {
        foreach (var vertex in _vertices)
            vertex.State = AState.Unvisited;
        foreach (var edge in _edges)
            edge.State = AState.Unvisited;

        HashSet<Vertex> openSet = new() { start };

        Dictionary<Vertex, Vertex> cameFrom = new();
        Dictionary<Vertex, float> gScore = new();
        Dictionary<Vertex, float> fScore = new();

        // Traverses the path back from the specified vertex and eliminates dead edges.
        List<Vertex> eliminatePath(Vertex current)
        {
            List<Vertex> eliminated = new();
            // Traverse back where we came from.

            while (cameFrom.ContainsKey(current))
            {
                var previous = cameFrom[current];
                eliminated.Add(current);
                current.State = AState.Eliminated;
                current.GetEdge(previous).State = AState.Eliminated;
                current = previous;

                // Vertices are only eliminated if they have <= 1 open paths.
                if (current.Edges.Count(x => x.State != AState.Eliminated) > 1)
                    break;
            }
            eliminated.Add(current);
            eliminated.Reverse();
            return eliminated;
        }

        gScore[start] = 0;
        fScore[start] = Heuristic(start, goal);

        // yield return BeginSearch();
        yield return "Beginning search";

        while (openSet.Count > 0)
        {
            Vertex? current = openSet.MinBy(x => fScore[x]);
            if (current is null) break;
            openSet.Remove(current);

            current.State = AState.Inspecting;
            // yield return PopVertex(current);
            yield return $"At {current}";

            if (current == goal)
            {
                current.State = AState.Success;
                List<Vertex> path = new() { current };
                while (cameFrom.ContainsKey(current))
                {
                    current.GetEdge(cameFrom[current]).State = AState.Success;
                    current = cameFrom[current];
                    current.State = AState.Success;
                    path.Insert(0, current);
                }
                // yield return EndSearch(path);
                yield return "Found path to goal";
                yield break;
            }

            cameFrom.TryGetValue(current, out Vertex? cameFromCurrent);
            foreach (Vertex neighbor in current.Connections)
            {
                // Skip the neighbor if it's where we currently came from.
                if (neighbor == cameFromCurrent) continue;

                // Skip the edge if it has been eliminated.
                if (current.GetEdge(neighbor).State == AState.Eliminated) continue;

                cameFrom.TryGetValue(neighbor, out Vertex? previouslyCameFrom);

                Edge edge = current.GetEdge(neighbor);
                AState previousState = edge.State;

                float gScoreNeighbor = gScore[current] + Maths.Distance(current.Position, neighbor.Position);
                edge.State = AState.Inspecting;

                // yield return CalculateGScore(current, neighbor, gScoreNeighbor);
                yield return $"Calculated gScore for {neighbor} = {gScoreNeighbor:N0}";
                if (gScoreNeighbor < gScore.GetValueOrDefault(neighbor, float.PositiveInfinity))
                {
                    // If there was already a path to this vertex, we can eliminate the previous path.
                    // This is not part of the A* algorithm, it is purely for visual representation of discarded paths.
                    if (previouslyCameFrom is not null)
                    {
                        previouslyCameFrom.GetEdge(neighbor).State = AState.Eliminated;
                        var eliminated = eliminatePath(neighbor);
                        neighbor.State = AState.Potential;
                        // yield return DiscardPath(eliminated, DiscardPathReason.ShorterRouteFound);
                        yield return $"Discovered shorter route to {neighbor}, eliminated path {string.Join("->", eliminated)}";
                    }

                    float fScoreNeighbor = gScoreNeighbor + Heuristic(neighbor, goal);

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = gScoreNeighbor;
                    fScore[neighbor] = fScoreNeighbor;

                    neighbor.State = AState.Potential;
                    edge.State = AState.Potential;

                    if (openSet.Add(neighbor))
                    {
                        // yield return PushVertex(neighbor, fScoreNeighbor);
                        yield return $"Added {neighbor} to open set, fScore = {fScoreNeighbor:N0}";
                    }
                    else
                    {
                        // yield return UpdateVertex(neighbor, gScoreNeighbor, fScoreNeighbor);
                        yield return $"Updated fScore for {neighbor} = {fScoreNeighbor:N0}";
                    }
                }
                else
                {
                    edge.State = AState.Eliminated;
                    // yield return DiscardPath(new[] { current, neighbor }, DiscardPathReason.ShorterRouteExists);
                    yield return $"{neighbor} has a shorter route, discarding path {current}->{neighbor}";
                }
            }

            if (current != start &&
                current.Edges.Count(x => x.State != AState.Eliminated) <= 1)
            {
                var eliminated = eliminatePath(current);
                if (eliminated.Count > 0)
                {
                    // yield return DiscardPath(eliminated, DiscardPathReason.DeadEnd);
                    yield return $"Dead end, eliminated path {string.Join("->", eliminated)}";
                }
            }
            else
            {
                current.State = AState.Potential;
            }
        }

        // yield return EndSearch();
        yield return "No solution found";
    }
}
