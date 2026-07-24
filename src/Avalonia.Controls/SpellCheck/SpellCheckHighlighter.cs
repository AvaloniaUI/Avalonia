using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace Avalonia.Controls;

internal sealed class SpellCheckHighlighter
{
    private TextRunProperties? _misspellingTextRunProperties;

    public void Refresh()
    {
        _misspellingTextRunProperties = null;
    }

    public void Clear(TextPresenter? presenter)
    {
        Refresh();
        presenter?.SetTextStyleOverrides(null);
    }

    public void Apply(
        TextPresenter presenter,
        IReadOnlyList<SpellCheckResult> results,
        List<SpellCheckRange> visibleRanges)
    {
        List<ValueSpan<TextRunProperties>>? spans = null;

        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];

            if (!IntersectsVisibleRange(result, visibleRanges))
            {
                continue;
            }

            _misspellingTextRunProperties ??= CreateMisspellingTextRunProperties(presenter);
            spans ??= new List<ValueSpan<TextRunProperties>>();
            spans.Add(new ValueSpan<TextRunProperties>(result.Start, result.Length, _misspellingTextRunProperties));
        }

        presenter.SetTextStyleOverrides(spans);
    }

    private static TextRunProperties CreateMisspellingTextRunProperties(TextPresenter presenter)
    {
        var typeface = new Typeface(
            presenter.FontFamily,
            presenter.FontStyle,
            presenter.FontWeight,
            presenter.FontStretch);

        var decorations = new TextDecorationCollection
        {
            new TextDecoration
            {
                Location = TextDecorationLocation.Underline,
                Stroke = Brushes.Red,
                StrokeDashArray = new AvaloniaList<double> { 1, 2 },
                StrokeLineCap = PenLineCap.Round
            }
        };

        return new GenericTextRunProperties(
            typeface,
            presenter.FontSize,
            decorations,
            presenter.Foreground,
            fontFeatures: presenter.FontFeatures);
    }

    private static bool IntersectsVisibleRange(
        SpellCheckResult result,
        List<SpellCheckRange> ranges)
    {
        var resultEnd = result.Start + result.Length;

        for (var i = 0; i < ranges.Count; i++)
        {
            var range = ranges[i];

            if (resultEnd <= range.Start)
            {
                continue;
            }

            if (result.Start < range.End)
            {
                return true;
            }
        }

        return false;
    }
}
