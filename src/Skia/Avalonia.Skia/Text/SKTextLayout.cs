// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
            Size = GetSize();
        }

        public IReadOnlyList<SKTextLine> TextLines { get; }

        public Size Size { get; }

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
                        var textRun = textLine.TextRuns[lineIndex];

                        if (availableLength < textRun.Text.Length)
                        {
                            textLine.RemoveTextRun(runIndex);

                            var firstTextRun = CreateTextRun(
                                textRun.Text.Substring(0, availableLength),
                                textRun.TextFormat);

                            ApplyTextSpan(span, firstTextRun);

                            textLine.InsertTextRun(runIndex, firstTextRun);

                            var secondTextRun = CreateTextRun(
                                textRun.Text.Substring(availableLength, textRun.Text.Length - availableLength),
                                textRun.TextFormat);

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

        // ToDo: This can be calculated when text runs are added (BaselineOrigin(Y += Width))
        private static SKTextLineMetrics CreateLineMetrics(IReadOnlyList<SKTextRun> textRuns)
        {
            var width = textRuns.Sum(x => x.Width);

            var ascent = textRuns.Min(x => x.FontMetrics.Ascent);

            var descent = textRuns.Max(x => x.FontMetrics.Descent);

            var leading = textRuns.Max(x => x.FontMetrics.Leading);

            var height = descent - ascent + leading;

            return new SKTextLineMetrics(width, height, new SKPoint(0, -ascent));
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

                    var textLine = CreateTextLine(_text, currentPosition, index - currentPosition + 1);

                    if (textLine.LineMetrics.Size.Width > _constraint.Width && _textWrapping == TextWrapping.Wrap)
                    {
                        var lineBreakResult = PerformLineBreak(textLine, (float)_constraint.Width);

                        textLines.AddRange(lineBreakResult);
                    }
                    else
                    {
                        textLines.Add(textLine);
                    }

                    currentPosition = index + 1;
                }
            }

            if (currentPosition < _text.Length)
            {
                textLines.Add(CreateTextLine(_text, currentPosition, _text.Length - currentPosition));
            }

            return textLines;
        }

        private IEnumerable<SKTextLine> PerformLineBreak(SKTextLine textLine, float maxWidth)
        {
            var lineBreakResult = new List<SKTextLine>();

            var currentPosition = 0;

            var currentWidth = 0.0f;

            for (var i = 0; i < textLine.TextRuns.Count; i++)
            {
                var currentRun = textLine.TextRuns[i];

                currentWidth += currentRun.Width;

                if (currentWidth > maxWidth)
                {
                    // Time to split something.

                    currentPosition = i;
                }
            }

            return lineBreakResult;
        }

        private SKTextLine CreateTextLine(string text, int startingIndex, int length)
        {
            var textRuns = CreateTextRuns(text, startingIndex, length);

            var lineMetrics = CreateLineMetrics(textRuns);

            return new SKTextLine(startingIndex, length, textRuns, lineMetrics);
        }

        // ToDo: Update size when lines are added
        private Size GetSize()
        {
            var width = TextLines.Max(x => x.LineMetrics.Size.Width);

            var height = TextLines.Sum(x => x.LineMetrics.Size.Height);

            return new Size(width, height);
        }

        private List<SKTextRun> CreateTextRuns(string text, int startingIndex, int length)
        {
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

                textRuns.Add(currentRun);

                currentPosition += glyphCount;
            }

            return textRuns;
        }
    }
}
