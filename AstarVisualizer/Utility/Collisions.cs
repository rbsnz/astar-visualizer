/*
 * References:
 * https://en.wikipedia.org/wiki/Line%2dline_intersection#Given_two_points_on_each_line_segment
 */

using SFML.System;

namespace AstarVisualizer;

/// <summary>
/// A helper class for detecting collisions.
/// </summary>
public static class Collisions
{
    /// <summary>
    /// Checks if a vertex intersects with a circle of the specified radius.
    /// </summary>
    /// <param name="vertex">The vertex to check for a collision with.</param>
    /// <param name="circle">The circle to check for a collision with.</param>
    /// <returns>Whether the vertex intersects with the circle.</returns>
    public static bool Intersects(this Vertex vertex, Circle circle)
        => Maths.Distance(vertex.Position, circle.Position) <= (vertex.Radius + circle.Radius);

    /// <summary>
    /// Checks if an edge intersects with a circle of the specified radius.
    /// </summary>
    /// <param name="edge">The edge to check for a collision with.</param>
    /// <param name="circle">The circle to check for a collision with.</param>
    /// <returns>Whether the edge intersects with the circle.</returns>
    public static bool Intersects(this Edge edge, Circle circle)
    {
        Line edgeLine = edge.Line;
        float rightAngle = edgeLine.Angle + MathF.PI / 4;
        Vector2f offset = new(MathF.Cos(rightAngle) * circle.Radius, MathF.Sin(rightAngle) * circle.Radius);
        Line collisionLine = new(circle.Position + offset, circle.Position - offset);
        return edgeLine.Intersects(collisionLine, out _);
    }

    /// <summary>
    /// Checks if a vertex intersects with a circle of the specified radius around another vertex.
    /// </summary>
    /// <param name="vertex">The vertex to check.</param>
    /// <param name="other">The other vertex to check for a collision with.</param>
    /// <param name="radius">The radius to check around each vertex.</param>
    /// <returns>Whether the two vertices intersect.</returns>
    public static bool Intersects(this Vertex vertex, Vertex other, float radius) => Maths.Distance(vertex.Position, other.Position) <= radius * 2;

    /// <summary>
    /// Checks if an edge intersects with a circle of the specified radius around a vertex.
    /// </summary>
    /// <param name="edge">The edge to check for a collision.</param>
    /// <param name="vertex">The vertex to check for a collision.</param>
    /// <param name="radius">The radius around the vertex to check.</param>
    /// <returns>Whether the edge intersects with the circle around the vertex.</returns>
    public static bool Intersects(this Edge edge, Vertex vertex, float radius)
        => Intersects(edge, new Circle(vertex.Position, radius));

    /// <summary>
    /// Checks if an edge intersects with another edge.
    /// </summary>
    /// <param name="edge">The edge to check to check for a collision.</param>
    /// <param name="other">The other edge to check for a collision with.</param>
    /// <returns>Whether the two edges intersect.</returns>
    public static bool Intersects(this Edge edge, Edge other) => edge.Line.Intersects(other.Line, out _);

    /// <summary>
    /// Calculates the point at which two line segments intersect.
    /// </summary>
    /// <param name="line">The first line.</param>
    /// <param name="other">The second line.</param>
    /// <param name="intersection">
    /// When the method returns, contains the point at which the two lines intersect, if there is an intersection.
    /// </param>
    /// <returns><see langword="true"/> if the lines intersect, otherwise <see langword="false"/>.</returns>
    public static bool Intersects(this Line line, Line other, out Vector2f intersection)
    {
        float
            x1 = line.PointA.X, y1 = line.PointA.Y,
            x2 = line.PointB.X, y2 = line.PointB.Y,
            x3 = other.PointA.X, y3 = other.PointA.Y,
            x4 = other.PointB.X, y4 = other.PointB.Y;

        float t =
            ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) /
            ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));
        float u =
            ((x1 - x3) * (y1 - y2) - (y1 - y3) * (x1 - x2)) /
            ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));

        if ((0 <= t && t <= 1) && (0 <= u && u <= 1))
        {
            intersection = new Vector2f(x1 + t * (x2 - x1), y1 + t * (y2 - y1));
            return true;
        }
        else
        {
            intersection = default;
            return false;
        }
    }
}
