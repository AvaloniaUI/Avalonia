// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

using Avalonia.Media;

using SkiaSharp;

namespace Avalonia.Skia
{
    using System;

    public class SKTextLayout
    {
        private readonly string _text;

        private readonly SKTypeface _typeface;

        private readonly float _fontSize;

        private readonly TextAlignment _textAlignment;

        private readonly TextWrapping _textWrapping;

        private readonly Size _constraint;

        private readonly SKPaint _paint;

        public SKTextLayout(string text, SKTypeface typeface, float fontSize, TextAlignment textAlignment, TextWrapping textWrapping, Size constraint)
        {
            _text = text;
            _typeface = typeface;
            _fontSize = fontSize;
            _textAlignment = textAlignment;
            _textWrapping = textWrapping;
            _constraint = constraint;
            _paint = CreatePaint(_typeface, _fontSize);
            TextLines = CreateTextLines();
        }

        public IReadOnlyList<SKTextLine> TextLines { get; }

        public Size Size { get; private set; }

        public void ApplyTextSpan(FormattedTextStyleSpan span)
        {
            var effectedStartingIndex = 0;
            var availableLength = span.Length;

            for (var i = 0; i < TextLines.Count; i++)
            {
                var textLine = TextLines[i];

                if (textLine.StartingIndex < span.StartIndex)
                {
                    continue;
                }

                effectedStartingIndex = i;

                break;
            }

            for (var lineIndex = effectedStartingIndex; lineIndex < TextLines.Count; lineIndex++)
            {
                var textLine = TextLines[lineIndex];

                if (availableLength >= textLine.Length)
                {
                    availableLength -= textLine.Length;

                    foreach (var textRun in textLine.TextRuns)
                    {
                        ApplyTextSpan(span, textRun);
                    }
                }
                else
                {
                    for (var runIndex = 0; runIndex < textLine.TextRuns.Count; runIndex++)
                    {
                        var textRun = textLine.TextRuns[runIndex];

                        if (availableLength < textRun.Text.Length)
                        {
                            textLine.RemoveTextRun(runIndex);

                            var (firstTextRun, secondTextRun) = SplitTextRun(textRun, 0, availableLength);

                            ApplyTextSpan(span, firstTextRun);

                            textLine.InsertTextRun(runIndex, firstTextRun);

                            textLine.InsertTextRun(runIndex + 1, secondTextRun);

                            break;
                        }

                        availableLength -= textRun.Text.Length;

                        ApplyTextSpan(span, textRun);
                    }
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
                    var lineX = currentX;
                    var lineY = currentY + textLine.LineMetrics.BaselineOrigin.Y;

                    foreach (var textRun in textLine.TextRuns)
                    {
                        InitializePaintForTextRun(context, textLine, textRun, foregroundWrapper);
                        canvas.DrawText(textRun.Text, lineX, lineY, _paint);
                        lineX += textRun.Width;
                    }

                    currentY += textLine.LineMetrics.Size.Height;
                }
            }
        }

        // ToDo: Need to figure out how to calculate the text position properly.
        private static float GetTextAlignmentOffset(TextAlignment textAlignment, float width, float availableWidth)
        {
            switch (textAlignment)
            {
                case TextAlignment.Left:
                    return 0.0f;
                case TextAlignment.Center:
                    return availableWidth - (width / 2);
                case TextAlignment.Right:
                    return availableWidth - width;
                default:
                    throw new ArgumentOutOfRangeException(nameof(textAlignment), textAlignment, null);
            }
        }

        private static void ApplyTextSpan(FormattedTextStyleSpan span, SKTextRun textRun)
        {
            textRun.SetDrawingEffect(span.ForegroundBrush);
        }

        private static SKPaint CreatePaint(SKTypeface typeface, float fontSize)
        {
            return new SKPaint
            {
                IsAntialias = true,
                LcdRenderText = true,
                IsStroke = false,
                TextEncoding = SKTextEncoding.Utf32,
                Typeface = typeface,
                TextSize = fontSize
            };
        }

        private (SKTextRun firstTextRun, SKTextRun secondTextRun) SplitTextRun(
            SKTextRun textRun,
            int startingIndex,
            int length)
        {
            var firstTextRun = CreateTextRun(
                textRun.Text.Substring(startingIndex, length),
                textRun.TextFormat);

            var secondTextRun = CreateTextRun(
                textRun.Text.Substring(length, textRun.Text.Length - length),
                textRun.TextFormat);

            return (firstTextRun, secondTextRun);
        }

        private void InitializePaintForTextRun(
            DrawingContextImpl context,
            SKTextLine textLine,
            SKTextRun textRun,
            DrawingContextImpl.PaintWrapper foregroundWrapper)
        {
            _paint.Typeface = textRun.TextFormat.Typeface;

            _paint.TextSize = textRun.TextFormat.FontSize;

            if (textRun.DrawingEffect == null)
            {
                foregroundWrapper.ApplyTo(_paint);
            }
            else
            {
                using (var effectWrapper = context.CreatePaint(
                    textRun.DrawingEffect,
                    new Size(textRun.Width, textLine.LineMetrics.Size.Height)))
                {
                    effectWrapper.ApplyTo(_paint);
                }
            }
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

            for (var index = 0; index < _text.Length; index++)
            {
                var c = _text[index];

                if (c == '\r')
                {
                    if (_text[index + 1] == '\n')
                    {
                        index++;
                    }

                    var breakLines = PerformLineBreak(_text, currentPosition, index - currentPosition + 1);

                    textLines.AddRange(breakLines);

                    currentPosition = index + 1;
                }
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

                sizeY += textLine.LineMetrics.Size.Width;
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

                        var (firstTextRun, secondTextRun) = SplitTextRun(
                            textLine.TextRuns[runIndex],
                            0,
                            measuredLength);

                        textLine.RemoveTextRun(runIndex);

                        textLine.InsertTextRun(runIndex, firstTextRun);

                        textLines.Add(textLine);

                        remainingTextRuns.Add(secondTextRun);

                        var width = secondTextRun.Width;

                        var ascent = secondTextRun.FontMetrics.Ascent;

                        var descent = secondTextRun.FontMetrics.Descent;

                        var leading = secondTextRun.FontMetrics.Leading;

                        for (var i = runIndex + 1; i < textLine.TextRuns.Count; i++)
                        {
                            var currentRun = textLine.TextRuns[i];

                            width += currentRun.Width;

                            if (ascent > currentRun.FontMetrics.Ascent)
                            {
                                ascent = currentRun.FontMetrics.Ascent;
                            }

                            if (descent < currentRun.FontMetrics.Descent)
                            {
                                descent = currentRun.FontMetrics.Descent;
                            }

                            if (leading < currentRun.FontMetrics.Leading)
                            {
                                leading = currentRun.FontMetrics.Leading;
                            }

                            remainingTextRuns.Add(currentRun);
                        }

                        var height = descent - ascent + leading;

                        var lineMetrics = new SKTextLineMetrics(width, height, new SKPoint(0, -ascent));

                        textLine = new SKTextLine(textLine.StartingIndex + measuredLength, textLine.Length - measuredLength, remainingTextRuns, lineMetrics);

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
            var textRuns = CreateTextRuns(text, startingIndex, length, out var lineMetrics);

            return new SKTextLine(startingIndex, length, textRuns, lineMetrics);
        }

        private List<SKTextRun> CreateTextRuns(string text, int startingIndex, int length, out SKTextLineMetrics lineMetrics)
        {
            var width = 0.0f;

            var ascent = 0.0f;

            var descent = 0.0f;

            var leading = 0.0f;

            var textRuns = new List<SKTextRun>();

            var currentPosition = 0;

            var runText = text.Substring(startingIndex, length);

            while (currentPosition < length)
            {
                var glyphCount = _typeface.CountGlyphs(runText);

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

                var currentRun = CreateTextRun(
                    runText,
                    new SKTextFormat(typeface, _fontSize));

                width += currentRun.Width;

                if (ascent > currentRun.FontMetrics.Ascent)
                {
                    ascent = currentRun.FontMetrics.Ascent;
                }

                if (descent < currentRun.FontMetrics.Descent)
                {
                    descent = currentRun.FontMetrics.Descent;
                }

                if (leading < currentRun.FontMetrics.Leading)
                {
                    leading = currentRun.FontMetrics.Leading;
                }

                textRuns.Add(currentRun);

                currentPosition += glyphCount;
            }

            var height = descent - ascent + leading;

            lineMetrics = new SKTextLineMetrics(width, height, new SKPoint(0, -ascent));

            return textRuns;
        }
    }
}
