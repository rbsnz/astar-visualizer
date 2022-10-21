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

    private readonly CircleShape _hoverCircle = new()
    {
        FillColor = new Color(255, 255, 255, 55),
        Radius = VertexRadius + 10,
        Origin = new Vector2f(VertexRadius + 10, VertexRadius + 10)
    };

    private Vertex? _hoverVertex;
    private Vertex? _draggingVertex;
    private Vector2f _dragOffset;
    private bool _canPlace;

    private Edge? _hoverEdge;
    private Vector2f _edgeHoverPoint;

    private Vertex? start, goal;

    public Visualizer()
    {
        _videoMode = new VideoMode(1280, 720);
        _window = new RenderWindow(_videoMode, "ISCG6426 Data Structures & Algorithms - A* Visualizer",
            Styles.Titlebar | Styles.Close, new ContextSettings() { AntialiasingLevel = 8 });
        _window.SetVerticalSyncEnabled(true);
        _window.Closed += (s, e) => _window.Close();

        _window.MouseMoved += HandleMouseMoved;
        _window.MouseButtonPressed += HandleMouseButtonPressed;
        _window.MouseButtonReleased += HandleMouseButtonReleased;
    }

    #region A*
    private List<Vertex>? AStar(Vertex start, Vertex goal, HeuristicFunc h)
    {
        PriorityQueue<Vertex, float> openSet = new();

        openSet.Enqueue(start, 0);

        Dictionary<Vertex, Vertex> cameFrom = new();

        Dictionary<Vertex, float> gScore = new();

        Dictionary<Vertex, float> fScore = new();

        gScore[start] = 0;
        fScore[start] = h(start, goal);

        while (openSet.Count > 0)
        {
            Vertex current = openSet.Dequeue();
            if (current == goal)
            {
                List<Vertex> path = new() { current };
                while (cameFrom.ContainsKey(current))
                {
                    current = cameFrom[current];
                    path.Insert(0, current);
                }
                return path;
            }

            foreach (Vertex neighbor in current.Connections)
            {
                float tentativeGscore = gScore.GetValueOrDefault(current, float.PositiveInfinity)
                    + Maths.Distance(current.Position, neighbor.Position);
                if (tentativeGscore < gScore.GetValueOrDefault(neighbor, float.PositiveInfinity))
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGscore;
                    fScore[neighbor] = tentativeGscore + h(neighbor, goal);
                    if (!openSet.UnorderedItems.Any(x => x.Element == neighbor))
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }

        return null;
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
                    if (edge.BelongsTo(vertexA) || edge.BelongsTo(vertexB)) continue;

                    if (Line.Intersects(edge.Line, new Line(vertexA.Position, vertexB.Position), out _))
                    {
                        valid = false;
                        break;
                    }
                }

                if (!valid)
                    continue;
                
                _potentialEdges.Add(new Edge(vertexA, vertexB) {
                    Color = Theme.PotentialEdgeFill,
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
    #endregion

    #region Events
    private void HandleMouseMoved(object? sender, MouseMoveEventArgs e)
    {
        Vector2i mousePos = Mouse.GetPosition(_window);
        Vector2f mousePosF = new(mousePos.X, mousePos.Y);

        if (_draggingVertex is not null)
        {
            _draggingVertex.Position = new Vector2f(e.X, e.Y) + _dragOffset;
            _hoverCircle.Position = _draggingVertex.Position;
        }
        else
        {
            _hoverVertex = GetVertexAtPoint(mousePosF);
            if (_hoverVertex is not null)
                _hoverCircle.Position = _hoverVertex.Position;
        }

        _canPlace = true;

        foreach (var vertex in _vertices)
        {
            if (vertex == _draggingVertex)
                continue;

            if (Maths.Distance(vertex.Position, mousePos) < VertexRadius * 3)
            {
                _canPlace = false;
                break;
            }
        }

        if (_hoverEdge is not null)
        {
            if (_hoverEdge.IsPotentialEdge)
            {
                _hoverEdge.IsVisible = false;
            }
        }
        _hoverEdge = null;

        if (!Keyboard.IsKeyPressed(Keyboard.Key.LShift)) return;

        if (SnapPointToNearestEdge(_edges, mousePosF, 10.0f, out Edge? edge, out Vector2f intersection) ||
            SnapPointToNearestEdge(_potentialEdges, mousePosF, 30.0f, out edge, out intersection))
        {
            float distanceMouse = Maths.Distance(mousePosF, intersection);
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

    private void HandleMouseButtonPressed(object? sender, MouseButtonEventArgs e)
    {
        Vector2f mousePosF = new(e.X, e.Y);
        Vertex? hoverVertex = GetVertexAtPoint(mousePosF);
        if (hoverVertex != null && e.Button == Mouse.Button.Right)
        {
            if (goal is null)
            {
                if (start is null)
                {
                    foreach (var edge in _edges)
                    {
                        edge.Color = new Color(0, 255, 0);
                    }
                    start = hoverVertex;
                    return;
                }
                else if (start == hoverVertex)
                {
                    return;
                }

                goal = hoverVertex;

                List<Vertex>? path = AStar(start, goal, Heuristics.Euclidean);
                if (path is not null)
                {
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        Vertex next = path[i + 1];
                        Edge pathEdge = path[i].Edges.First(e => e.A == next || e.B == next);
                        pathEdge.Color = new Color(255, 0, 255);
                    }
                }

                start = null;
                goal = null;
            }
        }

        if (e.Button != Mouse.Button.Left)
            return;

        Edge? newEdge;

        if (_hoverEdge is not null)
        {
            if (_hoverEdge.IsPotentialEdge)
            {
                if (_hoverEdge.A.Connect(_hoverEdge.B, out newEdge))
                {
                    newEdge.Color = Theme.EdgeFill;
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

        double minDistance = double.MaxValue;

        _draggingVertex = GetVertexAtPoint(mousePosF);
        if (_draggingVertex is not null)
        {
            _canPlace = true;
            _dragOffset = _draggingVertex.Position - mousePosF;
            return;
        }

        if (!_canPlace) return;

        // Prevent placing vertices too close to each other.
        if (minDistance <= (VertexRadius * 3)) return;

        Vertex? linkVertex = FindNearestLinkableVertex(new Vector2f(e.X, e.Y));
        if (linkVertex is null && _vertices.Count > 0)
            return;

        // Create a new vertex at the mouse location.
        var newVertex = new Vertex(new Vector2f(e.X, e.Y), VertexRadius);

        // Connect the last added vertex to the new vertex, if it exists.
        if (linkVertex is not null &&
            linkVertex.Connect(newVertex, out newEdge))
        {
            // Add the newly created edge.
            _edges.Add(newEdge);
        }

        // Add the new vertex.
        _vertices.Add(newVertex);
    }

    private void HandleMouseButtonReleased(object? sender, MouseButtonEventArgs e)
    {
        if (e.Button == Mouse.Button.Left &&
            _draggingVertex is not null)
        {
            _draggingVertex = null;
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
        _hoverCircle.FillColor = (_draggingVertex is not null) ? (_canPlace ? Theme.VertexDragging : Theme.VertexDraggingInvalid) : Theme.VertexHover;
    }

    private void Draw()
    {
        _window.Clear(Theme.Background);

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
            vertex.Draw(_window);
        }

        if (_hoverVertex is not null)
            _window.Draw(_hoverCircle);

        _window.Display();
    }

    private void ProcessEvents()
    {
        _window.DispatchEvents();
    }
    #endregion
}
