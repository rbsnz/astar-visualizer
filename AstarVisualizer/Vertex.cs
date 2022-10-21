using System.Diagnostics.CodeAnalysis;

using SFML.Graphics;
using SFML.System;

namespace AstarVisualizer;

public class Vertex
{
    private Vector2f _position;
    private readonly CircleShape _circle;
    private readonly Dictionary<Vertex, Edge> _edges = new();

    /// <summary>
    /// Gets or sets the position of this vertex.
    /// </summary>
    public Vector2f Position
    {
        get => _position;
        set
        {
            _position = value;
            _circle.Position = value;
            foreach (var edge in _edges.Values)
            {
                edge.Update();
            }
        }
    }

    /// <summary>
    /// Gets the X coordinate of this vertex.
    /// </summary>
    public float X => Position.X;

    /// <summary>
    /// Gets the Y coordinate of this vertex.
    /// </summary>
    public float Y => Position.Y;

    /// <summary>
    /// Gets the visual radius of this vertex.
    /// </summary>
    public float Radius => _circle.Radius;

    /// <summary>
    /// Gets the vertices connected to this vertex.
    /// </summary>
    public IReadOnlyCollection<Vertex> Connections => _edges.Keys;

    /// <summary>
    /// Gets the edges connected to this vertex.
    /// </summary>
    public IReadOnlyCollection<Edge> Edges => _edges.Values;

    /// <summary>
    /// Constructs a new vertex with the specified position and radius.
    /// </summary>
    public Vertex(Vector2f position, float radius)
    {
        _position = position;
        _circle = new CircleShape(radius)
        {
            Origin = new Vector2f(radius, radius),
            Position = _position,
            FillColor = Theme.Current.VertexFill,
            OutlineColor = Color.Black,
            OutlineThickness = 2
        };
    }

    /// <summary>
    /// Gets if this vertex belongs to the specified edge.
    /// </summary>
    public bool BelongsTo(Edge edge) => (this == edge.A) || (this == edge.B);

    /// <summary>
    /// Connects this vertex to another vertex and produces an edge.
    /// </summary>
    /// <param name="other"></param>
    /// <param name="edge">
    /// </param>
    /// <returns><see langword="true"/> if the connection was created, or <see langword="false"/> if it already exists.</returns>
    public bool Connect(Vertex other, [NotNullWhen(true)] out Edge? edge)
    {
        edge = new Edge(this, other);

        if (!_edges.TryAdd(other, edge))
            return false;

        // This should not happen, so throw an exception.
        if (!other._edges.TryAdd(this, edge))
            throw new InvalidOperationException("Failed to connect vertices.");

        return true;
    }

    /// <summary>
    /// Gets if this vertex is connected to another vertex.
    /// </summary>
    public bool IsConnectedTo(Vertex other) => _edges.ContainsKey(other);

    /// <summary>
    /// Disconnects this vertex from another vertex.
    /// </summary>
    /// <param name="other">The other vertex to remove the connection from.</param>
    /// <returns><see langword="true"/> if the connection was removed, or <see langword="false"/> if it does not exist.</returns>
    public bool Disconnect(Vertex other, out Edge? edge)
    {
        if (!_edges.Remove(other, out edge))
            return false;

        // This should not happen, so throw an exception.
        if (!other._edges.Remove(this))
            throw new InvalidOperationException("Failed to disconnect vertices.");

        return true;
    }

    /// <summary>
    /// Draws this vertex to the specified render target.
    /// </summary>
    public void Draw(RenderTarget target)
    {
        target.Draw(_circle);
    }
}
