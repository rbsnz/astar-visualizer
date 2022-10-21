using SFML.Graphics;

namespace AstarVisualizer;

public class Edge
{
    private readonly LineShape _lineShape = new()
    {
        FillColor = Theme.EdgeFill,
        Weight = 6
    };

    public Vertex A { get; }
    public Vertex B { get; }

    /// <summary>
    /// Gets if this edge belongs to the specified vertex.
    /// </summary>
    public bool BelongsTo(Vertex vertex) => vertex == A || vertex == B;

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
        set => _lineShape.FillColor = value;
    }

    /// <summary>
    /// Gets or sets if this is a potential edge that may be inserted.
    /// </summary>
    public bool IsPotentialEdge { get; set; }

    /// <summary>
    /// Gets or sets if this edge is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    public Edge(Vertex a, Vertex b)
    {
        A = a;
        B = b;
        Update();
    }

    public void Update()
    {
        _lineShape.PointA = A.Position;
        _lineShape.PointB = B.Position;
    }

    public void Draw(RenderWindow window)
    {
        window.Draw(_lineShape);
    }
}
