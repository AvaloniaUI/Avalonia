﻿// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

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
            var currentLength = 0;
            var remainingLength = span.Length;

            foreach (var textLine in TextLines)
            {
                if (textLine.StartingIndex + textLine.Length < span.StartIndex)
                {
                    currentLength += textLine.Length;

                    continue;
                }

                for (var runIndex = 0; runIndex < textLine.TextRuns.Count; runIndex++)
                {
                    var textRun = textLine.TextRuns[runIndex];

                    if (currentLength + textRun.Text.Length < span.StartIndex)
                    {
                        currentLength += textRun.Text.Length;

                        continue;
                    }

                    var splitLength = Math.Min(textRun.Text.Length, remainingLength);

                    var start = SplitTextRun(textRun, 0, splitLength);

                    if (currentLength != span.StartIndex)
                    {
                        if (Math.Max(0, textRun.Text.Length - splitLength) + remainingLength == textRun.Text.Length)
                        {
                            // Apply at the end of the run
                            ApplyTextSpan(span, start.SecondTextRun);

                            textLine.RemoveTextRun(runIndex);

                            textLine.InsertTextRun(runIndex, start.FirstTextRun);

                            runIndex++;

                            textLine.InsertTextRun(runIndex, start.SecondTextRun);

                            remainingLength -= start.SecondTextRun.Text.Length;
                        }
                        else
                        {
                            // Apply in between the run
                            var end = SplitTextRun(start.SecondTextRun, 0, remainingLength);

                            ApplyTextSpan(span, end.FirstTextRun);

                            textLine.RemoveTextRun(runIndex);

                            textLine.InsertTextRun(runIndex, start.FirstTextRun);

                            runIndex++;

                            textLine.InsertTextRun(runIndex, end.FirstTextRun);

                            runIndex++;

                            textLine.InsertTextRun(runIndex, end.SecondTextRun);

                            remainingLength = 0;
                        }
                    }
                    else
                    {
                        // Apply at start of the run
                        ApplyTextSpan(span, start.FirstTextRun);

                        textLine.RemoveTextRun(runIndex);

                        textLine.InsertTextRun(runIndex, start.FirstTextRun);

                        remainingLength -= start.FirstTextRun.Text.Length;

                        runIndex++;

                        textLine.InsertTextRun(runIndex, start.SecondTextRun);
                    }

                    if (remainingLength == 0)
                    {
                        return;
                    }

                    currentLength += textRun.Text.Length;
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
                        InitializePaintForTextRun(_paint, context, textLine, textRun, foregroundWrapper);
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
                /*Bug: Transparency issue with LcdRenderText = true,*/
                IsStroke = false,
                TextEncoding = SKTextEncoding.Utf32,
                Typeface = typeface,
                TextSize = fontSize
            };
        }

        private static void UpdateTextLineMetrics(ref SKTextLineMetrics lineMetrics, SKTextRun textRun)
        {
            var width = lineMetrics.Size.Width;

            var ascent = lineMetrics.Ascent;

            var descent = lineMetrics.Descent;

            var leading = lineMetrics.Leading;

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

            lineMetrics = new SKTextLineMetrics(width, ascent, descent, leading);
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

        private SplitTextRunResult SplitTextRun(
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

            for (var index = 0; index < _text.Length; index++)
            {
                var c = _text[index];

                switch (c)
                {
                    case '\r':
                    {
                        var breakLines = PerformLineBreak(_text, currentPosition, index - currentPosition);

                        textLines.AddRange(breakLines);

                        if (_text[index + 1] == '\n')
                        {
                            index++;
                        }

                        currentPosition = index + 1;
                        break;
                    }

                    case '\n':
                    {
                        var breakLines = PerformLineBreak(_text, currentPosition, index - currentPosition);

                        textLines.AddRange(breakLines);

                        if (_text[index + 1] == '\r')
                        {
                            index++;
                        }

                        currentPosition = index + 1;
                        break;
                    }
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

                        for (var i = measuredLength; i > 0; i--)
                        {
                            var c = textRun.Text[i];

                            if (char.IsWhiteSpace(c) || c == '\u200B')
                            {
                                measuredLength = ++i;
                                break;
                            }
                        }

                        var splitResult = SplitTextRun(
                            textLine.TextRuns[runIndex],
                            0,
                            measuredLength);

                        textLine.RemoveTextRun(runIndex);

                        textLine.InsertTextRun(runIndex, splitResult.FirstTextRun);

                        textLines.Add(textLine);

                        remainingTextRuns.Add(splitResult.SecondTextRun);

                        var textLineMetrics = new SKTextLineMetrics(
                            splitResult.SecondTextRun.Width,
                            splitResult.SecondTextRun.FontMetrics.Ascent,
                            splitResult.SecondTextRun.FontMetrics.Descent,
                            splitResult.SecondTextRun.FontMetrics.Leading);

                        for (var i = runIndex + 1; i < textLine.TextRuns.Count; i++)
                        {
                            var currentRun = textLine.TextRuns[i];

                            UpdateTextLineMetrics(ref textLineMetrics, currentRun);

                            remainingTextRuns.Add(currentRun);
                        }

                        textLine = new SKTextLine(textLine.StartingIndex + measuredLength, textLine.Length - measuredLength, remainingTextRuns, textLineMetrics);

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
                var textLineMetrics = new SKTextLineMetrics();

                _paint.Typeface = _typeface;

                _paint.TextSize = _fontSize;

                var emptyTextRun = new SKTextRun(string.Empty, new SKTextFormat(_typeface, _fontSize), _paint.FontMetrics, 0.0f);

                UpdateTextLineMetrics(ref textLineMetrics, emptyTextRun);

                return new SKTextLine(startingIndex, 0, new List<SKTextRun> { emptyTextRun }, textLineMetrics);
            }

            var textRuns = CreateTextRuns(text, startingIndex, length, out var lineMetrics);

            return new SKTextLine(startingIndex, length, textRuns, lineMetrics);
        }

        private List<SKTextRun> CreateTextRuns(string text, int startingIndex, int length, out SKTextLineMetrics textLineMetrics)
        {
            var textRuns = new List<SKTextRun>();
            var currentPosition = 0;

            var runText = text.Substring(startingIndex, length);

            textLineMetrics = new SKTextLineMetrics();

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

                UpdateTextLineMetrics(ref textLineMetrics, currentRun);

                textRuns.Add(currentRun);

                currentPosition += glyphCount;
            }

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
