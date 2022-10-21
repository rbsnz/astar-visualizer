using SFML.Graphics;
using SFML.System;

namespace AstarVisualizer;

public class LineShape : RectangleShape
{
    private float _weight;
    private Vector2f _pointA, _pointB;

    /// <summary>
    /// Gets the length of this line.
    /// </summary>
    public float Length { get; private set; }

    /// <summary>
    /// Gets or sets the weight of this line.
    /// </summary>
    public float Weight
    {
        get => _weight;
        set
        {
            if (_weight == value)
                return;
            _weight = value;
            CalculateGeometry();
        }
    }

    /// <summary>
    /// Gets or sets the first point of this line.
    /// </summary>
    public Vector2f PointA
    {
        get => _pointA;
        set
        {
            if (_pointA == value)
                return;
            _pointA = value;
            CalculateGeometry();
        }
    }

    /// <summary>
    /// Gets or sets the second point of this line.
    /// </summary>
    public Vector2f PointB
    {
        get => _pointB;
        set
        {
            if (_pointB == value)
                return;
            _pointB = value;
            CalculateGeometry();
        }
    }

    /// <summary>
    /// Calculates the geometry of this line.
    /// </summary>
    private void CalculateGeometry()
    {
        Length = Maths.Distance(PointA, PointB);
        Size = new Vector2f(Length + _weight, _weight);
        Origin = new Vector2f(_weight / 2, _weight / 2);

        Position = PointA;

        float angle = MathF.Atan2(PointB.Y - PointA.Y, PointB.X - PointA.X);
        Rotation = (angle / (MathF.PI * 2)) * 360.0f;
    }
}
