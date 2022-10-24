using SFML.Graphics;
using SFML.System;

namespace AstarVisualizer;

/// <summary>
/// A visual card that displays a vertex label, gScore and fScore.
/// </summary>
public class OpenVertexCard : Drawable
{
    private readonly RectangleShape _background;
    private readonly Text _textVertex, _textGscore, _textFscore;

    private Vector2f _position, _size, _origin;
    private float _gScore, _fScore;

    public Vertex Vertex { get; }

    public float GScore
    {
        get => _gScore;
        set
        {
            _gScore = value;
            UpdateGeometry();
        }
    }

    public float FScore
    {
        get => _fScore;
        set
        {
            _fScore = value;
            UpdateGeometry();
        }
    }

    public Vector2f TargetPosition { get; set; }

    public Vector2f Position
    {
        get => _position;
        set
        {
            if (_position == value) return;
            _position = value;
            UpdateGeometry();
        }
    }

    public Vector2f Size
    {
        get => _size;
        set
        {
            if (_size == value) return;
            _size = value;
            UpdateGeometry();
        }
    }

    public Vector2f Origin
    {
        get => _origin;
        set
        {
            if (_origin == value) return;
            _origin = value;
            UpdateGeometry();
        }
    }

    public OpenVertexCard(Vertex vertex, float gScore, float fScore)
    {
        Vertex = vertex;

        Color textColor = new(0, 0, 0, 225);

        _background = new RectangleShape()
        {
            Size = _size,
            FillColor = new Color(255, 255, 255, 225),
            OutlineColor = new Color(255, 255, 255, 128),
            OutlineThickness = 2
        };

        _textVertex = new Text()
        {
            Font = Theme.Current.Font,
            CharacterSize = 16,
            FillColor = textColor,
            DisplayedString = Vertex.Label
        };

        _textGscore = new Text()
        {
            Font = Theme.Current.Font,
            CharacterSize = 16,
            FillColor = textColor
        };

        _textFscore = new Text()
        {
            Font = Theme.Current.Font,
            CharacterSize = 16,
            FillColor = textColor
        };

        _textVertex.Center();

        GScore = gScore;
        FScore = fScore;
    }

    private void UpdateGeometry()
    {
        _background.Size = _size;
        _background.Origin = _origin;
        _background.Position = _position;

        _textGscore.DisplayedString = $"{GScore:0}";
        _textGscore.Center();
        _textFscore.DisplayedString = $"{FScore:0}";
        _textFscore.Center();

        _textVertex.Position = _position - _origin + new Vector2f(_size.X / 6, _size.Y / 2);
        _textGscore.Position = _position - _origin + new Vector2f(_size.X / 6 * 3, _size.Y / 2);
        _textFscore.Position = _position - _origin + new Vector2f(_size.X / 6 * 5, _size.Y / 2);
    }

    public void Draw(RenderTarget target) => Draw(target, RenderStates.Default);

    public void Draw(RenderTarget target, RenderStates states)
    {
        target.Draw(_background, states);
        target.Draw(_textVertex, states);
        target.Draw(_textGscore, states);
        target.Draw(_textFscore, states);
    }
}
