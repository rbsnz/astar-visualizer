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

        yield return "Begin";

        while (openSet.Count > 0)
        {
            Vertex? current = openSet.MinBy(x => fScore[x]);
            if (current is null) break;
            openSet.Remove(current);

            current.State = AState.Inspecting;
            yield return $"Inspecting vertex: {current.Label}";

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
                yield return "Found goal.";
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

                float tentativeGscore = gScore.GetValueOrDefault(current, float.PositiveInfinity)
                    + Maths.Distance(current.Position, neighbor.Position);
                edge.State = AState.Inspecting;
                yield return $"Tentative GScore for vertex {neighbor.Label} = {tentativeGscore:N0}";
                if (tentativeGscore < gScore.GetValueOrDefault(neighbor, float.PositiveInfinity))
                {
                    // If there was already a path to this vertex, we can eliminate the previous path.
                    // This is not part of the A* algorithm, it is purely for visual representation of discarded paths.
                    if (previouslyCameFrom is not null)
                    {
                        previouslyCameFrom.GetEdge(neighbor).State = AState.Eliminated;
                        var eliminated = eliminatePath(neighbor);
                        neighbor.State = AState.Potential;
                        yield return $"Found shorter route to vertex {neighbor.Label}, eliminated path {string.Join("->", eliminated.Select(x => x.Label))}";
                    }

                    float currentFscore = tentativeGscore + Heuristic(neighbor, goal);

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGscore;
                    fScore[neighbor] = currentFscore;

                    neighbor.State = AState.Potential;
                    edge.State = AState.Potential;

                    if (openSet.Add(neighbor))
                    {
                        yield return $"Added vertex {neighbor.Label} to open set, fScore = {currentFscore:N0}";
                    }
                }
                else
                {
                    edge.State = AState.Eliminated;
                    yield return $"Vertex {neighbor.Label} has a shorter route";
                }
            }

            if (current != start &&
                current.Edges.Count(x => x.State != AState.Eliminated) <= 1)
            {
                var eliminated = eliminatePath(current);
                if (eliminated.Count > 0)
                    yield return $"Eliminated path: {string.Join("->", eliminated.Select(x => x.Label))}";
            }
            else
            {
                current.State = AState.Potential;
            }
        }

        yield return "No solution found";
    }
}
