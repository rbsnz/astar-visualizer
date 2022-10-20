using SFML.Graphics;
using SFML.System;

namespace AstarVisualizer;

public class LineShape : RectangleShape
{
    private float _weight;
    private Vector2f _pointA, _pointB;

    public float Distance { get; private set; }

    public float Weight
    {
        get => _weight;
        set
        {
            if (_weight == value)
                return;
            _weight = value;
            CalculateShape();
        }
    }

    public Vector2f PointA
    {
        get => _pointA;
        set
        {
            if (_pointA == value)
                return;
            _pointA = value;
            CalculateShape();
        }
    }

    public Vector2f PointB
    {
        get => _pointB;
        set
        {
            if (_pointB == value)
                return;
            _pointB = value;
            CalculateShape();
        }
    }

    private void CalculateShape()
    {
        Distance = Maths.Distance(PointA, PointB);
        Size = new Vector2f(Distance + _weight, _weight);
        Origin = new Vector2f(_weight / 2, _weight / 2);

        Position = PointA;

        float angle = MathF.Atan2(PointB.Y - PointA.Y, PointB.X - PointA.X);
        Rotation = (angle / (MathF.PI * 2)) * 360.0f;
    }
}
