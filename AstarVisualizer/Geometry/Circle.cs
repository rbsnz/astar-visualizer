using SFML.System;

namespace AstarVisualizer;

/// <summary>
/// Represents a circle.
/// </summary>
public struct Circle
{
    public Vector2f Position;
    public float Radius;

    public Circle(Vector2f position, float radius)
    {
        Position = position;
        Radius = radius;
    }
}