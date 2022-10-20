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

    private Vertex? _draggingVertex;
    private Vector2f _dragOffset;
    private bool _canPlace;
    private Vector2f? _snipPoint;

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

    #region Events
    private void HandleMouseMoved(object? sender, MouseMoveEventArgs e)
    {
        if (_draggingVertex is not null)
        {
            _draggingVertex.Position = new Vector2f(e.X, e.Y) + _dragOffset;
        }

        _snipPoint = null;

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

        foreach (var edge in _edges)
        {
            float lineAngle = MathF.Atan2(
                edge.A.Position.Y - edge.B.Position.Y,
                edge.A.Position.X - edge.B.Position.X
            );

            float parallelAngle = lineAngle + MathF.PI / 2;
            float lineLen = VertexRadius * 2;

            var mouseLine = new Line(
                new Vector2f(
                    mousePos.X + MathF.Cos(parallelAngle) * lineLen,
                    mousePos.Y + MathF.Sin(parallelAngle) * lineLen
                ),
                new Vector2f(
                    mousePos.X - MathF.Cos(parallelAngle) * lineLen,
                    mousePos.Y - MathF.Sin(parallelAngle) * lineLen
                )
            );

            var edgeLine = new Line(edge.A.Position, edge.B.Position);

            if (Line.Intersects(mouseLine, edgeLine, out Vector2f intersection))
            {
                _canPlace = false;

                float distanceMouse = Maths.Distance(intersection, mousePos);
                float distanceA = Maths.Distance(edge.A.Position, intersection);
                float distanceB = Maths.Distance(edge.B.Position, intersection);
                if (distanceMouse <= 10 &&
                    distanceA > edge.A.Radius * 2 &&
                    distanceB > edge.B.Radius * 2)
                {
                    _snipPoint = intersection;
                }
                break;
            }
        }
    }

    private void HandleMouseButtonPressed(object? sender, MouseButtonEventArgs e)
    {
        if (e.Button != Mouse.Button.Left)
            return;

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
    }
    #endregion

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

        if (_snipPoint.HasValue)
        {
            _window.Draw(new SFML.Graphics.Vertex[]
            {
                new(_snipPoint.Value + new Vector2f(-5, -5)) { Color = Color.Red },
                new(_snipPoint.Value + new Vector2f(5, 5)) { Color = Color.Red },
            }, PrimitiveType.LineStrip);
            _window.Draw(new SFML.Graphics.Vertex[]
            {
                new(_snipPoint.Value + new Vector2f(-5, 5)) { Color = Color.Red },
                new(_snipPoint.Value + new Vector2f(5, -5)) { Color = Color.Red },
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
}
