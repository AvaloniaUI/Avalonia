// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Media;

using SkiaSharp;

namespace Avalonia.Skia
{
    public partial class FormattedTextImpl
    {
        public class SKTextLayout
        {
            private readonly string _text;

            private readonly SKTypeface _typeface;

            private readonly float _fontSize;

            private readonly Brush _foreground;

            private readonly TextWrapping _wrapping;

            private readonly Size _constraint;

            private readonly SKPaint _paint;

            private readonly List<SKTextLine> _lines = new List<SKTextLine>();

            public SKTextLayout(string text, SKTypeface typeface, float fontSize, TextWrapping wrapping, Size constraint, SKPaint paint)
            {
                _text = text;
                _typeface = typeface;
                _fontSize = fontSize;
                _wrapping = wrapping;
                _constraint = constraint;
                _paint = paint;
                CreateTextLines();
            }

            public Rect TextBounds { get; private set; }

            private void CreateTextLines()
            {
                var text = _text.AsSpan();

                var currentLine = new SKTextLine(_paint, 0, text.Length);

                _lines.Add(currentLine);

                for (int index = 0; index < text.Length; index++)
                {
                    char c = text[index];

                    if (c == '\r')
                    {
                        c = text[index++];

                        if (c == '\n')
                        {
                            index++;
                        }

                        currentLine = currentLine.SliceAt(index);

                        _lines.Add(currentLine);
                    }
                }

                foreach (var textLine in _lines)
                {
                    textLine.CreateTextRuns(this);
                }

                var width = _lines.Max(x => x.LineMetrics.Width);

                var height = _lines.Sum(x => x.LineMetrics.Height);

                TextBounds = new Rect(0, 0, width, height);
            }

            private struct SKTextLineMetrics
            {
                public SKTextLineMetrics(float width, float height, Point baselineOrigin)
                {
                    Width = width;
                    Height = height;
                    BaselineOrigin = baselineOrigin;
                }

                public float Width { get; }

                public float Height { get; }

                public Point BaselineOrigin { get; }
            }

            private class SKTextLine
            {
                private readonly SKPaint _paint;

                private readonly List<SKTextRun> _textRuns = new List<SKTextRun>();

                public SKTextLine(SKPaint paint, int startingIndex, int length)
                {
                    _paint = paint;

                    StartingIndex = startingIndex;

                    Length = length;
                }

                public int StartingIndex { get; }

                public int Length { get; private set; }

                public SKTextLineMetrics LineMetrics { get; private set; }

                public IReadOnlyList<SKTextRun> TextRuns => _textRuns;

                // ToDo: move to TextLayout
                public void CreateTextRuns(SKTextLayout textLayout)
                {
                    int currentPosition = 0;

                    var text = textLayout._text.Substring(StartingIndex, Length);

                    while (currentPosition < Length)
                    {
                        var glyphCount = textLayout._typeface.CountGlyphs(text);

                        if (glyphCount == 0)
                        {
                            var fallback = SKFontManager.Default.MatchCharacter(text[currentPosition]);

                            glyphCount = fallback.CountGlyphs(text);
                        }

                        if (currentPosition != Length)
                        {
                            text = text.Substring(currentPosition, glyphCount);
                        }

                        var currentRun = CreateTextRun(
                            text,
                            new SKTextFormat(textLayout._typeface, textLayout._fontSize));

                        _textRuns.Add(currentRun);

                        currentPosition += glyphCount;
                    }

                    var width = _textRuns.Sum(x => x.Width);

                    var ascent = _textRuns.Min(x => x.FontMetrics.Ascent);

                    var descent = _textRuns.Max(x => x.FontMetrics.Descent);

                    var height = descent - ascent;

                    LineMetrics = new SKTextLineMetrics(width, height, new Point(0, -ascent));
                }

                // ToDo: move to TextLayout
                public SKTextLine SliceAt(int index)
                {
                    var length = Length - index;

                    Length = Length - length;

                    return new SKTextLine(_paint, index, length);
                }

                private SKTextRun CreateTextRun(string text, SKTextFormat textFormat)
                {
                    _paint.Typeface = textFormat.Typeface;

                    _paint.TextSize = textFormat.FontSize;

                    var fontMetrics = _paint.FontMetrics;

                    var width = _paint.MeasureText(text);

                    return new SKTextRun
                    {
                        Text = text,
                        FontMetrics = fontMetrics,
                        Width = width
                    };
                }             
            }

            private class SKTextFormat
            {
                public SKTextFormat(SKTypeface typeface, float fontSize)
                {
                    Typeface = typeface;
                    FontSize = fontSize;
                }

                public SKTypeface Typeface { get; }

                public float FontSize { get; }
            }

            private class SKTextRun
            {
                public string Text { get; set; }

                public SKTextFormat TextFormat { get; set; }

                public IBrush DrawingEffect { get; set; }

                public SKFontMetrics FontMetrics { get; set; }

                public float Width { get; set; }
            }
        }            
    }
}
