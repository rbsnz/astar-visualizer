using SFML.Graphics;
using SFML.System;

namespace AstarVisualizer;

public static class SfmlUtil
{
    public static Vector2f ToVector2f(this Vector2i v) => new(v.X, v.Y);

    public static void Center(this Text text)
    {
        var bounds = text.GetLocalBounds();
        float underlinePosition = text.Font.GetUnderlinePosition(text.CharacterSize);
        text.Origin = new Vector2f(bounds.Width / 2, bounds.Height - (underlinePosition / 2));
    }
}
