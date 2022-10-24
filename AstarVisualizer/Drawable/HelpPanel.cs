using SFML.Graphics;
using SFML.System;

namespace AstarVisualizer;

public class HelpPanel : Drawable
{
    private readonly RectangleShape _background = new()
    {
        FillColor = new Color(255, 255, 255, 225)
    };

    private readonly Text _helpText = new()
    {
        Font = Theme.Current.Font,
        CharacterSize = 16,
        DisplayedString = @"F1 - Toggle help

Click to place vertices.
Click + drag to move vertices.
Hold space, then click + drag to pan around.
Hold shift and hover over an existing edge, then click to delete it.
Hold shift and hover the cursor around a vertex to
see edges that may be inserted, then click to create them.

Right click a vertex to select the start, then right click
another to select the goal. Once a start/goal vertex has been
selected, press N to step through the search algorithm.

X - Reset the algorithm state.
C - Clear all vertices.
R - Generate a random graph.
L - Toggle displaying the log.
O - Toggle displaying the open list.",
        FillColor = new Color(0, 0, 0, 255)
    };

    public HelpPanel()
    {
        var bounds = _helpText.GetLocalBounds();
        Vector2f textSize = new Vector2f(bounds.Width, bounds.Height);
        _background.Size = textSize + new Vector2f(20, 20);
        _background.Origin = _background.Size / 2;
        _helpText.Origin = textSize / 2;
    }

    public void Draw(RenderTarget target) => Draw(target, RenderStates.Default);

    public void Draw(RenderTarget target, RenderStates states)
    {
        Vector2f center = new Vector2f((int)target.DefaultView.Center.X, (int)target.DefaultView.Center.Y);
        _background.Position = target.DefaultView.Center;
        _helpText.Position = center;

        _background.Draw(target, states);
        _helpText.Draw(target, states);
    }
}
