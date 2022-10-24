using SFML.System;

namespace AstarVisualizer;

/// <summary>
/// Represents a line segment.
/// </summary>
public struct Line
{
    /// <summary>
    /// The first point of this line segment.
    /// </summary>
    public Vector2f PointA;

    /// <summary>
    /// The second point of this line segment.
    /// </summary>
    public Vector2f PointB;

    /// <summary>
    /// Calculates the length of this line.
    /// </summary>
    public float Length => Maths.Distance(PointA, PointB);

    /// <summary>
    /// Calculates the angle of this line.
    /// </summary>
    public float Angle => MathF.Atan2(PointB.Y - PointA.Y, PointB.X - PointA.X);

    /// <summary>
    /// Constructs a new line segment between the specified points.
    /// </summary>
    /// <param name="pointA">The first point.</param>
    /// <param name="pointB">The second point.</param>
    public Line(Vector2f pointA, Vector2f pointB)
    {
        PointA = pointA;
        PointB = pointB;
    }

    /// <summary>
    /// Constructs a new line segment between the specified point coordinates.
    /// </summary>
    /// <param name="x1">The X coordinate of the first point.</param>
    /// <param name="y1">The Y coordinate of the first point.</param>
    /// <param name="x2">The X coordinate of the second point.</param>
    /// <param name="y2">The Y coordinate of the second point.</param>
    public Line(float x1, float y1, float x2, float y2)
        : this(new Vector2f(x1, y1), new Vector2f(x2, y2))
    { }
}
