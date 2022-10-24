using SFML.System;

namespace AstarVisualizer;

/// <summary>
/// A utility class for mathematical calculations.
/// </summary>
public static class Maths
{
    /// <summary>
    /// Calculates the distance between two points.
    /// </summary>
    /// <param name="x1">The x coordinate of the first point.</param>
    /// <param name="y1">The y coordinate of the first point.</param>
    /// <param name="x2">The x coordinate of the second point.</param>
    /// <param name="y2">The y coordinate of the second point.</param>
    /// <returns>The distance between the two points.</returns>
    public static float Distance(float x1, float y1, float x2, float y2)
        => MathF.Sqrt(MathF.Pow(x1 - x2, 2) + MathF.Pow(y1 - y2, 2));

    /// <summary>
    /// Calculates the distance between two points.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>The distance between the two points.</returns>
    public static float Distance(Vector2f a, Vector2f b) => Distance(a.X, a.Y, b.X, b.Y);

    /// <summary>
    /// Calculates the distance between two points.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>The distance between the two points.</returns>
    public static float Distance(Vector2f a, Vector2i b) => Distance(a.X, a.Y, b.X, b.Y);

    /// <summary>
    /// Calculates the distance between two points.
    /// </summary>
    /// <param name="a">The first point.</param>
    /// <param name="b">The second point.</param>
    /// <returns>The distance between the two points.</returns>
    public static float Distance(Vector2i a, Vector2f b) => Distance(a.X, a.Y, b.X, b.Y);
}