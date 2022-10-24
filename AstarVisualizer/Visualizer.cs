using System.Data;
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
    private readonly View _view;

    private readonly Clock _clock = new();

    private readonly List<Vertex> _vertices = new();
    private readonly List<Edge> _edges = new();
    private readonly List<Edge> _potentialEdges = new();

    private readonly List<OpenVertexCard> _openVertices = new();

    private readonly List<string> _log = new();
    private const int MaxLogLines = 10;

    private readonly Text _creditText = new()
    {
        Font = Theme.Current.Font,
        CharacterSize = 20,
        DisplayedString = "A* Visualizer by rob",
        FillColor = new Color(255, 255, 255, 100)
    };

    private readonly Text _logText = new()
    {
        Font = Theme.Current.Font,
        CharacterSize = 14,
        DisplayedString = "",
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

    private bool _showLog = true, _showOpenList = true;

    private Vertex? _hoverVertex;
    private bool _canPlace;

    private bool _isPanning;
    private Vector2f _panOrigin;
    private Vector2i _panFrom;

    private bool _isDragging;
    private Vertex? _draggingVertex;
    private Vector2f _draggingFrom;
    private Vector2f _dragOffset;

    private Edge? _hoverEdge;
    private Vector2f _edgeHoverPoint;

    private Vertex? _start, _goal;

    private AStar? _astar;
    private bool _canStep = true;

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
        _window.KeyReleased += HandleKeyReleased;

        _view = new View(_window.DefaultView);

        var textBounds = _creditText.GetLocalBounds();
        _creditText.Origin = new Vector2f(0, textBounds.Height);
        _creditText.Position = new Vector2f(10, _videoMode.Height - 10);

        _logText.Position = new Vector2f(10, 10);
    }

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
        Time deltaTime = _clock.Restart();
        float delta = deltaTime.AsSeconds();

        _hoverCircle.FillColor =
            (_draggingVertex is not null)
            ? (_canPlace ? Theme.Current.VertexDragging : Theme.Current.VertexDraggingInvalid)
            : Theme.Current.VertexHover;

        foreach (var card in _openVertices)
        {
            card.Position += (card.TargetPosition - card.Position) * delta * 5;
        }
    }

    private void Draw()
    {
        _window.Clear(Theme.Current.Background);

        // World space
        _window.SetView(_view);

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

        // Screen space
        _window.SetView(_window.DefaultView);

        if (_showOpenList)
        {
            foreach (var openVertex in _openVertices)
                openVertex.Draw(_window);
        }

        if (_showLog)
        {
            _window.Draw(_logText);
        }

        _window.Draw(_creditText);

        _window.Display();
    }

    private void ProcessEvents()
    {
        _window.DispatchEvents();
    }

    #endregion

    #region Logic

    /// <summary>
    /// Adds a line to the log.
    /// </summary>
    private void Log(string line)
    {
        _log.Add(line);
        while (_log.Count > MaxLogLines)
            _log.RemoveAt(0);
        _logText.DisplayedString = string.Join("\n", _log);
    }

    /// <summary>
    /// Clears all vertices and edges.
    /// </summary>
    private void Clear()
    {
        _vertices.Clear();
        _edges.Clear();
        _hoverVertex = null;
        _hoverEdge = null;
        Reset();
    }

    /// <summary>
    /// Resets the algorithm state.
    /// </summary>
    private void Reset()
    {
        _start = _goal = null;
        _astar = null;
        _log.Clear();
        _logText.DisplayedString = string.Empty;
        _openVertices.Clear();
        foreach (var vertex in _vertices)
            vertex.State = AState.None;
        foreach (var edge in _edges)
            edge.State = AState.None;
    }

    /// <summary>
    /// Generates a random graph.
    /// </summary>
    private void GenerateRandomGraph()
    {
        Clear();

        float padding = VertexRadius * 8;

        float left = _view.Center.X - (_view.Size.X / 2) + padding;
        float top = _view.Center.Y - (_view.Size.Y / 2) + padding;
        float width = _view.Size.X - padding * 2;
        float height = _view.Size.Y - padding * 2;

        int n = Random.Shared.Next(16, 33);
        while (_vertices.Count < n)
        {
            PlaceVertex(new Vector2f(
                left + (float)(Random.Shared.NextDouble() * width),
                top + (float)(Random.Shared.NextDouble() * height)
            ));
        }

        CalculatePotentialEdges();
        foreach (var edge in _potentialEdges)
        {
            if (Random.Shared.NextDouble() < 0.05)
            {
                if (edge.A.Connect(edge.B, out Edge? ee))
                    _edges.Add(ee);
            }
        }
    }

    /// <summary>
    /// Sorts and updates the target positions of open vertex cards.
    /// </summary>
    private void UpdateOpenVertices()
    {
        _openVertices.Sort((a, b) => a.FScore.CompareTo(b.FScore));
        for (int i = 0; i < _openVertices.Count; i++)
            _openVertices[i].TargetPosition = new Vector2f(_openVertices[i].Position.X, 10 + (50 * i));
    }

    /// <summary>
    /// Advances the A* algorithm by one step.
    /// </summary>
    private void Step()
    {
        if (_astar is null || !_canStep) return;

        if (!_astar.Step())
        {
            _start = null;
            _goal = null;
            _log.Clear();
            _logText.DisplayedString = string.Empty;

            foreach (var vertex in _vertices)
                vertex.State = AState.None;
            foreach (var edge in _edges)
                edge.State = AState.None;

            _openVertices.Clear();

            return;
        }

        switch (_astar.CurrentStep)
        {
            case BeginSearch:
                {
                    Log("Beginning search");
                }
                break;
            case VisitVertex visitVertex:
                {
                    Log($"Visiting {visitVertex.Vertex}");
                    var card = _openVertices.FirstOrDefault(x => x.Vertex == visitVertex.Vertex);
                    if (card is not null)
                    {
                        _openVertices.Remove(card);
                        UpdateOpenVertices();
                    }
                }
                break;
            case ConsiderVertex considerVertex:
                {
                    Log($"Considering {considerVertex.To}, gScore = {considerVertex.GScore:N0}");
                }
                break;
            case OpenVertex openVertex:
                {
                    Log($"Adding {openVertex.Vertex} to open set, fScore = {openVertex.FScore:N0}");
                    var card = new OpenVertexCard(openVertex.Vertex, openVertex.GScore, openVertex.FScore)
                    {
                        Size = new Vector2f(200, 40),
                        Origin = new Vector2f(200, 0)
                    };
                    card.Position = card.TargetPosition = new Vector2f(_videoMode.Width - 10, 10 + (50 * _openVertices.Count));
                    _openVertices.Add(card);
                    UpdateOpenVertices();
                }
                break;
            case UpdateVertex updateVertex:
                {
                    Log($"Updating {updateVertex.Vertex}, fScore = {updateVertex.FScore:N0}");
                    var card = _openVertices.FirstOrDefault(x => x.Vertex == updateVertex.Vertex);
                    if (card is not null)
                    {
                        card.GScore = updateVertex.GScore;
                        card.FScore = updateVertex.FScore;
                        UpdateOpenVertices();
                    }
                }
                break;
            case DiscardPath discardPath:
                {
                    string message = discardPath.Reason switch
                    {
                        DiscardPathReason.DeadEnd => "Dead end, discarding path",
                        DiscardPathReason.ShorterRouteFound => $"Shorter route to {discardPath.To} found, discarding previous path",
                        DiscardPathReason.ShorterRouteExists => $"Shorter route to {discardPath.To} exists, discarding path",
                        _ => throw new Exception("Unknown discard reason")
                    };
                    Log($"{message} {string.Join("->", discardPath.Path)}");
                }
                break;
            case EndSearch endSearch:
                {
                    _openVertices.Clear();
                    _canStep = false;

                    Log(endSearch.Success ? "Found path to goal" : "No solution found");
                }
                break;
        }
    }

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
    /// Checks if a vertex can be placed or moved to the specified point.
    /// </summary>
    /// <param name="point">The point to check if a vertex can be placed at or moved to.</param>
    /// <param name="dragVertex">The vertex being dragged, or null if placing a new vertex.</param>
    /// <returns>Whether a vertex can be placed at/moved to the specified location.</returns>
    private bool CanPlaceVertexAt(Vector2f point, Vertex? dragVertex)
    {
        // Check each vertex and return false if the point is too close.
        foreach (Vertex vertex in _vertices)
        {
            if (vertex == dragVertex)
                continue;
            if (Maths.Distance(vertex.Position, point) <= vertex.Radius * 3)
                return false;
        }

        // Check each edge and return false if the point is too close to the line.
        foreach (Edge edge in _edges)
        {
            // Ignore this edge if it is connected to the drag vertex.
            if (dragVertex is not null && edge.IsConnectedTo(dragVertex))
                continue;
            // TODO: circle/line collision detection
            Line edgeLine = edge.Line;
            float angle = edgeLine.Angle;
            float rightAngle = angle + MathF.PI / 4;
            Vector2f offset = new(MathF.Cos(rightAngle) * VertexRadius * 3, MathF.Sin(rightAngle) * VertexRadius * 3);
            Line placementLine = new(point + offset, point - offset);
            if (Line.Intersects(edgeLine, placementLine, out _))
                return false;
        }

        return true;
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

                _potentialEdges.Add(new Edge(vertexA, vertexB)
                {
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
    /// <param name="edge">When the method returns, contains the nearest edge to the specified point, if one was found.</param>
    /// <param name="intersection">When the method returns, contains the point along the nearest edge, if one was found.</param>
    /// <returns>Whether an edge was found or not.</returns>
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

    private void UpdateCanPlaceAt(Vector2f pos)
    {
        if (_isDragging)
            pos += _dragOffset;

        _canPlace = CanPlaceVertexAt(pos, _draggingVertex);
    }

    private void UpdateHover(Vector2f pos)
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
            _hoverVertex = GetVertexAtPoint(pos);
            if (_hoverVertex is not null)
                _hoverCircle.Position = _hoverVertex.Position;
        }
    }

    private Vertex? PlaceVertex(Vector2f position)
    {
        // Prevent placing vertices in invalid locations.
        UpdateCanPlaceAt(position);
        if (!_canPlace) return null;

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

        // Update potential edges.
        CalculatePotentialEdges();

        return newVertex;
    }

    #region Panning

    private void BeginPan(Vector2i mousePos)
    {
        _isPanning = true;
        _panFrom = mousePos;
        _panOrigin = _view.Center;
    }

    private void UpdatePan(Vector2i mousePos)
    {
        if (!_isPanning) return;

        _view.Center = _panOrigin - (mousePos - _panFrom).ToVector2f();
    }

    private void EndPan()
    {
        _isPanning = false;

        // Reset the view position back to the center of the graph if all vertices are outside the view.
        if (_vertices.Count > 0)
        {
            FloatRect viewBounds = new(_view.Center - _view.Size / 2, _view.Size);
            bool isInBounds = false;
            float
                left = float.PositiveInfinity,
                right = float.NegativeInfinity,
                top = float.PositiveInfinity,
                bottom = float.NegativeInfinity;

            foreach (var vertex in _vertices)
            {
                if (vertex.X < left) left = vertex.X;
                if (vertex.X > right) right = vertex.X;
                if (vertex.Y < top) top = vertex.Y;
                if (vertex.Y > bottom) bottom = vertex.Y;
                if (!isInBounds && viewBounds.Contains(vertex.X, vertex.Y))
                    isInBounds = true;
            }

            if (!isInBounds)
            {
                _view.Center = new Vector2f(left + (right - left) / 2, top + (bottom - top) / 2);
            }
        }
    }

    #endregion

    #region Dragging

    /// <summary>
    /// Begins dragging a vertex from the specified mouse position.
    /// </summary>
    private void BeginDrag(Vertex vertex, Vector2f pos)
    {
        _draggingVertex = vertex;
        _draggingFrom = vertex.Position;
        _dragOffset = vertex.Position - pos;
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

    #endregion

    #endregion

    #region Events

    private void HandleMouseButtonPressed(object? sender, MouseButtonEventArgs e)
    {
        Vector2i mousePos = new(e.X, e.Y);
        Vector2f pos = _window.MapPixelToCoords(mousePos, _view);

        if (e.Button == Mouse.Button.Left &&
            Keyboard.IsKeyPressed(Keyboard.Key.Space))
        {
            BeginPan(mousePos);
            return;
        }

        if (_hoverVertex != null && e.Button == Mouse.Button.Right)
        {
            if (_goal is null)
            {
                if (_start is null)
                {
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

                _log.Clear();
                _logText.DisplayedString = "";

                _astar = new AStar(_vertices, Heuristics.Euclidean, _start, _goal);
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
                BeginDrag(_hoverVertex, pos);
            }
            else if (_canPlace)
            {
                PlaceVertex(pos);
                UpdateHover(pos);
            }
        }
    }

    private void HandleMouseMoved(object? sender, MouseMoveEventArgs e)
    {
        Vector2i mousePos = new(e.X, e.Y);
        Vector2f pos = _window.MapPixelToCoords(mousePos, _view);

        if (_isPanning)
        {
            UpdatePan(mousePos);
            return;
        }

        if (_isDragging)
        {
            UpdateDrag(pos);
        }

        UpdateHover(pos);

        UpdateCanPlaceAt(pos);

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
            if (SnapPointToNearestEdge(_edges, pos, 10.0f, out Edge? edge, out Vector2f intersection) ||
                SnapPointToNearestEdge(_potentialEdges, pos, 30.0f, out edge, out intersection))
            {
                float distanceMouse = Maths.Distance(pos, intersection);
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
        Vector2i mousePos = new(e.X, e.Y);
        Vector2f pos = _window.MapPixelToCoords(mousePos, _view);

        if (_isPanning && e.Button == Mouse.Button.Left)
        {
            EndPan();
        }

        if (_isDragging && e.Button == Mouse.Button.Left)
        {
            EndDrag(pos);
            CalculatePotentialEdges();
        }
    }

    private void HandleKeyPressed(object? sender, KeyEventArgs e)
    {
        switch (e.Code)
        {
            case Keyboard.Key.C: Clear(); break;
            case Keyboard.Key.X: Reset(); break;
            case Keyboard.Key.R: GenerateRandomGraph(); break;
            case Keyboard.Key.N: Step(); break;
            case Keyboard.Key.L: _showLog = !_showLog; break;
            case Keyboard.Key.O: _showOpenList = !_showOpenList; break;
        }
    }

    private void HandleKeyReleased(object? sender, KeyEventArgs e)
    {
        if (e.Code == Keyboard.Key.N)
        {
            _canStep = true;
        }
    }

    #endregion

}
