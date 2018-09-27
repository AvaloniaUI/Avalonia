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

        private readonly TextWrapping _wrapping;

        private readonly Size _constraint;

        private readonly SKPaint _paint;

        public SKTextLayout(string text, SKTypeface typeface, float fontSize, TextWrapping wrapping, Size constraint)
        {
            _text = text;
            _typeface = typeface;
            _fontSize = fontSize;
            _wrapping = wrapping;
            _constraint = constraint;
            _paint = CreatePaint(typeface, fontSize);
            TextLines = CreateTextLines();
            LayoutBounds = GetLayoutBounds();
        }

        public IReadOnlyList<SKTextLine> TextLines { get; }

        public Rect LayoutBounds { get; }

        private static SKPaint CreatePaint(SKTypeface typeface, float fontSize)
        {
            return new SKPaint
            {
                HintingLevel = SKPaintHinting.Full,
                DeviceKerningEnabled = true,
                IsAntialias = true,
                IsAutohinted = true,
                LcdRenderText = true,
                SubpixelText = true,
                TextEncoding = SKTextEncoding.Utf32,
                Typeface = typeface,
                TextSize = fontSize
            };
        }

        private static SKTextLineMetrics CreateLineMetrics(IReadOnlyList<SKTextRun> textRuns)
        {
            var width = textRuns.Sum(x => x.Width);

            var ascent = textRuns.Min(x => x.FontMetrics.Ascent);

            var descent = textRuns.Max(x => x.FontMetrics.Descent);

            var leading = textRuns.Max(x => x.FontMetrics.Leading);

            var height = descent - ascent + leading;

            return new SKTextLineMetrics(width, height, new Point(0, -ascent));
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

                    if (textLine.LineMetrics.Size.Width > _constraint.Width)
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

        private Rect GetLayoutBounds()
        {
            var width = TextLines.Max(x => x.LineMetrics.Size.Width);

            var height = TextLines.Sum(x => x.LineMetrics.Size.Height);

            return new Rect(0, 0, width, height);
        }

        private IReadOnlyList<SKTextRun> CreateTextRuns(string text, int startingIndex, int length)
        {
            var textRuns = new List<SKTextRun>();

            var currentPosition = 0;

            var runText = text.Substring(startingIndex, length);

            while (currentPosition < length)
            {
                var glyphCount = _typeface.CountGlyphs(runText);

                if (glyphCount == 0)
                {
                    var fallback = SKFontManager.Default.MatchCharacter(runText[currentPosition]);

                    glyphCount = fallback.CountGlyphs(runText);
                }

                if (glyphCount != length)
                {
                    runText = runText.Substring(currentPosition, glyphCount);
                }

                var currentRun = CreateTextRun(
                    runText,
                    new SKTextFormat(_typeface, _fontSize));

                textRuns.Add(currentRun);

                currentPosition += glyphCount;
            }

            return textRuns;
        }

        private SKTextRun CreateTextRun(string text, SKTextFormat textFormat)
        {
            _paint.Typeface = textFormat.Typeface;

            _paint.TextSize = textFormat.FontSize;

            var fontMetrics = _paint.FontMetrics;

            var width = _paint.MeasureText(text);

            return new SKTextRun(text, textFormat, fontMetrics, width);
        }
    }
}
