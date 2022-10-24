using SFML.Graphics;
using SFML.System;

namespace AstarVisualizer;

/// <summary>
/// An SFML utility class.
/// </summary>
public static class SfmlUtil
{
    /// <summary>
    /// Converts the specified <see cref="Vector2i"/> into a <see cref="Vector2f"/>.
    /// </summary>
    public static Vector2f ToVector2f(this Vector2i v) => new(v.X, v.Y);

    /// <summary>
    /// Sets the specified text's origin to its center based on its local bounds.
    /// </summary>
    public static void Center(this Text text)
    {
        var bounds = text.GetLocalBounds();
        float underlinePosition = text.Font.GetUnderlinePosition(text.CharacterSize);
        text.Origin = new Vector2f(bounds.Width / 2, bounds.Height - (underlinePosition / 2));
    }
}
