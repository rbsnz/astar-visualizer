using SFML.System;

namespace AstarVisualizer;

public static class SfmlUtil
{
    public static Vector2f ToVector2f(this Vector2i v) => new(v.X, v.Y);
}
