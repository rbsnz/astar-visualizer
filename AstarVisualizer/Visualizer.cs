using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace AstarVisualizer;

/// <summary>
/// Manages all logic for the visualizer.
/// </summary>
public sealed class Visualizer
{
    /// <summary>
    /// The default radius for new vertices.
    /// </summary>
    const int VertexRadius = 15;

    private readonly VideoMode _videoMode;
    private readonly RenderWindow _window;

    private readonly Clock _clock = new();

    private readonly List<Vertex> _vertices = new();
    private readonly List<Edge> _edges = new();
    private readonly List<Edge> _potentialEdges = new();

    private readonly Text _creditText = new()
    {
        Font = Theme.Current.Font,
        CharacterSize = 16,
        DisplayedString = "A* Visualizer by rob",
        FillColor = new Color(255, 255, 255, 100)
    };

    private readonly CircleShape _hoverCircle = new()
    {
        FillColor = new Color(255, 255, 255, 55),
        Radius = VertexRadius * 2,
        Origin = new Vector2f(VertexRadius * 2, VertexRadius * 2)
    };

    private readonly CircleShape _startCircle = new()
    {
        FillColor = new Color(0, 200, 200, 100),
        Radius = VertexRadius * 2,
        Origin = new Vector2f(VertexRadius * 2, VertexRadius * 2)
    };

    private readonly CircleShape _goalCircle = new()
    {
        FillColor = new Color(0, 255, 0, 100),
        Radius = VertexRadius * 2,
        Origin = new Vector2f(VertexRadius * 2, VertexRadius * 2)
    };

    private Vertex? _hoverVertex;
    private bool _canPlace;

    private bool _isDragging;
    private Vertex? _draggingVertex;
    private Vector2f _draggingFrom;
    private Vector2f _dragOffset;

    private Edge? _hoverEdge;
    private Vector2f _edgeHoverPoint;

    private Vertex? _start, _goal;

    private IEnumerator? _astarEnumerator;

    public Visualizer()
    {
        _videoMode = new VideoMode(1280, 720);
        _window = new RenderWindow(_videoMode, "A* Visualizer",
            Styles.Titlebar | Styles.Close, new ContextSettings() { AntialiasingLevel = 8 });
        _window.SetVerticalSyncEnabled(true);
        _window.Closed += (s, e) => _window.Close();

        _window.MouseMoved += HandleMouseMoved;
        _window.MouseButtonPressed += HandleMouseButtonPressed;
        _window.MouseButtonReleased += HandleMouseButtonReleased;
        _window.KeyPressed += HandleKeyPressed;

        var textBounds = _creditText.GetLocalBounds();
        _creditText.Origin = new Vector2f(0, textBounds.Height);
        _creditText.Position = new Vector2f(10, _videoMode.Height - 10);
    }

    private void HandleKeyPressed(object? sender, KeyEventArgs e)
    {
        if (e.Code == Keyboard.Key.Space)
        {
            if (_astarEnumerator is not null)
            {
                if (_astarEnumerator.MoveNext())
                {
                    Debug.WriteLine(_astarEnumerator.Current);
                }
                else
                {
                    _start = null;
                    _goal = null;
                }
            }
        }
    }

    #region A* Search
    private IEnumerable AStar(Vertex start, Vertex goal, HeuristicFunc h)
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
        fScore[start] = h(start, goal);

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
                yield return $"Calculate tentative GScore for vertex {neighbor.Label}: {tentativeGscore:N0}";
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

                    float currentFscore = tentativeGscore + h(neighbor, goal);

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGscore;
                    fScore[neighbor] = currentFscore;

                    neighbor.State = AState.Potential;
                    edge.State = AState.Potential;

                    if (openSet.Add(neighbor))
                    {
                        yield return $"Added vertex {neighbor.Label} to the open set (fScore = {currentFscore:N0})";
                    }
                }
                else
                {
                    edge.State = AState.Eliminated;
                    yield return $"Vertex {neighbor.Label} has a shorter path leading to it";
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
    #endregion

    #region Logic
    /// <summary>
    /// Gets the vertex at the specified point.
    /// </summary>
    /// <param name="point">The point at which to test for a collision.</param>
    /// <returns>The resulting vertex, or <see langword="null"/> if it does not exist.</returns>
    private Vertex? GetVertexAtPoint(Vector2f point)
    {
        foreach (Vertex vertex in _vertices)
        {
            if (Maths.Distance(point, vertex.Position) <= vertex.Radius)
                return vertex;
        }

        return null;
    }

    /// <summary>
    /// Calculates potential edges between nodes that do not intersect with any existing edges.
    /// </summary>
    private void CalculatePotentialEdges()
    {
        _potentialEdges.Clear();

        for (int i = 0; i < _vertices.Count - 1; i++)
        {
            for (int j = i + 1; j < _vertices.Count; j++)
            {
                var vertexA = _vertices[i];
                var vertexB = _vertices[j];
                if (vertexA.IsConnectedTo(vertexB))
                    continue;

                bool valid = true;
                foreach (var edge in _edges)
                {
                    if (edge.IsConnectedTo(vertexA) || edge.IsConnectedTo(vertexB)) continue;

                    if (Line.Intersects(edge.Line, new Line(vertexA.Position, vertexB.Position), out _))
                    {
                        valid = false;
                        break;
                    }
                }

                if (!valid)
                    continue;
                
                _potentialEdges.Add(new Edge(vertexA, vertexB) {
                    Color = Theme.Current.PotentialEdgeFill,
                    Weight = 4,
                    IsPotentialEdge = true,
                    IsVisible = false
                });
            }
        }
    }

    /// <summary>
    /// Snaps the specified point to the nearest point along one of the specified edges,
    /// where the distance from the point to the line does not exceed maxDistance.
    /// </summary>
    /// <param name="edges">The edges to search.</param>
    /// <param name="point">The point from which to find the nearest edge.</param>
    /// <param name="maxDistance">The maximum distance from the point t</param>
    /// <param name="edge"></param>
    /// <param name="intersection"></param>
    /// <returns></returns>
    private bool SnapPointToNearestEdge(IEnumerable<Edge> edges, Vector2f point, float maxDistance,
        [NotNullWhen(true)] out Edge? edge, out Vector2f intersection)
    {
        edge = null;
        intersection = default;
        float minDistance = float.MaxValue;

        foreach (var currentEdge in edges)
        {
            // Calculate a line perpendicular to the edge angle, centered around the specified point.
            Line edgeLine = currentEdge.Line;
            float rightAngle = edgeLine.Angle + (MathF.PI / 2);
            Line pointLine = new(
                point.X + MathF.Cos(rightAngle) * maxDistance,
                point.Y + MathF.Sin(rightAngle) * maxDistance,
                point.X - MathF.Cos(rightAngle) * maxDistance,
                point.Y - MathF.Sin(rightAngle) * maxDistance
            );

            // Check if the lines intersect.
            if (Line.Intersects(edgeLine, pointLine, out Vector2f p))
            {
                // Update the current edge if this is the nearest to the specified point.
                float distance = Maths.Distance(point, p);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    edge = currentEdge;
                    intersection = p;
                }
            }
        }

        return edge is not null;
    }

    /// <summary>
    /// Finds a vertex nearest to the specified point where an edge
    /// between the vertex and point would not collide with any existing edges.
    /// </summary>
    /// <returns>The resulting vertex if one was found, otherwise <see langword="null"/>.</returns>
    private Vertex? FindNearestLinkableVertex(Vector2f point)
    {
        float minDistance = float.MaxValue;
        Vertex? minVertex = null;

        // Iterate through each existing vertex.
        foreach (var vertex in _vertices)
        {
            bool valid = true;

            // Create a line between the point and vertex.
            Line line = new(vertex.Position, point);

            // Iterate through each edge.
            foreach (var edge in _edges)
            {
                // Skip edges connected to this vertex.
                if (edge.A == vertex || edge.B == vertex)
                    continue;

                if (Line.Intersects(line, edge.Line, out _))
                {
                    valid = false;
                    continue;
                }
            }

            if (valid)
            {
                float distance = Maths.Distance(vertex.Position, point);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    minVertex = vertex;
                }
            }
        }

        return minVertex;
    }

    private void PlaceVertex(Vector2f position)
    {
        // Prevent placing vertices in invalid locations.
        UpdateCanPlaceAt(position);
        if (!_canPlace) return;

        // Create a new vertex at the position.
        HashSet<string> usedLabels = new(_vertices.Select(x => x.Label));
        int i = 0;
        string label;
        while (usedLabels.Contains(label = Base26.Encode(i)))
            i++;
        var newVertex = new Vertex(position, VertexRadius, label);

        // Connect the nearest linkable vertex to the new vertex, if it exists.
        Vertex? linkVertex = FindNearestLinkableVertex(position);
        if (linkVertex is not null &&
            linkVertex.Connect(newVertex, out Edge? newEdge))
        {
            // Add the newly created edge.
            _edges.Add(newEdge);
        }

        // Add the new vertex.
        _vertices.Add(newVertex);
    }

    /// <summary>
    /// Begins dragging a vertex from the specified mouse position.
    /// </summary>
    private void BeginDrag(Vertex vertex, Vector2f mousePos)
    {
        _draggingVertex = vertex;
        _draggingFrom = vertex.Position;
        _dragOffset = vertex.Position - mousePos;
        _canPlace = true;
        _isDragging = true;
    }

    /// <summary>
    /// Updates dragging a vertex to the specified mouse position.
    /// </summary>
    private void UpdateDrag(Vector2f mousePos)
    {
        if (_draggingVertex is null) return;

        _draggingVertex.Position = mousePos + _dragOffset;
        _hoverCircle.Position = _draggingVertex.Position;
    }

    /// <summary>
    /// Ends dragging a vertex to the specified mouse position.
    /// </summary>
    private void EndDrag(Vector2f mousePos)
    {
        _isDragging = false;

        if (_draggingVertex is not null)
        {
            // Revert the vertex position back to where it was dragged from if it cannot be placed here.
            if (!_canPlace)
                _draggingVertex.Position = _draggingFrom;

            _draggingVertex = null;
        }

        UpdateHover(mousePos);
    }

    private void UpdateCanPlaceAt(Vector2f pos)
    {
        _canPlace = true;

        if (_isDragging)
            pos += _dragOffset;

        foreach (var vertex in _vertices)
        {
            if (vertex == _draggingVertex)
                continue;

            if (Maths.Distance(vertex.Position, pos) < VertexRadius * 3)
            {
                _canPlace = false;
                break;
            }
        }

        // TODO: Check edge collisions.
    }

    private void UpdateHover(Vector2f mousePos)
    {
        if (_isDragging)
        {
            if (_draggingVertex is not null)
            {
                _hoverCircle.Position = _draggingVertex.Position;
            }
        }
        else
        {
            _hoverVertex = GetVertexAtPoint(mousePos);
            if (_hoverVertex is not null)
                _hoverCircle.Position = _hoverVertex.Position;
        }
    }
    #endregion

    #region Events
    private void HandleMouseButtonPressed(object? sender, MouseButtonEventArgs e)
    {
        Vector2f mousePos = new(e.X, e.Y);

        if (_hoverVertex != null && e.Button == Mouse.Button.Right)
        {
            if (_goal is null)
            {
                if (_start is null)
                {
                    foreach (var edge in _edges)
                    {
                        edge.Color = new Color(0, 255, 0);
                    }
                    _start = _hoverVertex;
                    _startCircle.Position = _start.Position;
                    return;
                }
                else if (_start == _hoverVertex)
                {
                    return;
                }

                _goal = _hoverVertex;
                _goalCircle.Position = _goal.Position;

                _astarEnumerator = AStar(_start, _goal, Heuristics.Euclidean).GetEnumerator();
            }
        }

        if (e.Button == Mouse.Button.Left)
        {
            if (_hoverEdge is not null)
            {
                if (_hoverEdge.IsPotentialEdge)
                {
                    if (_hoverEdge.A.Connect(_hoverEdge.B, out Edge? newEdge))
                    {
                        newEdge.Color = Theme.Current.EdgeFill;
                        _edges.Add(newEdge);
                        _hoverEdge = null;
                        CalculatePotentialEdges();
                        return;
                    }
                }
                else
                {
                    if (_hoverEdge.A.Disconnect(_hoverEdge.B, out _))
                    {
                        _edges.Remove(_hoverEdge);
                        if (_hoverEdge.A.Connections.Count == 0)
                            _vertices.Remove(_hoverEdge.A);
                        if (_hoverEdge.B.Connections.Count == 0)
                            _vertices.Remove(_hoverEdge.B);
                        _hoverEdge = null;

                        CalculatePotentialEdges();
                        return;
                    }

                }
            }

            if (_hoverVertex is not null)
            {
                BeginDrag(_hoverVertex, mousePos);
            }
            else if (_canPlace)
            {
                PlaceVertex(mousePos);
                UpdateHover(mousePos);
            }
        }
    }

    private void HandleMouseMoved(object? sender, MouseMoveEventArgs e)
    {
        Vector2f mousePos = new(e.X, e.Y);

        if (_isDragging)
        {
            UpdateDrag(mousePos);
        }

        UpdateHover(mousePos);

        UpdateCanPlaceAt(mousePos);

        if (_hoverEdge is not null)
        {
            if (_hoverEdge.IsPotentialEdge)
            {
                _hoverEdge.IsVisible = false;
            }
        }
        _hoverEdge = null;

        if (Keyboard.IsKeyPressed(Keyboard.Key.LShift) && !_isDragging)
        {
            if (SnapPointToNearestEdge(_edges, mousePos, 10.0f, out Edge? edge, out Vector2f intersection) ||
                SnapPointToNearestEdge(_potentialEdges, mousePos, 30.0f, out edge, out intersection))
            {
                float distanceMouse = Maths.Distance(mousePos, intersection);
                if (distanceMouse <= VertexRadius * 2)
                {
                    _canPlace = false;

                    float distanceA = Maths.Distance(edge.A.Position, intersection);
                    float distanceB = Maths.Distance(edge.B.Position, intersection);
                    if (distanceA > edge.A.Radius * 2 &&
                        distanceB > edge.B.Radius * 2)
                    {
                        _hoverEdge = edge;
                        if (_hoverEdge.IsPotentialEdge)
                        {
                            _hoverEdge.IsVisible = true;
                        }
                        _edgeHoverPoint = intersection;
                    }
                }
            }
        }
    }

    private void HandleMouseButtonReleased(object? sender, MouseButtonEventArgs e)
    {
        Vector2f mousePos = new(e.X, e.Y);

        if (e.Button == Mouse.Button.Left && _isDragging)
        {
            EndDrag(mousePos);
        }

        CalculatePotentialEdges();
    }
    #endregion

    #region Main loop
    public void Run()
    {
        while (_window.IsOpen)
        {
            Update();
            Draw();
            ProcessEvents();
        }
    }

    private void Update()
    {
        _hoverCircle.FillColor =
            (_draggingVertex is not null)
            ? (_canPlace ? Theme.Current.VertexDragging : Theme.Current.VertexDraggingInvalid)
            : Theme.Current.VertexHover;
    }

    private void Draw()
    {
        _window.Clear(Theme.Current.Background);

        if (_start is not null)
            _window.Draw(_startCircle);
        if (_goal is not null)
            _window.Draw(_goalCircle);

        foreach (var edge in _edges.Concat(_potentialEdges))
        {
            if (edge.IsVisible)
                edge.Draw(_window);
        }

        if (_hoverEdge is not null)
        {
            _window.Draw(new SFML.Graphics.Vertex[]
            {
                new(_edgeHoverPoint + new Vector2f(-5, -5)) { Color = Color.Red },
                new(_edgeHoverPoint + new Vector2f(5, 5)) { Color = Color.Red },
            }, PrimitiveType.LineStrip);
            _window.Draw(new SFML.Graphics.Vertex[]
            {
                new(_edgeHoverPoint + new Vector2f(-5, 5)) { Color = Color.Red },
                new(_edgeHoverPoint + new Vector2f(5, -5)) { Color = Color.Red },
            }, PrimitiveType.LineStrip);
        }

        foreach (var vertex in _vertices)
        {
            if (vertex == _draggingVertex)
                continue;
            vertex.Draw(_window);
        }

        if (_draggingVertex is not null)
        {
            _draggingVertex.Draw(_window);
        }

        if (_hoverVertex is not null)
            _window.Draw(_hoverCircle);

        _window.Draw(_creditText);

        _window.Display();
    }

    private void ProcessEvents()
    {
        _window.DispatchEvents();
    }
    #endregion
}
