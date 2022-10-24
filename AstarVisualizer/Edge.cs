using SFML.Graphics;
using SFML.System;

namespace AstarVisualizer;

public class Edge
{
    private readonly LineShape _lineShape = new() { Weight = 6 };

    /// <summary>
    /// Gets the first vertex connected to this edge.
    /// </summary>
    public Vertex A { get; }

    /// <summary>
    /// Gets the second vertex connected to this edge.
    /// </summary>
    public Vertex B { get; }

    /// <summary>
    /// Gets if this edge is connected to the specified vertex.
    /// </summary>
    public bool IsIncidentTo(Vertex vertex) => vertex == A || vertex == B;

    /// <summary>
    /// Gets if this edge shares a common vertex with another edge.
    /// </summary>
    public bool IsIncidentTo(Edge edge) => edge.IsIncidentTo(A) || edge.IsIncidentTo(B);

    /// <summary>
    /// Gets a line that represents this edge.
    /// </summary>
    public Line Line => new(A.Position, B.Position);

    /// <summary>
    /// Gets or sets the line weight of this edge.
    /// </summary>
    public float Weight
    {
        get => _lineShape.Weight;
        set => _lineShape.Weight = value;
    }

    /// <summary>
    /// Gets or sets the color of this edge.
    /// </summary>
    public Color Color
    {
        get => _lineShape.FillColor;
        protected set => _lineShape.FillColor = value;
    }

    /// <summary>
    /// Gets or sets if this is a potential edge that may be inserted.
    /// </summary>
    public bool IsPotentialEdge { get; set; }

    /// <summary>
    /// Gets or sets if this edge is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    private AState _state = AState.None;
    public AState State
    {
        get => _state;
        set
        {
            _state = value;
            Color = _state switch
            {
                AState.None => Theme.Current.EdgeFill,
                AState.Invalid => Theme.Current.EdgeStateInvalid,
                AState.Unvisited => Theme.Current.EdgeStateUnvisited,
                AState.Potential => Theme.Current.EdgeStatePotential,
                AState.Inspecting => Theme.Current.EdgeStateInspecting,
                AState.Eliminated => Theme.Current.EdgeStateEliminated,
                AState.Success => Theme.Current.EdgeStateSuccess,
                _ => new Color(255, 0, 0)
            };
        }
    }

    /// <summary>
    /// Constructs a new edge that connects the specified vertices.
    /// </summary>
    /// <param name="a">The first vertex.</param>
    /// <param name="b">The second vertex.</param>
    public Edge(Vertex a, Vertex b)
    {
        A = a;
        B = b;
        State = AState.None;
        Update();
    }

    /// <summary>
    /// Updates the geometry for this edge.
    /// </summary>
    public void Update()
    {
        _lineShape.PointA = A.Position;
        _lineShape.PointB = B.Position;
    }

    /// <summary>
    /// Draws this edge to the specified render target.
    /// </summary>
    public void Draw(RenderTarget target)
    {
        target.Draw(_lineShape);
    }
}
