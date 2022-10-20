using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System.Diagnostics.CodeAnalysis;

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
    private readonly List<Edge> _insertableEdges = new();

    private Vertex? _draggingVertex;
    private Vector2f _dragOffset;
    private bool _canPlace;

    private Edge? _hoverEdge;
    private Vector2f _edgeHoverPoint;

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

    #region Logic
    private void CalculateInsertableEdges()
    {
        _insertableEdges.Clear();

        for (int i = 0; i < _vertices.Count - 1; i++)
        {
            for (int j = i + 1; j < _vertices.Count; j++)
            {
                var vertexA = _vertices[i];
                var vertexB = _vertices[j];
                bool valid = true;
                if (vertexA.IsConnectedTo(vertexB))
                    continue;
                
                foreach (var edge in _edges)
                {
                    if (edge.A == vertexA || edge.A == vertexB ||
                        edge.B == vertexA || edge.B == vertexB) continue;

                    if (Line.Intersects(new Line(edge.A.Position, edge.B.Position),
                        new Line(vertexA.Position, vertexB.Position), out _))
                    {
                        valid = false;
                        break;
                    }
                }

                if (!valid)
                    continue;
                
                _insertableEdges.Add(new Edge(vertexA, vertexB) { Color = new Color(0, 0, 255, 0), Weight = 4 });
            }
        }
    }

    private bool FindNearestEdge(IEnumerable<Edge> edges, Vector2f point, float maxDistance,
        [NotNullWhen(true)] out Edge? edge, out Vector2f intersection)
    {
        edge = null;
        intersection = default;
        float minDistance = float.MaxValue;

        foreach (var e in edges)
        {
            float angle = MathF.Atan2(
                e.B.Position.Y - e.A.Position.Y,
                e.B.Position.X - e.A.Position.X
            );
            float parallelAngle = angle + MathF.PI / 2;
            Line line = new(
                new Vector2f(
                    point.X + MathF.Cos(parallelAngle) * 1000,
                    point.Y + MathF.Sin(parallelAngle) * 1000
                ),
                new Vector2f(
                    point.X - MathF.Cos(parallelAngle) * 1000,
                    point.Y - MathF.Sin(parallelAngle) * 1000
                )
            );

            Line edgeLine = new(e.A.Position, e.B.Position);
            if (Line.Intersects(edgeLine, line, out Vector2f inters))
            {
                float distance = Maths.Distance(point, inters);
                if (distance <= maxDistance && distance < minDistance)
                {
                    intersection = inters;
                    minDistance = distance;
                    edge = e;
                }
            }
        }

        return edge is not null;
    }
    #endregion

    #region Events
    private void HandleMouseMoved(object? sender, MouseMoveEventArgs e)
    {
        if (_draggingVertex is not null)
        {
            _draggingVertex.Position = new Vector2f(e.X, e.Y) + _dragOffset;
        }

        Vector2i mousePos = Mouse.GetPosition(_window);

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
            if (_hoverEdge.Color.B == 255)
            {
                _hoverEdge.Color = new Color(0, 0, 255, 0);
            }
        }
        _hoverEdge = null;

        Vector2f mousePosF = new(mousePos.X, mousePos.Y);
        if (FindNearestEdge(_edges, mousePosF, 10.0f, out Edge? edge, out Vector2f intersection) ||
            FindNearestEdge(_insertableEdges, mousePosF, 30.0f, out edge, out intersection))
        {
            float distanceMouse = Maths.Distance(mousePosF, intersection);
            if (distanceMouse <= VertexRadius * 2)
            {
                _canPlace = false;

                float distanceA = Maths.Distance(edge.A.Position, intersection);
                float distanceB = Maths.Distance(edge.B.Position, intersection);
                if (//distanceMouse <= 10 &&
                    distanceA > edge.A.Radius * 2 &&
                    distanceB > edge.B.Radius * 2)
                {
                    _hoverEdge = edge;
                    if (_hoverEdge.Color.B == 255)
                    {
                        _hoverEdge.Color = new Color(0, 0, 255, 128);
                    }
                    _edgeHoverPoint = intersection;
                }
            }
        }
    }

    private void HandleMouseButtonPressed(object? sender, MouseButtonEventArgs e)
    {
        if (e.Button != Mouse.Button.Left)
            return;

        if (_hoverEdge is not null)
        {
            if (_hoverEdge.Color.B == 255)
            {
                if (_hoverEdge.A.Connect(_hoverEdge.B, out Edge? newEdge))
                {
                    _edges.Add(newEdge);
                    _hoverEdge = null;
                    CalculateInsertableEdges();
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

                    CalculateInsertableEdges();
                    return;
                }

            }
        }

        double minDistance = double.MaxValue;

        Vector2f mousePos = new(e.X, e.Y);
        foreach (var vertex in _vertices)
        {
            float distance = Maths.Distance(mousePos, vertex.Position);
            if (distance < minDistance)
                minDistance = distance;
            if (distance <= vertex.Radius)
            {
                _draggingVertex = vertex;
                _dragOffset = vertex.Position - mousePos;
                return;
            }
        }

        if (!_canPlace) return;

        // Prevent placing vertices too close to each other.
        if (minDistance <= (VertexRadius * 3)) return;

        // Create a new vertex at the mouse location.
        var newVertex = new Vertex(new Vector2f(e.X, e.Y), VertexRadius);

        // Connect the last added vertex to the new vertex, if it exists.
        if (_vertices.Count > 0 && _vertices[^1].Connect(newVertex, out Edge? edge))
        {
            // Add the newly created edge.
            _edges.Add(edge);
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

        CalculateInsertableEdges();
    }
    #endregion

    #region Event loop
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
        Time delta = _clock.Restart();
    }

    private void Draw()
    {
        _window.Clear(Color.White);

        foreach (var edge in _edges)
        {
            edge.Draw(_window);
        }

        foreach (var edge in _insertableEdges)
        {
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

        _window.Display();
    }

    private void ProcessEvents()
    {
        _window.DispatchEvents();
    }
    #endregion
}
