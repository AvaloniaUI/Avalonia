using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Controls;

internal static class SpellCheckRangeFinder
{
    private static readonly ConditionalWeakTable<TextLayout, TextLineIndex> s_textLineIndexes = new();

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

        var textLayout = presenter.TextLayout;
        var textLines = textLayout.TextLines;

        if (textLines.Count == 0)
        {
            return false;
        }

        var viewport = GetViewportInPresenter(presenter, scrollViewer);

        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return false;
        }

        // Match the rendering origin for vertically aligned text.
        var textTop = presenter.GetTextVerticalOffset();
        var lineIndex = GetLineIndexFromVerticalOffset(
            textLayout,
            viewport.Top - textTop,
            out var lineTop);
        var currentY = textTop + lineTop;

        for (var i = lineIndex; i < textLines.Count; i++)
        {
            var textLine = textLines[i];
            var currentLineTop = currentY;
            var lineBottom = currentY + textLine.Height;
            currentY = lineBottom;

            if (MathUtilities.LessThanOrClose(lineBottom, viewport.Top))
            {
                continue;
            }

            if (MathUtilities.GreaterThanOrClose(currentLineTop, viewport.Bottom))
            {
                break;
            }

            AddVisibleRange(ranges, presenter, textLine, viewport.Left, viewport.Right, text);
        }

        // Reordered visual runs must become sorted, non-overlapping source ranges.
        SortAndMergeRanges(ranges);
        return ranges.Count > 0;
    }

    // Use binary search and the shared line-height index to avoid scanning from line zero.
    internal static int GetLineIndexFromCharacterIndex(
        TextLayout textLayout,
        int characterIndex,
        out double lineTop)
    {
        var textLineIndex = s_textLineIndexes.GetValue(textLayout, static layout => new TextLineIndex(layout));
        var index = textLineIndex.GetLineIndexFromCharacterIndex(characterIndex);
        lineTop = index < textLayout.TextLines.Count
            ? textLineIndex.GetLineTop(index)
            : 0;
        return index;
    }

    private static int GetLineIndexFromVerticalOffset(
        TextLayout textLayout,
        double offset,
        out double lineTop)
    {
        return s_textLineIndexes.GetValue(textLayout, static layout => new TextLineIndex(layout))
            .GetLineIndexFromVerticalOffset(offset, out lineTop);
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

            rangeStart = SpellCheckTokenization.Start(text, caret);
            rangeEnd = SpellCheckTokenization.End(text, rangeStart);
        }
        else
        {
            var tokenStart = SpellCheckTokenization.Start(text, rangeStart);
            var tokenEnd = SpellCheckTokenization.End(text, rangeStart);

            // Keyboard context menus can target a selection only when it contains one token.
            if (tokenEnd <= tokenStart || rangeEnd > tokenEnd)
            {
                return;
            }

            rangeStart = tokenStart;
            rangeEnd = tokenEnd;
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
        TextPresenter presenter,
        TextLine textLine,
        double viewportLeft,
        double viewportRight,
        string text)
    {
        var lineStart = presenter.GetTextPositionFromLayoutPosition(textLine.FirstTextSourceIndex);
        var lineEnd = presenter.GetTextPositionFromLayoutPosition(
            textLine.FirstTextSourceIndex + textLine.Length);
        var lineLeft = textLine.Start;
        var lineRight = lineLeft + textLine.WidthIncludingTrailingWhitespace;
        if (lineEnd <= lineStart ||
            MathUtilities.LessThanOrClose(viewportRight, lineLeft) ||
            MathUtilities.GreaterThanOrClose(viewportLeft, lineRight))
        {
            return;
        }

        if (viewportLeft > lineLeft || viewportRight < lineRight)
        {
            if (HasMixedTextDirections(textLine))
            {
                AddVisibleRunRanges(ranges, presenter, textLine, viewportLeft, viewportRight, text);
                return;
            }

            var start = presenter.GetTextPositionFromLayoutPosition(
                GetTextPosition(textLine.GetCharacterHitFromDistance(viewportLeft)));
            var end = presenter.GetTextPositionFromLayoutPosition(
                GetTextPosition(textLine.GetCharacterHitFromDistance(viewportRight)));

            if (start > end)
            {
                (start, end) = (end, start);
            }

            lineStart = Math.Max(lineStart, SpellCheckResultCache.PreviousScalarStart(text, start));
            lineEnd = Math.Min(lineEnd, SpellCheckResultCache.NextScalarEnd(text, end));

            if (lineEnd <= lineStart)
            {
                return;
            }
        }

        AddRange(ranges, text, lineStart, lineEnd);
    }

    private static void AddVisibleRunRanges(
        List<SpellCheckRange> ranges,
        TextPresenter presenter,
        TextLine textLine,
        double viewportLeft,
        double viewportRight,
        string text)
    {
        // Bidi text needs per-run clipping; viewport edges alone can include off-screen text.
        foreach (var bounds in textLine.GetTextBounds(textLine.FirstTextSourceIndex, textLine.Length))
        {
            foreach (var runBounds in bounds.TextRunBounds)
            {
                var rect = runBounds.Rectangle;
                if (rect.Width <= 0 || viewportRight <= rect.Left || viewportLeft >= rect.Right)
                    continue;

                var runStart = runBounds.TextSourceCharacterIndex;
                var runEnd = runStart + runBounds.Length;
                var start = runStart;
                var end = runEnd;

                if (runBounds.TextRun is ShapedTextRun shapedRun &&
                    (viewportLeft > rect.Left || viewportRight < rect.Right))
                {
                    var glyphRun = shapedRun.GlyphRun;
                    var offset = runStart - glyphRun.Metrics.FirstCluster;
                    start = offset + GetTextPosition(glyphRun.GetCharacterHitFromDistance(viewportLeft - rect.Left, out _));
                    end = offset + GetTextPosition(glyphRun.GetCharacterHitFromDistance(viewportRight - rect.Left, out _));
                    if (start > end)
                        (start, end) = (end, start);
                }

                runStart = presenter.GetTextPositionFromLayoutPosition(runStart);
                runEnd = presenter.GetTextPositionFromLayoutPosition(runEnd);
                start = presenter.GetTextPositionFromLayoutPosition(start);
                end = presenter.GetTextPositionFromLayoutPosition(end);
                start = Math.Max(runStart, SpellCheckResultCache.PreviousScalarStart(text, start));
                end = Math.Min(runEnd, SpellCheckResultCache.NextScalarEnd(text, end));
                AddRange(ranges, text, start, end);
            }
        }
    }

    private static void AddRange(List<SpellCheckRange> ranges, string text, int start, int end)
    {
        if (end > start)
            ranges.Add(new SpellCheckRange(start, end,
                SpellCheckTokenization.IsInsideToken(text, start), SpellCheckTokenization.IsInsideToken(text, end)));
    }

    private static void SortAndMergeRanges(List<SpellCheckRange> ranges)
    {
        if (ranges.Count < 2)
            return;

        ranges.Sort(static (x, y) => x.Start.CompareTo(y.Start));
        var count = 1;
        for (var i = 1; i < ranges.Count; i++)
        {
            var last = ranges[count - 1];
            var next = ranges[i];
            if (next.Start <= last.End)
            {
                ranges[count - 1] = new SpellCheckRange(last.Start, Math.Max(last.End, next.End),
                    last.StartIsInsideWord && (next.Start != last.Start || next.StartIsInsideWord),
                    next.End > last.End ? next.EndIsInsideWord :
                        last.EndIsInsideWord && (next.End != last.End || next.EndIsInsideWord));
            }
            else
                ranges[count++] = next;
        }
        ranges.RemoveRange(count, ranges.Count - count);
    }

    private static bool HasMixedTextDirections(TextLine textLine)
    {
        bool? isLeftToRight = null;

        foreach (var textRun in textLine.TextRuns)
        {
            if (textRun is not ShapedTextRun shapedTextRun)
            {
                continue;
            }

            var currentIsLeftToRight = (shapedTextRun.BidiLevel & 1) == 0;

            if (isLeftToRight.HasValue && isLeftToRight.Value != currentIsLeftToRight)
            {
                return true;
            }

            isLeftToRight = currentIsLeftToRight;
        }

        return false;
    }

    private static int GetTextPosition(CharacterHit characterHit)
    {
        return characterHit.FirstCharacterIndex + characterHit.TrailingLength;
    }

    // Cache line tops as scrolling reaches them; use binary search when revisiting them.
    private sealed class TextLineIndex
    {
        private readonly IReadOnlyList<TextLine> _lines;
        private readonly List<double> _lineTops = new() { 0 };

        public TextLineIndex(TextLayout textLayout)
        {
            _lines = textLayout.TextLines;
        }

        public double GetLineTop(int lineIndex)
        {
            lineIndex = Math.Clamp(lineIndex, 0, _lines.Count);

            while (_lineTops.Count <= lineIndex)
            {
                var previous = _lineTops.Count - 1;
                _lineTops.Add(_lineTops[previous] + _lines[previous].Height);
            }

            return _lineTops[lineIndex];
        }

        public int GetLineIndexFromVerticalOffset(double offset, out double lineTop)
        {
            if (_lines.Count == 0)
            {
                lineTop = 0;
                return 0;
            }

            if (offset <= 0)
            {
                lineTop = 0;
                return 0;
            }

            // Include the next line top to cover the viewport's lower edge.
            while (_lineTops.Count <= _lines.Count)
            {
                var line = _lineTops.Count - 1;
                var bottom = _lineTops[line] + _lines[line].Height;

                if (!MathUtilities.LessThanOrClose(bottom, offset))
                {
                    break;
                }

                _lineTops.Add(bottom);
            }

            var low = 0;
            var high = Math.Min(_lineTops.Count - 1, _lines.Count);

            // Find the greatest cached line top at or before the requested offset.
            while (low < high)
            {
                var middle = low + (high - low + 1) / 2;

                if (MathUtilities.LessThanOrClose(_lineTops[middle], offset))
                {
                    low = middle;
                }
                else
                {
                    high = middle - 1;
                }
            }

            var result = low;

            while (result < _lines.Count &&
                   MathUtilities.LessThanOrClose(GetLineTop(result) + _lines[result].Height, offset))
            {
                result++;
            }

            lineTop = GetLineTop(result);
            return result;
        }

        public int GetLineIndexFromCharacterIndex(int characterIndex)
        {
            if (_lines.Count == 0)
            {
                return 0;
            }

            var low = 0;
            var high = _lines.Count - 1;

            while (low < high)
            {
                var middle = low + (high - low) / 2;
                var line = _lines[middle];

                if (line.FirstTextSourceIndex + line.Length <= characterIndex)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }
    }
}
