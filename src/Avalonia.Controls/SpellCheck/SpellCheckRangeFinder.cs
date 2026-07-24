using System;
using System.Collections.Generic;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Utils;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Controls;

internal static class SpellCheckRangeFinder
{
    public static bool TryGetVisibleRanges(
        TextPresenter presenter,
        ScrollViewer? scrollViewer,
        string? text,
        List<SpellCheckRange> ranges)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var textLines = presenter.TextLayout.TextLines;

        if (textLines.Count == 0)
        {
            return false;
        }

        var viewport = GetViewportInPresenter(presenter, scrollViewer);
        var currentY = 0.0;

        for (var i = 0; i < textLines.Count; i++)
        {
            var textLine = textLines[i];
            var lineTop = currentY;
            var lineBottom = currentY + textLine.Height;
            currentY = lineBottom;

            if (lineBottom < viewport.Top)
            {
                continue;
            }

            if (lineTop > viewport.Bottom)
            {
                break;
            }

            AddVisibleRange(ranges, textLine, viewport.Left, viewport.Right, text);
        }

        return ranges.Count > 0;
    }

    public static void AddContextRange(
        List<SpellCheckRange> ranges,
        string text,
        int caretIndex,
        int selectionStart,
        int selectionEnd)
    {
        var textLength = text.Length;
        var rangeStart = MathUtilities.Clamp(Math.Min(selectionStart, selectionEnd), 0, textLength);
        var rangeEnd = MathUtilities.Clamp(Math.Max(selectionStart, selectionEnd), 0, textLength);

        if (rangeStart == rangeEnd)
        {
            var caret = MathUtilities.Clamp(caretIndex, 0, textLength);

            rangeStart = StringUtils.PreviousWord(text, caret);
            rangeEnd = StringUtils.NextWord(text, rangeStart);
        }
        else
        {
            rangeStart = StringUtils.PreviousWord(text, rangeStart);
            rangeEnd = StringUtils.NextWord(text, rangeEnd);
        }

        rangeStart = MathUtilities.Clamp(rangeStart, 0, textLength);
        rangeEnd = MathUtilities.Clamp(rangeEnd, rangeStart, textLength);

        if (rangeEnd > rangeStart)
        {
            ranges.Add(new SpellCheckRange(rangeStart, rangeEnd));
        }
    }

    private static Rect GetViewportInPresenter(TextPresenter presenter, ScrollViewer? scrollViewer)
    {
        if (scrollViewer is not null &&
            scrollViewer.Viewport.Width > 0 &&
            scrollViewer.Viewport.Height > 0 &&
            scrollViewer.TranslatePoint(default, presenter) is { } topLeft &&
            scrollViewer.TranslatePoint(new Point(scrollViewer.Viewport.Width, scrollViewer.Viewport.Height), presenter) is { } bottomRight)
        {
            var x = Math.Min(topLeft.X, bottomRight.X);
            var y = Math.Min(topLeft.Y, bottomRight.Y);

            return new Rect(x, y, Math.Abs(bottomRight.X - topLeft.X), Math.Abs(bottomRight.Y - topLeft.Y));
        }

        return new Rect(presenter.Bounds.Size);
    }

    private static void AddVisibleRange(
        List<SpellCheckRange> ranges,
        TextLine textLine,
        double viewportLeft,
        double viewportRight,
        string text)
    {
        var textLength = text.Length;
        var lineStart = textLine.FirstTextSourceIndex;
        var lineEnd = Math.Min(textLength, textLine.FirstTextSourceIndex + textLine.Length);
        var startIsInsideWord = false;
        var endIsInsideWord = false;

        if (lineEnd <= lineStart)
        {
            return;
        }

        if (viewportLeft > 0 || viewportRight < textLine.WidthIncludingTrailingWhitespace)
        {
            var start = GetTextPosition(textLine.GetCharacterHitFromDistance(viewportLeft));
            var end = GetTextPosition(textLine.GetCharacterHitFromDistance(viewportRight));

            if (start > end)
            {
                (start, end) = (end, start);
            }

            startIsInsideWord = IsInsideWord(text, start);
            endIsInsideWord = IsInsideWord(text, end);
            lineStart = Math.Max(lineStart, start - 1);
            lineEnd = Math.Min(lineEnd, end + 1);

            if (lineEnd <= lineStart)
            {
                return;
            }
        }

        if (ranges.Count > 0)
        {
            var last = ranges[ranges.Count - 1];

            if (lineStart <= last.End)
            {
                if (lineEnd > last.End)
                {
                    ranges[ranges.Count - 1] = new SpellCheckRange(
                        last.Start,
                        lineEnd,
                        last.StartIsInsideWord,
                        endIsInsideWord);
                }

                return;
            }
        }

        ranges.Add(new SpellCheckRange(lineStart, lineEnd, startIsInsideWord, endIsInsideWord));
    }

    private static bool IsInsideWord(string text, int index)
    {
        return index > 0 &&
            index < text.Length &&
            char.IsLetterOrDigit(text[index - 1]) &&
            char.IsLetterOrDigit(text[index]);
    }

    private static int GetTextPosition(CharacterHit characterHit)
    {
        return characterHit.FirstCharacterIndex + characterHit.TrailingLength;
    }
}
