using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace RendererBench.Scenes;

/// <summary>
/// Text at mixed sizes. Static text is shaped once and moves; dynamic text is reshaped every frame.
/// </summary>
internal sealed class TextScene : DrawnScene
{
    private static readonly string[] s_words =
    {
        "The quick brown fox jumps over the lazy dog", "Avalonia", "Impeller vs Skia", "0123456789",
        "Lorem ipsum dolor sit amet", "ÀÉÎÕÜ ßæø", "Frame pacing", "Glyph cache",
    };

    private readonly (double X, double Y, double Size)[] _items;
    private readonly FormattedText[]? _cached;

    public TextScene(double scale, bool dynamic) : base(scale)
    {
        var rng = new Random(3);
        _items = new (double, double, double)[Count(dynamic ? 300 : 400)];
        for (var i = 0; i < _items.Length; i++)
            _items[i] = (rng.NextDouble(), rng.NextDouble(), 10 + rng.Next(23));

        if (!dynamic)
        {
            _cached = new FormattedText[_items.Length];
            for (var i = 0; i < _items.Length; i++)
                _cached[i] = Create(s_words[i % s_words.Length], i);
        }
    }

    private FormattedText Create(string text, int i) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, _items[i].Size,
            Palette[i % Palette.Length]);

    protected override void Draw(DrawingContext context, Size size)
    {
        for (var i = 0; i < _items.Length; i++)
        {
            var (x, y, _) = _items[i];
            var text = _cached?[i] ??
                       Create(string.Create(CultureInfo.InvariantCulture,
                           $"{s_words[i % s_words.Length]} {Frame + i} {Math.Sin(Time + i):F3}"), i);
            var origin = new Point(x * size.Width - 100 + Math.Sin(Time + i) * 30, y * size.Height);
            context.DrawText(text, origin);
        }
    }
}
