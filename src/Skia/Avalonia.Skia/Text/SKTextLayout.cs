// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        /// <summary>
        /// Gets the size of the layout box.
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public Size Size { get; private set; }

        /// <summary>
        /// Applies a text span to the layout.
        /// </summary>
        /// <param name="span">The span.</param>
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
                    bool needsUpdate;
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
                                if (start.SecondTextRun.Text.Length == 0)
                                {
                                    continue;
                                }

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

        /// <summary>
        /// Draws the layout.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="foreground">The default foreground.</param>
        /// <param name="canvas">The canvas.</param>
        /// <param name="origin">The origin.</param>
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
                        if (textRun.TextFormat.Typeface != null)
                        {
                            InitializePaintForTextRun(_paint, context, textLine, textRun, foregroundWrapper);

                            canvas.DrawText(textRun.Text, lineX, lineY, _paint);
                        }

                        lineX += textRun.Width;
                    }

                    currentY += textLine.LineMetrics.Size.Height;
                }
            }
        }

        /// <summary>
        /// Hit tests the specified point.
        /// </summary>
        /// <param name="point">The point to hit test against.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Get the pixel location relative to the top-left of the layout box given the text position.
        /// </summary>
        /// <param name="textPosition">The text position.</param>
        /// <returns></returns>
        public Rect HitTestTextPosition(int textPosition)
        {
            var rectangles = GetRectangles();

            if (textPosition < 0 || textPosition >= rectangles.Count)
            {
                var r = rectangles.LastOrDefault();

                return new Rect(r.X + r.Width, r.Y, 0, r.Height);
            }

            if (textPosition == rectangles.Count)
            {
                var lr = rectangles[rectangles.Count - 1];

                return new Rect(new Point(lr.X + lr.Width, lr.Y), rectangles[textPosition - 1].Size);
            }

            return rectangles[textPosition];
        }

        /// <summary>
        /// Get a set of hit-test rectangles corresponding to a range of text positions.
        /// </summary>
        /// <param name="textPosition">The starting text position.</param>
        /// <param name="textLength">The text length.</param>
        /// <returns></returns>
        public IEnumerable<Rect> HitTestTextRange(int textPosition, int textLength)
        {
            var result = new List<Rect>();

            var rectangles = GetRectangles();

            var lastIndex = textPosition + textLength - 1;

            var currentY = 0.0f;

            foreach (var textLine in TextLines)
            {
                if (textLine.StartingIndex + textLine.Length > textPosition && lastIndex >= textLine.StartingIndex)
                {
                    var lineEndIndex = textLine.StartingIndex + (textLine.Length > 0 ? textLine.Length - 1 : 0);

                    var left = rectangles[textLine.StartingIndex > textPosition ? textLine.StartingIndex : textPosition].X;

                    var right = rectangles[lineEndIndex > lastIndex ? lastIndex : lineEndIndex].Right;

                    result.Add(new Rect(left, currentY, right - left, textLine.LineMetrics.Size.Height));
                }

                currentY += textLine.LineMetrics.Size.Height;
            }

            return result;
        }

        /// <summary>
        /// Applies the text span to a text run.
        /// </summary>
        /// <param name="span">The text span.</param>
        /// <param name="textRun">The text run.</param>
        /// <param name="needsUpdate">If set to <c>true</c> an update to text metrics is needed.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Creates the paint.
        /// </summary>
        /// <param name="typeface">The default typeface.</param>
        /// <param name="fontSize">The default font size.</param>
        /// <returns></returns>
        private static SKPaint CreatePaint(SKTypeface typeface, float fontSize)
        {
            return new SKPaint
            {
                IsAntialias = true,
                /*Bug: Transparency issue with LcdRenderText = true,*/
                IsStroke = false,
                TextEncoding = SKTextEncoding.Utf16,
                Typeface = typeface,
                TextSize = fontSize
            };
        }

        /// <summary>
        /// Creates the text line metrics.
        /// </summary>
        /// <param name="textRuns">The text runs.</param>
        /// <returns></returns>
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

        private static byte[] CreateCharacterCodePoints(string s)
        {
            return Encoding.Unicode.GetBytes(s);
        }

        /// <summary>
        /// Initializes the paint for text run.
        /// </summary>
        /// <param name="paint">The paint.</param>
        /// <param name="context">The context.</param>
        /// <param name="textLine">The text line.</param>
        /// <param name="textRun">The text run.</param>
        /// <param name="foregroundWrapper">The foreground wrapper.</param>
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

        /// <summary>
        /// Determines whether [c] is a break char.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns>
        ///   <c>true</c> if [is break character] [the specified c]; otherwise, <c>false</c>.
        /// </returns>
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

        /// <summary>
        /// Gets the rectangles to hit test against. Each index maps to a text position.
        /// </summary>
        /// <returns></returns>
        private List<Rect> GetRectangles()
        {
            if (_rectangles == null || _rectangles.Count != _text.Length)
            {
                _rectangles = CreateRectangles();
            }

            return _rectangles;
        }

        /// <summary>
        /// Gets the text line offset x.
        /// </summary>
        /// <param name="textAlignment">The text alignment.</param>
        /// <param name="lineWidth">The line width.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">textAlignment - null</exception>
        private double GetTextLineOffsetX(TextAlignment textAlignment, double lineWidth)
        {
            var availableWidth = _constraint.Width > 0 && !double.IsPositiveInfinity(_constraint.Width)
                                     ? _constraint.Width
                                     : Size.Width;

            switch (textAlignment)
            {
                case TextAlignment.Left:
                    return 0.0d;
                case TextAlignment.Center:
                    return (availableWidth - lineWidth) / 2;
                case TextAlignment.Right:
                    return availableWidth - lineWidth;
                default:
                    throw new ArgumentOutOfRangeException(nameof(textAlignment), textAlignment, null);
            }
        }

        /// <summary>
        /// Creates the initial text lines.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Creates a new text line of a specified text range.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="startingIndex">Text starting index.</param>
        /// <param name="length">The text length.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Creates text run with a specific text format.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="textFormat">The text format.</param>
        /// <returns></returns>
        private SKTextRun CreateTextRun(string text, SKTextFormat textFormat)
        {
            _paint.Typeface = textFormat.Typeface;

            _paint.TextSize = textFormat.FontSize;

            var fontMetrics = _paint.FontMetrics;

            var width = _paint.MeasureText(text);

            return new SKTextRun(text, textFormat, fontMetrics, width);
        }

        /// <summary>
        /// Creates a list of text runs. Each text run only consists of one combination of text properties.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="startingIndex">Index of the starting.</param>
        /// <param name="length">The length.</param>
        /// <param name="textLineMetrics">The text line metrics.</param>
        /// <returns></returns>
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
                var glyphCount = Math.Min(runText.Length, _typeface.CountGlyphs(runText));

                var typeface = _typeface;

                if (glyphCount == 0)
                {
                    var codePoint = char.ConvertToUtf32(runText, 0);

                    typeface = SKFontManager.Default.MatchCharacter(codePoint);

                    if (codePoint > sizeof(short))
                    {
                        glyphCount = 2;
                    }
                    else
                    {
                        glyphCount++;
                    }

                    if (typeface != null)
                    {
                        while (glyphCount < runText.Length)
                        {
                            var c = runText[glyphCount];

                            if (c == 0x200D)
                            {
                                glyphCount++;

                                continue;
                            }

                            if (char.IsHighSurrogate(c))
                            {
                                var lowSurrogate = runText[glyphCount + 1];

                                var bytes = Encoding.Unicode.GetBytes(new[] { c, lowSurrogate });

                                var symbol = Encoding.Unicode.GetString(bytes);

                                if (typeface.CountGlyphs(symbol) == 0)
                                {
                                    break;
                                }

                                if (_typeface.CountGlyphs(symbol) != 0)
                                {
                                    break;
                                }

                                glyphCount += 2;
                            }
                            else
                            {
                                var symbol = c.ToString();

                                if (typeface.CountGlyphs(symbol) == 0)
                                {
                                    break;
                                }

                                if (_typeface.CountGlyphs(symbol) != 0)
                                {
                                    break;
                                }

                                glyphCount++;
                            }
                        }
                    }
                }

                if (currentPosition + glyphCount < length)
                {
                    runText = text.Substring(startingIndex + currentPosition, glyphCount);
                }

                var currentRun = CreateTextRun(runText, new SKTextFormat(typeface, _fontSize));

                textRuns.Add(currentRun);

                currentPosition += glyphCount;

                if (currentPosition != length)
                {
                    runText = text.Substring(startingIndex + currentPosition, length - currentPosition);
                }
            }

            textLineMetrics = CreateTextLineMetrics(textRuns);

            return textRuns;
        }

        /// <summary>
        /// Creates a list of rectangles to hit test against. Each index maps to a text position.
        /// </summary>
        /// <returns></returns>
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

                    for (var index = 0; index < textRun.Text.Length; index++)
                    {
                        var c = textRun.Text[index];

                        _paint.Typeface = textRun.TextFormat.Typeface;

                        _paint.TextSize = textRun.TextFormat.FontSize;

                        if (IsBreakChar(c))
                        {
                            rectangles.Add(new Rect(currentX, currentY, 0.0f, currentLine.LineMetrics.Size.Height));
                        }
                        else
                        {
                            byte[] bytes;

                            if (char.IsHighSurrogate(c))
                            {
                                index++;

                                var lowSurrogate = textRun.Text[index];

                                bytes = Encoding.Unicode.GetBytes(new[] { c, lowSurrogate });

                                rectangles.Add(
                                    new Rect(currentX, currentY, 0.0f, currentLine.LineMetrics.Size.Height));
                            }
                            else
                            {
                                bytes = Encoding.Unicode.GetBytes(new[] { c });
                            }

                            var width = _paint.MeasureText(bytes);

                            rectangles.Add(
                                new Rect(currentX, currentY, width, currentLine.LineMetrics.Size.Height));

                            currentX += width;
                        }
                    }
                }

                currentY += currentLine.LineMetrics.Size.Height;
            }

            return rectangles;
        }

        /// <summary>
        /// Performs line breaks if needed and returns a list of text lines.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="startingIndex">Text starting index.</param>
        /// <param name="length">The text length.</param>
        /// <returns></returns>
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

                        var bytes = CreateCharacterCodePoints(textRun.Text);

                        // returns number of bytes
                        var measuredLength = (int)(_paint.BreakText(bytes, availableLength) / 2);

                        if (measuredLength < textRun.Text.Length)
                        {
                            for (var i = measuredLength; i > 0; i--)
                            {
                                var c = textRun.Text[i];

                                if (char.IsWhiteSpace(c) || c == '\u200B')
                                {
                                    measuredLength = ++i;

                                    break;
                                }
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

        /// <summary>
        /// Splits a text run at a specified position and retains all text properties.
        /// </summary>
        /// <param name="textRun">The text run.</param>
        /// <param name="startingIndex">Index of the starting.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        private SplitTextRunResult SplitTextRun(SKTextRun textRun, int startingIndex, int length)
        {
            var firstTextRun = CreateTextRun(textRun.Text.Substring(startingIndex, length), textRun.TextFormat);

            var secondTextRun = CreateTextRun(
                textRun.Text.Substring(length, textRun.Text.Length - length),
                textRun.TextFormat);

            return new SplitTextRunResult(firstTextRun, secondTextRun);
        }

        private class SplitTextRunResult
        {
            public SplitTextRunResult(SKTextRun firstTextRun, SKTextRun secondTextRun)
            {
                FirstTextRun = firstTextRun;

                SecondTextRun = secondTextRun;
            }

            /// <summary>
            /// Gets the first text run.
            /// </summary>
            /// <value>
            /// The first text run.
            /// </value>
            public SKTextRun FirstTextRun { get; }

            /// <summary>
            /// Gets the second text run.
            /// </summary>
            /// <value>
            /// The second text run.
            /// </value>
            public SKTextRun SecondTextRun { get; }
        }
    }
}
