// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Media;

using SkiaSharp;

namespace Avalonia.Skia
{
    public class SKTextLayout
    {
        private readonly string _text;

        private readonly SKTypeface _typeface;

        private readonly float _fontSize;

        private readonly TextAlignment _textAlignment;

        private readonly TextWrapping _textWrapping;

        private readonly Size _constraint;

        private readonly SKPaint _paint;

        private readonly List<SKTextLine> _textLines;

        private List<Rect> _rectangles;

        public SKTextLayout(
            string text,
            SKTypeface typeface,
            float fontSize,
            TextAlignment textAlignment,
            TextWrapping textWrapping,
            Size constraint)
        {
            _text = text;
            _typeface = typeface;
            _fontSize = fontSize;
            _textAlignment = textAlignment;
            _textWrapping = textWrapping;
            _constraint = constraint;
            _paint = CreatePaint(_typeface, _fontSize);
            _textLines = CreateTextLines();
        }

        public IReadOnlyList<SKTextLine> TextLines => _textLines;

        public Size Size { get; private set; }

        public void ApplyTextSpan(FormattedTextStyleSpan span)
        {
            var currentLength = 0;
            var remainingLength = span.Length;

            for (var lineIndex = 0; lineIndex < _textLines.Count; lineIndex++)
            {
                var currentTextLine = _textLines[lineIndex];

                if (currentTextLine.Length == 0)
                {
                    continue;
                }

                if (currentTextLine.StartingIndex + currentTextLine.Length < span.StartIndex)
                {
                    currentLength += currentTextLine.Length;

                    continue;
                }

                for (var runIndex = 0; runIndex < currentTextLine.TextRuns.Count; runIndex++)
                {
                    var needsUpdate = false;
                    var currentTextRun = currentTextLine.TextRuns[runIndex];

                    if (currentLength + currentTextRun.Text.Length < span.StartIndex)
                    {
                        currentLength += currentTextRun.Text.Length;

                        continue;
                    }

                    var textRuns = new List<SKTextRun>(currentTextLine.TextRuns);

                    var splitLength = Math.Min(currentTextRun.Text.Length, remainingLength);

                    if (splitLength == currentTextRun.Text.Length)
                    {
                        // Apply to the whole run 
                        textRuns.RemoveAt(runIndex);

                        var updatedTextRun = ApplyTextSpan(span, currentTextRun, out needsUpdate);

                        textRuns.Insert(runIndex, updatedTextRun);

                        remainingLength -= currentTextRun.Text.Length;
                    }
                    else
                    {
                        if (currentLength == span.StartIndex)
                        {
                            // Apply at start of the run 
                            var start = SplitTextRun(currentTextRun, 0, splitLength);

                            textRuns.RemoveAt(runIndex);

                            var updatedTextRun = ApplyTextSpan(span, start.FirstTextRun, out needsUpdate);

                            textRuns.Insert(runIndex, updatedTextRun);

                            remainingLength -= start.FirstTextRun.Text.Length;

                            runIndex++;

                            textRuns.Insert(runIndex, start.SecondTextRun);
                        }
                        else
                        {
                            splitLength = Math.Max(0, span.StartIndex - currentLength);

                            var start = SplitTextRun(currentTextRun, 0, splitLength);

                            if (splitLength + remainingLength == currentTextRun.Text.Length)
                            {
                                // Apply at the end of the run                         
                                textRuns.RemoveAt(runIndex);

                                textRuns.Insert(runIndex, start.FirstTextRun);

                                runIndex++;

                                var updatedTextRun = ApplyTextSpan(span, start.SecondTextRun, out needsUpdate);

                                textRuns.Insert(runIndex, updatedTextRun);

                                remainingLength -= start.SecondTextRun.Text.Length;
                            }
                            else
                            {
                                // Apply in between the run
                                var end = SplitTextRun(start.SecondTextRun, 0, remainingLength);

                                textRuns.RemoveAt(runIndex);

                                textRuns.Insert(runIndex, start.FirstTextRun);

                                runIndex++;

                                var updatedTextRun = ApplyTextSpan(span, end.FirstTextRun, out needsUpdate);

                                textRuns.Insert(runIndex, updatedTextRun);

                                runIndex++;

                                textRuns.Insert(runIndex, end.SecondTextRun);

                                remainingLength = 0;
                            }
                        }
                    }

                    _textLines.RemoveAt(lineIndex);

                    if (needsUpdate)
                    {
                        // ToDo: We need to update the length etc if we apply a different TextFormat to a run of the line
                    }
                    else
                    {
                        currentTextLine = new SKTextLine(
                            currentTextLine.StartingIndex,
                            currentTextLine.Length,
                            textRuns,
                            currentTextLine.LineMetrics);

                        _textLines.Insert(lineIndex, currentTextLine);
                    }

                    if (remainingLength == 0)
                    {
                        return;
                    }

                    currentLength += currentTextRun.Text.Length;
                }
            }
        }

        public void Draw(DrawingContextImpl context, IBrush foreground, SKCanvas canvas, SKPoint origin)
        {
            using (var foregroundWrapper = context.CreatePaint(foreground, Size))
            {
                var currentX = origin.X;
                var currentY = origin.Y;

                foreach (var textLine in TextLines)
                {
                    var offsetX = (float)GetTextLineOffsetX(_textAlignment, textLine.LineMetrics.Size.Width);
                    var lineX = currentX + offsetX;
                    var lineY = currentY + textLine.LineMetrics.BaselineOrigin.Y;

                    foreach (var textRun in textLine.TextRuns)
                    {
                        InitializePaintForTextRun(_paint, context, textLine, textRun, foregroundWrapper);
                        canvas.DrawText(textRun.Text, lineX, lineY, _paint);
                        lineX += textRun.Width;
                    }

                    currentY += textLine.LineMetrics.Size.Height;
                }
            }
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            var rectangles = GetRectangles();

            var pointY = (float)point.Y;

            var currentY = 0.0f;

            var isTrailing = point.X > Size.Width || point.Y > Size.Height;

            foreach (var textLine in TextLines)
            {
                if (pointY <= currentY + textLine.LineMetrics.Size.Height)
                {
                    for (var glyphIndex = textLine.StartingIndex; glyphIndex < rectangles.Count; glyphIndex++)
                    {
                        var glyphRectangle = rectangles[glyphIndex];

                        if (glyphRectangle.Contains(point))
                        {
                            return new TextHitTestResult
                            {
                                IsInside = true,
                                TextPosition = glyphIndex,
                                IsTrailing = point.X - glyphRectangle.X > glyphRectangle.Width / 2
                            };
                        }
                    }

                    var offset = 0;

                    if (point.X >= (rectangles[textLine.StartingIndex].X + textLine.LineMetrics.Size.Width) / 2 && textLine.Length > 0)
                    {
                        offset = textLine.Length - 1;
                    }

                    if (offset > 2 && IsBreakChar(_text[offset - 1]))
                    {
                        offset--;
                    }

                    return new TextHitTestResult
                    {
                        IsInside = false,
                        TextPosition = textLine.StartingIndex + offset,
                        IsTrailing = _text.Length == textLine.StartingIndex + offset + 1
                    };
                }

                currentY += textLine.LineMetrics.Size.Height;
            }

            return new TextHitTestResult
            {
                IsInside = false,
                IsTrailing = isTrailing,
                TextPosition = isTrailing ? _text.Length - 1 : 0
            };
        }

        public Rect HitTestTextPosition(int index)
        {
            var rectangles = GetRectangles();

            if (index < 0 || index >= rectangles.Count)
            {
                var r = rectangles.LastOrDefault();

                return new Rect(r.X + r.Width, r.Y, 0, r.Height);
            }

            if (index == rectangles.Count)
            {
                var lr = rectangles[rectangles.Count - 1];

                return new Rect(new Point(lr.X + lr.Width, lr.Y), rectangles[index - 1].Size);
            }

            return rectangles[index];
        }

        public IEnumerable<Rect> HitTestTextRange(int index, int length)
        {
            var result = new List<Rect>();

            var rectangles = GetRectangles();

            var lastIndex = index + length - 1;

            var currentY = 0.0f;

            foreach (var textLine in TextLines)
            {
                if (textLine.StartingIndex + textLine.Length > index && lastIndex >= textLine.StartingIndex)
                {
                    var lineEndIndex = textLine.StartingIndex + (textLine.Length > 0 ? textLine.Length - 1 : 0);

                    var left = rectangles[textLine.StartingIndex > index ? textLine.StartingIndex : index].X;

                    var right = rectangles[lineEndIndex > lastIndex ? lastIndex : lineEndIndex].Right;

                    result.Add(new Rect(left, currentY, right - left, textLine.LineMetrics.Size.Height));
                }

                currentY += textLine.LineMetrics.Size.Height;
            }

            return result;
        }

        private static SKPaint CreatePaint(SKTypeface typeface, float fontSize)
        {
            return new SKPaint
            {
                IsAntialias = true,
                /*Bug: Transparency issue with LcdRenderText = true,*/
                IsStroke = false,
                TextEncoding = SKTextEncoding.Utf32,
                Typeface = typeface,
                TextSize = fontSize
            };
        }

        private static void InitializePaintForTextRun(
            SKPaint paint,
            DrawingContextImpl context,
            SKTextLine textLine,
            SKTextRun textRun,
            DrawingContextImpl.PaintWrapper foregroundWrapper)
        {
            paint.Typeface = textRun.TextFormat.Typeface;

            paint.TextSize = textRun.TextFormat.FontSize;

            if (textRun.DrawingEffect == null)
            {
                foregroundWrapper.ApplyTo(paint);
            }
            else
            {
                using (var effectWrapper = context.CreatePaint(
                    textRun.DrawingEffect,
                    new Size(textRun.Width, textLine.LineMetrics.Size.Height)))
                {
                    effectWrapper.ApplyTo(paint);
                }
            }
        }

        private static SKTextRun ApplyTextSpan(FormattedTextStyleSpan span, SKTextRun textRun, out bool needsUpdate)
        {
            // ToDo: We need to make sure to update all measurements if the TextFormat etc changes.
            needsUpdate = false;

            return new SKTextRun(
                textRun.Text,
                textRun.TextFormat,
                textRun.FontMetrics,
                textRun.Width,
                span.ForegroundBrush);
        }

        private static SKTextLineMetrics CreateTextLineMetrics(IEnumerable<SKTextRun> textRuns)
        {
            var width = 0.0f;

            var ascent = 0.0f;

            var descent = 0.0f;

            var leading = 0.0f;

            foreach (var textRun in textRuns)
            {
                width += textRun.Width;

                if (ascent > textRun.FontMetrics.Ascent)
                {
                    ascent = textRun.FontMetrics.Ascent;
                }

                if (descent < textRun.FontMetrics.Descent)
                {
                    descent = textRun.FontMetrics.Descent;
                }

                if (leading < textRun.FontMetrics.Leading)
                {
                    leading = textRun.FontMetrics.Leading;
                }
            }

            return new SKTextLineMetrics(width, ascent, descent, leading);
        }

        private static bool IsBreakChar(char c)
        {
            switch (c)
            {
                case '\r':
                    return true;
                case '\n':
                    return true;
                default:
                    return false;
            }
        }

        private List<Rect> GetRectangles()
        {
            if (_rectangles == null || _rectangles.Count != _text.Length)
            {
                _rectangles = CreateRectangles();
            }

            return _rectangles;
        }

        private List<Rect> CreateRectangles()
        {
            var rectangles = new List<Rect>();

            var currentY = 0.0f;

            foreach (var currentLine in _textLines)
            {
                var currentX = GetTextLineOffsetX(_textAlignment, currentLine.LineMetrics.Size.Width);

                foreach (var textRun in currentLine.TextRuns)
                {
                    if (textRun.Text.Length == 0)
                    {
                        rectangles.Add(new Rect(currentX, currentY, 0, currentLine.LineMetrics.Size.Height));

                        continue;
                    }

                    foreach (var c in textRun.Text)
                    {
                        if (IsBreakChar(c))
                        {
                            rectangles.Add(new Rect(
                                currentX,
                                currentY,
                                0.0f,
                                currentLine.LineMetrics.Size.Height));
                        }
                        else
                        {
                            var width = _paint.MeasureText(c.ToString());

                            rectangles.Add(new Rect(
                                currentX,
                                currentY,
                                width,
                                currentLine.LineMetrics.Size.Height));

                            currentX += width;
                        }
                    }
                }

                currentY += currentLine.LineMetrics.Size.Height;
            }

            return rectangles;
        }

        private double GetTextLineOffsetX(TextAlignment textAlignment, double width)
        {
            var availableWidth = _constraint.Width > 0 && !double.IsPositiveInfinity(_constraint.Width)
                                     ? _constraint.Width
                                     : Size.Width;

            switch (textAlignment)
            {
                case TextAlignment.Left:
                    return 0.0d;
                case TextAlignment.Center:
                    return (availableWidth - width) / 2;
                case TextAlignment.Right:
                    return availableWidth - width;
                default:
                    throw new ArgumentOutOfRangeException(nameof(textAlignment), textAlignment, null);
            }
        }

        private SplitTextRunResult SplitTextRun(SKTextRun textRun, int startingIndex, int length)
        {
            var firstTextRun = CreateTextRun(textRun.Text.Substring(startingIndex, length), textRun.TextFormat);

            var secondTextRun = CreateTextRun(
                textRun.Text.Substring(length, textRun.Text.Length - length),
                textRun.TextFormat);

            return new SplitTextRunResult(firstTextRun, secondTextRun);
        }

        private SKTextRun CreateTextRun(string text, SKTextFormat textFormat)
        {
            _paint.Typeface = textFormat.Typeface;

            _paint.TextSize = textFormat.FontSize;

            var fontMetrics = _paint.FontMetrics;

            var width = _paint.MeasureText(text);

            return new SKTextRun(text, textFormat, fontMetrics, width);
        }

        private List<SKTextLine> CreateTextLines()
        {
            var sizeX = 0.0f;
            var sizeY = 0.0f;

            var textLines = new List<SKTextLine>();

            var currentPosition = 0;

            if (_text.Length != 0)
            {
                for (var index = 0; index < _text.Length; index++)
                {
                    var c = _text[index];

                    switch (c)
                    {
                        case '\r':
                            {
                                if (_text[index + 1] == '\n')
                                {
                                    index++;
                                }

                                var breakLines = PerformLineBreak(_text, currentPosition, index - currentPosition + 1);

                                textLines.AddRange(breakLines);

                                currentPosition = index + 1;
                                break;
                            }

                        case '\n':
                            {
                                if (_text[index + 1] == '\r')
                                {
                                    index++;
                                }

                                var breakLines = PerformLineBreak(_text, currentPosition, index - currentPosition + 1);

                                textLines.AddRange(breakLines);

                                currentPosition = index + 1;
                                break;
                            }
                    }
                }
            }
            else
            {
                textLines.Add(CreateTextLine(string.Empty, 0, 0));
            }

            if (currentPosition < _text.Length)
            {
                var breakLines = PerformLineBreak(_text, currentPosition, _text.Length - currentPosition);

                textLines.AddRange(breakLines);
            }

            foreach (var textLine in textLines)
            {
                if (sizeX < textLine.LineMetrics.Size.Width)
                {
                    sizeX = textLine.LineMetrics.Size.Width;
                }

                sizeY += textLine.LineMetrics.Size.Height;
            }

            Size = new Size(sizeX, sizeY);

            return textLines;
        }

        private IEnumerable<SKTextLine> PerformLineBreak(string text, int startingIndex, int length)
        {
            var textLines = new List<SKTextLine>();

            var textLine = CreateTextLine(text, startingIndex, length);

            if (textLine.LineMetrics.Size.Width > _constraint.Width && _textWrapping == TextWrapping.Wrap)
            {
                var availableLength = (float)_constraint.Width;
                var currentWidth = 0.0f;
                var runIndex = 0;

                while (runIndex < textLine.TextRuns.Count)
                {
                    var textRun = textLine.TextRuns[runIndex];

                    currentWidth += textRun.Width;

                    if (currentWidth > availableLength)
                    {
                        var remainingTextRuns = new List<SKTextRun>();

                        var measuredLength = (int)_paint.BreakText(textRun.Text, availableLength);

                        for (var i = measuredLength; i > 0; i--)
                        {
                            var c = textRun.Text[i];

                            if (char.IsWhiteSpace(c) || c == '\u200B')
                            {
                                measuredLength = ++i;
                                break;
                            }
                        }

                        var splitResult = SplitTextRun(textLine.TextRuns[runIndex], 0, measuredLength);

                        var textRuns = new List<SKTextRun>(textLine.TextRuns);

                        textRuns.RemoveAt(runIndex);

                        textRuns.Insert(runIndex, splitResult.FirstTextRun);

                        var textLineMetrics = CreateTextLineMetrics(textRuns);

                        textLine = new SKTextLine(textLine.StartingIndex, measuredLength, textRuns, textLineMetrics);

                        textLines.Add(textLine);

                        remainingTextRuns.Add(splitResult.SecondTextRun);

                        var remainingLength = splitResult.SecondTextRun.Text.Length;

                        for (var i = runIndex + 1; i < textLine.TextRuns.Count; i++)
                        {
                            var currentRun = textLine.TextRuns[i];

                            remainingLength += currentRun.Text.Length;

                            remainingTextRuns.Add(currentRun);
                        }

                        textLineMetrics = CreateTextLineMetrics(remainingTextRuns);

                        textLine = new SKTextLine(
                            textLine.StartingIndex + measuredLength,
                            remainingLength,
                            remainingTextRuns,
                            textLineMetrics);

                        availableLength = (float)_constraint.Width;

                        currentWidth = 0.0f;

                        runIndex = 0;

                        continue;
                    }

                    textLines.Add(textLine);

                    availableLength -= textRun.Width;

                    runIndex++;
                }
            }
            else
            {
                textLines.Add(textLine);
            }

            return textLines;
        }

        private SKTextLine CreateTextLine(string text, int startingIndex, int length)
        {
            if (length == 0)
            {
                _paint.Typeface = _typeface;

                _paint.TextSize = _fontSize;

                var textRuns = new List<SKTextRun>
                               {
                                   new SKTextRun(
                                       string.Empty,
                                       new SKTextFormat(_typeface, _fontSize),
                                       _paint.FontMetrics,
                                       0.0f)
                               };

                var textLineMetrics = CreateTextLineMetrics(textRuns);

                return new SKTextLine(startingIndex, 0, textRuns, textLineMetrics);
            }
            else
            {
                var textRuns = CreateTextRuns(text, startingIndex, length, out var lineMetrics);

                return new SKTextLine(startingIndex, length, textRuns, lineMetrics);
            }
        }

        private List<SKTextRun> CreateTextRuns(
            string text,
            int startingIndex,
            int length,
            out SKTextLineMetrics textLineMetrics)
        {
            var textRuns = new List<SKTextRun>();
            var currentPosition = 0;

            var runText = text.Substring(startingIndex, length);

            while (currentPosition < length)
            {
                var glyphCount = _typeface.CountGlyphs(runText);

                while (glyphCount < runText.Length)
                {
                    if (IsBreakChar(runText[glyphCount]))
                    {
                        glyphCount++;
                    }
                }

                var typeface = _typeface;

                if (glyphCount == 0)
                {
                    typeface = SKFontManager.Default.MatchCharacter(runText[currentPosition]);

                    glyphCount = typeface.CountGlyphs(runText);
                }

                if (glyphCount != length)
                {
                    runText = runText.Substring(currentPosition, glyphCount);
                }

                var currentRun = CreateTextRun(runText, new SKTextFormat(typeface, _fontSize));

                textRuns.Add(currentRun);

                currentPosition += glyphCount;
            }

            textLineMetrics = CreateTextLineMetrics(textRuns);

            return textRuns;
        }

        private class SplitTextRunResult
        {
            public SplitTextRunResult(SKTextRun firstTextRun, SKTextRun secondTextRun)
            {
                FirstTextRun = firstTextRun;

                SecondTextRun = secondTextRun;
            }

            public SKTextRun FirstTextRun { get; }

            public SKTextRun SecondTextRun { get; }
        }
    }
}
