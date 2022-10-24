using SFML.System;

namespace AstarVisualizer;

/// <summary>
/// Represents a circle.
/// </summary>
public struct Circle
{
    /// <summary>
    /// The position of the circle.
    /// </summary>
    public Vector2f Position;

    /// <summary>
    /// The radius of the circle.
    /// </summary>
    public float Radius;

    /// <summary>
    /// Constructs a new circle with the specified position and radius.
    /// </summary>
    /// <param name="position">The position of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    public Circle(Vector2f position, float radius)
    {
        Position = position;
        Radius = radius;
    }
}
