// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Avalonia.Media;

using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace Avalonia.Skia.Text
{
    public class SKTextLayout
    {
        private readonly ReadOnlyMemory<char> _text;

        private readonly SKTypeface _typeface;

        private readonly float _fontSize;

        private readonly TextAlignment _textAlignment;

        private readonly TextWrapping _textWrapping;

        private readonly Size _constraint;

        private readonly SKPaint _paint;

        private readonly List<SKTextLine> _textLines;

        /// <summary>
        /// Initializes a new instance of the <see cref="SKTextLayout"/> class.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="typeface">The typeface.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="textAlignment">The text alignment.</param>
        /// <param name="textWrapping">The text wrapping.</param>
        /// <param name="constraint">The constraint.</param>
        /// <param name="spans">The spans.</param>
        public SKTextLayout(
            string text,
            SKTypeface typeface,
            float fontSize,
            TextAlignment textAlignment,
            TextWrapping textWrapping,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans = null)
        {
            _text = text.AsMemory();
            _typeface = typeface;
            _fontSize = fontSize;
            _textAlignment = textAlignment;
            _textWrapping = textWrapping;
            _constraint = constraint;
            _paint = CreatePaint(typeface, fontSize);
            _textLines = CreateTextLines(spans);
        }

        /// <summary>
        /// Gets the text lines.
        /// </summary>
        /// <value>
        /// The text lines.
        /// </value>
        public IReadOnlyList<SKTextLine> TextLines => _textLines;

        /// <summary>
        /// Gets the size of the layout box.
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        public Size Size { get; private set; }

        /// <summary>
        /// Draws the layout.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="foreground">The default foreground.</param>
        /// <param name="canvas">The canvas.</param>
        /// <param name="origin">The origin.</param>
        public unsafe void Draw(DrawingContextImpl context, IBrush foreground, SKCanvas canvas, SKPoint origin)
        {
            if (!TextLines.Any())
            {
                return;
            }

            var currentMatrix = canvas.TotalMatrix;

            _paint.TextEncoding = SKTextEncoding.GlyphId;

            canvas.Translate(origin.X, origin.Y);

            using (var foregroundWrapper = context.CreatePaint(foreground, Size))
            {
                foreach (var textLine in TextLines)
                {
                    var lineOffsetX = (float)GetTextLineOffsetX(_textAlignment, textLine.LineMetrics.Size.Width);

                    canvas.Translate(lineOffsetX, textLine.LineMetrics.BaselineOrigin.Y);

                    foreach (var textRun in textLine.TextRuns)
                    {
#if LAYOUT_DEBUG
                        canvas.Translate(0, -textLine.LineMetrics.BaselineOrigin.Y);

                        canvas.DrawRect(
                            new SKRect(
                                0,
                                0,
                                textRun.Width,
                                textLine.LineMetrics.Size.Height),
                            new SKPaint
                            {
                                IsStroke = true,
                                Color = GetRandomColor().ToSKColor()
                            });

                        foreach (var glyphCluster in textRun.GlyphRun.GlyphClusters)
                        {
                            canvas.DrawRect(
                                glyphCluster.Bounds,
                                new SKPaint
                                {
                                    Color = GetRandomColor().ToSKColor()
                                });
                        }

                        canvas.Translate(0, textLine.LineMetrics.BaselineOrigin.Y);
#endif
                        if (textRun.TextFormat.Typeface != null)
                        {
                            InitializePaintForTextRun(_paint, context, textLine, textRun, foregroundWrapper);

                            fixed (ushort* buffer = textRun.GlyphRun.GlyphIndices)
                            {
                                var p = (IntPtr)buffer;

                                canvas.DrawPositionedText(p, textRun.GlyphRun.GlyphIndices.Length * 2, textRun.GlyphRun.GlyphOffsets, _paint);
                            }
                        }

                        canvas.Translate(textRun.Width, 0);
                    }

                    canvas.Translate(-textLine.LineMetrics.Size.Width, textLine.LineMetrics.Descent);
                }
            }

            canvas.SetMatrix(currentMatrix);
        }

#if LAYOUT_DEBUG
        private static readonly Random s_random = new Random();

        private static Color GetRandomColor()
        {
            return Color.FromArgb(128, (byte)s_random.Next(256), (byte)s_random.Next(256), (byte)s_random.Next(256));
        }
#endif

        /// <summary>
        /// Hit tests the specified point.
        /// </summary>
        /// <param name="point">The point to hit test against.</param>
        /// <returns></returns>
        public TextHitTestResult HitTestPoint(Point point)
        {
            if (_text.Length == 0)
            {
                return new TextHitTestResult();
            }

            var pointY = (float)point.Y;

            var currentY = 0.0f;

            bool isTrailing;

            foreach (var textLine in TextLines)
            {
                if (pointY <= currentY + textLine.LineMetrics.Size.Height)
                {
                    var currentX = GetTextLineOffsetX(_textAlignment, textLine.LineMetrics.Size.Width);

                    var textPosition = textLine.TextPointer.StartingIndex;

                    foreach (var textRun in textLine.TextRuns)
                    {
                        if (currentX + textRun.Width < point.X)
                        {
                            currentX += textRun.Width;

                            textPosition += textRun.Text.Length;

                            continue;
                        }

                        foreach (var glyphCluster in textRun.GlyphRun.GlyphClusters)
                        {
                            if (currentX + glyphCluster.Bounds.Width < point.X)
                            {
                                currentX += glyphCluster.Bounds.Width;

                                continue;
                            }

                            isTrailing = point.X - currentX > glyphCluster.Bounds.Width / 2;

                            var isInside = point.X >= currentX && point.X <= textRun.Width;

                            textPosition += glyphCluster.TextPosition;

                            return new TextHitTestResult
                            {
                                IsInside = isInside,
                                TextPosition = textPosition,
                                Length = glyphCluster.Length,
                                Bounds = new Rect(currentX, currentY, glyphCluster.Bounds.Width, glyphCluster.Bounds.Height),
                                IsTrailing = isTrailing
                            };
                        }
                    }

                    if (point.X > currentX && textLine.TextPointer.Length > 0)
                    {
                        textPosition = textLine.TextPointer.StartingIndex;

                        for (var runIndex = 0; runIndex < textLine.TextRuns.Count - 1; runIndex++)
                        {
                            textPosition += textLine.TextRuns[runIndex].Text.Length;
                        }

                        var textRun = textLine.TextRuns.LastOrDefault();

                        var glyphCluster = textRun?.GlyphRun.GlyphClusters.LastOrDefault();

                        if (glyphCluster != null)
                        {
                            textPosition += glyphCluster.TextPosition;

                            isTrailing = _text.Length == textPosition + 1;

                            return new TextHitTestResult
                            {
                                IsInside = false,
                                IsTrailing = isTrailing,
                                TextPosition = textPosition,
                                Length = glyphCluster.Length,
                            };
                        }
                    }
                }

                currentY += textLine.LineMetrics.Size.Height;
            }

            isTrailing = point.X > Size.Width || point.Y > Size.Height;

            var lastLine = TextLines.Last();

            var lastRun = lastLine.TextRuns.Last();

            var lastCluster = lastRun.GlyphRun.GlyphClusters.Last();

            return new TextHitTestResult
            {
                IsInside = false,
                IsTrailing = true,
                TextPosition = isTrailing ? _text.Length - lastCluster.Length : 0,
                Length = lastCluster.Length
            };
        }

        /// <summary>
        /// Get the pixel location relative to the top-left of the layout box given the text position.
        /// </summary>
        /// <param name="textPosition">The text position.</param>
        /// <returns></returns>
        public Rect HitTestTextPosition(int textPosition)
        {
            if (!TextLines.Any())
            {
                return new Rect();
            }

            if (textPosition < 0 || textPosition >= _text.Length)
            {
                var lastLine = TextLines.Last();

                var offsetX = GetTextLineOffsetX(_textAlignment, lastLine.LineMetrics.Size.Width);

                var lineX = offsetX + lastLine.LineMetrics.Size.Width;

                var lineY = Size.Height - lastLine.LineMetrics.Size.Height;

                return new Rect(lineX, lineY, 0, lastLine.LineMetrics.Size.Height);
            }

            var currentY = 0.0f;

            foreach (var textLine in TextLines)
            {
                if (textLine.TextPointer.StartingIndex + textLine.TextPointer.Length - 1 < textPosition)
                {
                    currentY += textLine.LineMetrics.Size.Height;

                    continue;
                }

                var currentX = GetTextLineOffsetX(_textAlignment, textLine.LineMetrics.Size.Width);

                var currentPosition = textLine.TextPointer.StartingIndex;

                foreach (var textRun in textLine.TextRuns)
                {
                    if (currentPosition + textRun.Text.Length - 1 < textPosition)
                    {
                        currentX += textRun.Width;

                        currentPosition += textRun.Text.Length;

                        continue;
                    }

                    foreach (var glyphCluster in textRun.GlyphRun.GlyphClusters)
                    {
                        if (currentPosition + glyphCluster.TextPosition + glyphCluster.Length - 1 < textPosition)
                        {
                            currentX += glyphCluster.Bounds.Width;

                            continue;
                        }

                        return new Rect(currentX, currentY, glyphCluster.Bounds.Width, glyphCluster.Bounds.Height);
                    }
                }
            }

            return new Rect();
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

            var currentY = 0f;
            var remainingLength = textLength;

            foreach (var textLine in TextLines)
            {
                if (textLine.TextPointer.StartingIndex + textLine.TextPointer.Length - 1 < textPosition)
                {
                    currentY += textLine.LineMetrics.Size.Height;

                    continue;
                }

                var lineX = (float)GetTextLineOffsetX(_textAlignment, textLine.LineMetrics.Size.Width);
                var currentPosition = textLine.TextPointer.StartingIndex;
                var startX = -1f;

                foreach (var textRun in textLine.TextRuns)
                {
                    if (currentPosition + textRun.Text.Length - 1 < textPosition)
                    {
                        lineX += textRun.Width;

                        currentPosition += textRun.Text.Length;

                        continue;
                    }

                    foreach (var glyphCluster in textRun.GlyphRun.GlyphClusters)
                    {
                        if (currentPosition + glyphCluster.TextPosition < textPosition)
                        {
                            lineX += glyphCluster.Bounds.Width;

                            continue;
                        }

                        if (startX < 0)
                        {
                            startX = lineX;
                        }

                        remainingLength -= glyphCluster.Length;

                        lineX += glyphCluster.Bounds.Width;

                        if (remainingLength <= 0)
                        {
                            break;
                        }
                    }

                    currentPosition += textRun.Text.Length;

                    if (remainingLength <= 0)
                    {
                        break;
                    }
                }

                var rect = new Rect(startX, currentY, lineX - startX, textLine.LineMetrics.Size.Height);

                result.Add(rect);

                if (remainingLength <= 0)
                {
                    break;
                }

                currentY += textLine.LineMetrics.Size.Height;
            }

            return result;
        }

        /// <summary>
        /// Gets the line break position that is indicated by a unicode break char.
        /// </summary>
        /// <param name="textRun">The text run.</param>
        /// <returns></returns>
        private static int GetLineBreakPosition(SKTextRun textRun)
        {
            for (var index = 0; index < textRun.Text.Length; index++)
            {
                var c = textRun.Text.Span[index];

                if (!IsBreakChar(c))
                {
                    continue;
                }

                if (index < textRun.Text.Length - 1)
                {
                    switch (c)
                    {
                        case '\r' when textRun.Text.Span[index + 1] == '\n':
                        case '\n' when textRun.Text.Span[index + 1] == '\r':
                            return ++index;
                    }
                }

                return index;
            }

            return -1;
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
        /// <param name="length">Text length</param>
        /// <returns></returns>
        private static SKTextLineMetrics CreateTextLineMetrics(IEnumerable<SKTextRun> textRuns, out int length)
        {
            var width = 0.0f;
            var ascent = 0.0f;
            var descent = 0.0f;
            var leading = 0.0f;

            length = 0;

            foreach (var textRun in textRuns)
            {
                length += textRun.Text.Length;

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

            if (textRun.Foreground == null)
            {
                foregroundWrapper.ApplyTo(paint);
            }
            else
            {
                using (var effectWrapper = context.CreatePaint(
                    textRun.Foreground,
                    new Size(textRun.Width, textLine.LineMetrics.Size.Height)))
                {
                    effectWrapper.ApplyTo(paint);
                }
            }
        }

        /// <summary>
        /// Determines whether [c] is a break char.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns>
        ///   <c>true</c> if [is break character] [the specified c]; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsBreakChar(char c)
        {
            switch (c)
            {
                case '\u000A':
                case '\u000B':
                case '\u000C':
                case '\u000D':
                case '\u0085':
                case '\u2028':
                case '\u2029':
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether [c] is a zero space char.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns>
        /// <c>true</c> if [is zero space character] [the specified c]; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsZeroSpace(char c)
        {
            switch (char.GetUnicodeCategory(c))
            {
                case UnicodeCategory.Control:
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.Format:
                    return true;
            }

            return false;
        }

        private static int BreakGlyphs(SKGlyphRun glyphRun, float availableWidth)
        {
            var count = 0;
            var currentWidth = 0.0f;

            foreach (var cluster in glyphRun.GlyphClusters)
            {
                if (currentWidth + cluster.Bounds.Width > availableWidth)
                {
                    return count;
                }

                currentWidth += cluster.Bounds.Width;

                count += cluster.Length;
            }

            return count;
        }

        /// <summary>
        /// Creates a new text line of a specified text runs.
        /// </summary>
        /// <returns></returns>
        private SKTextLine CreateShapedTextLine(IReadOnlyList<SKTextRun> textRuns, int startingIndex)
        {
            var shapedTextRuns = new List<SKTextRun>();

            foreach (var textRun in textRuns)
            {
                if (textRun.GlyphRun == null)
                {
                    var shapedRun = CreateTextRun(textRun.Text, textRun.TextFormat, textRun.Foreground, true);

                    shapedTextRuns.Add(shapedRun);
                }
                else
                {
                    shapedTextRuns.Add(textRun);
                }
            }

            var lineMetrics = CreateTextLineMetrics(shapedTextRuns, out var length);

            return new SKTextLine(new SKTextPointer(startingIndex, length), shapedTextRuns, lineMetrics);
        }

        /// <summary>
        /// Creates a empty text line.
        /// </summary>
        /// <returns></returns>
        private SKTextLine CreateEmptyTextLine()
        {
            _paint.Typeface = _typeface;

            _paint.TextSize = _fontSize;

            var fontMetrics = _paint.FontMetrics;

            var textLineMetrics = new SKTextLineMetrics(0, fontMetrics.Ascent, fontMetrics.Descent, fontMetrics.Leading);

            return new SKTextLine(new SKTextPointer(), new List<SKTextRun>(), textLineMetrics);
        }

        /// <summary>
        /// Applies a text style span to the layout.
        /// </summary>
        /// <param name="textLines">The text lines to apply the text style span to.</param>
        /// <param name="span">The text style span.</param>
        private void ApplyTextStyleSpan(IList<SKTextLine> textLines, FormattedTextStyleSpan span)
        {
            if (span.Length < 1)
            {
                return;
            }

            var currentLength = 0;
            var appliedLength = 0;

            for (var lineIndex = 0; lineIndex < textLines.Count; lineIndex++)
            {
                var currentTextLine = textLines[lineIndex];

                if (currentTextLine.TextPointer.Length == 0)
                {
                    continue;
                }

                if (currentLength + currentTextLine.TextPointer.Length - 1 < span.StartIndex)
                {
                    currentLength += currentTextLine.TextPointer.Length;

                    continue;
                }

                var textRuns = new List<SKTextRun>(currentTextLine.TextRuns);

                for (var runIndex = 0; runIndex < textRuns.Count; runIndex++)
                {
                    bool needsUpdate;

                    var currentTextRun = textRuns[runIndex];

                    if (currentTextRun.Text.Length == 0)
                    {
                        continue;
                    }

                    if (currentLength + currentTextRun.Text.Length - 1 < span.StartIndex)
                    {
                        currentLength += currentTextRun.Text.Length;

                        continue;
                    }

                    if (currentLength == span.StartIndex + appliedLength)
                    {
                        var splitLength = span.Length - appliedLength;

                        // Make sure we don't split a surrogate pair
                        if (splitLength < currentTextRun.Text.Length && char.IsSurrogatePair(
                                currentTextRun.Text.Span[splitLength - 1],
                                currentTextRun.Text.Span[splitLength]))
                        {
                            splitLength++;
                        }

                        if (splitLength >= currentTextRun.Text.Length)
                        {
                            // Apply to the whole run 
                            textRuns.RemoveAt(runIndex);

                            var updatedTextRun = ApplyTextStyleSpan(span, currentTextRun, out needsUpdate);

                            appliedLength += updatedTextRun.Text.Length;

                            textRuns.Insert(runIndex, updatedTextRun);
                        }
                        else
                        {
                            // Apply at start of the run 
                            var start = SplitTextRun(currentTextRun, 0, splitLength);

                            textRuns.RemoveAt(runIndex);

                            var updatedTextRun = ApplyTextStyleSpan(span, start.FirstTextRun, out needsUpdate);

                            appliedLength += updatedTextRun.Text.Length;

                            textRuns.Insert(runIndex, updatedTextRun);

                            runIndex++;

                            textRuns.Insert(runIndex, start.SecondTextRun);
                        }
                    }
                    else
                    {
                        var splitLength = Math.Min(
                            span.StartIndex + appliedLength - currentLength,
                            currentTextRun.Text.Length);

                        var splitWithinSurrogatePair = false;

                        // Make sure we don't split a surrogate pair
                        if (char.IsHighSurrogate(currentTextRun.Text.Span[splitLength - 1]))
                        {
                            splitWithinSurrogatePair = true;
                            splitLength--;
                        }

                        if (splitLength > 0)
                        {
                            var start = SplitTextRun(currentTextRun, 0, splitLength);

                            if (splitLength + span.Length - appliedLength >= currentTextRun.Text.Length)
                            {
                                // Apply at the end of the run      
                                textRuns.RemoveAt(runIndex);

                                textRuns.Insert(runIndex, start.FirstTextRun);

                                runIndex++;

                                var updatedTextRun = ApplyTextStyleSpan(span, start.SecondTextRun, out needsUpdate);

                                appliedLength += updatedTextRun.Text.Length;

                                textRuns.Insert(runIndex, updatedTextRun);
                            }
                            else
                            {
                                // Make sure we don't split a surrogate pair
                                if ((splitWithinSurrogatePair && span.Length < 2)
                                    || char.IsHighSurrogate(start.SecondTextRun.Text.Span[span.Length - 1]))
                                {
                                    splitLength = 2;
                                }
                                else
                                {
                                    splitLength = span.Length;
                                }

                                // Apply in between the run
                                var end = SplitTextRun(start.SecondTextRun, 0, splitLength);

                                textRuns.RemoveAt(runIndex);

                                textRuns.Insert(runIndex, start.FirstTextRun);

                                runIndex++;

                                var updatedTextRun = ApplyTextStyleSpan(span, end.FirstTextRun, out needsUpdate);

                                appliedLength += updatedTextRun.Text.Length;

                                textRuns.Insert(runIndex, updatedTextRun);

                                runIndex++;

                                textRuns.Insert(runIndex, end.SecondTextRun);
                            }
                        }
                        else
                        {
                            textRuns.RemoveAt(runIndex);

                            var updatedTextRun = ApplyTextStyleSpan(span, currentTextRun, out needsUpdate);

                            textRuns.Insert(runIndex, updatedTextRun);
                        }
                    }

                    textLines.RemoveAt(lineIndex);

                    if (needsUpdate)
                    {
                        currentTextLine = CreateShapedTextLine(textRuns, currentTextLine.TextPointer.StartingIndex);
                    }
                    else
                    {
                        currentTextLine = new SKTextLine(
                            currentTextLine.TextPointer,
                            textRuns,
                            currentTextLine.LineMetrics);
                    }

                    textLines.Insert(lineIndex, currentTextLine);

                    if (appliedLength >= span.Length)
                    {
                        return;
                    }

                    currentLength += currentTextRun.Text.Length;
                }
            }
        }

        /// <summary>
        /// Applies the text style span to a text run.
        /// </summary>
        /// <param name="span">The text span.</param>
        /// <param name="textRun">The text run.</param>
        /// <param name="needsUpdate">Indicates whether the layout needs to update measures.</param>
        /// <returns></returns>
        private SKTextRun ApplyTextStyleSpan(FormattedTextStyleSpan span, SKTextRun textRun, out bool needsUpdate)
        {
            var text = textRun.Text;
            var textFormat = textRun.TextFormat;
            var glyphRun = textRun.GlyphRun;
            var fontMetrics = textRun.FontMetrics;
            var width = textRun.Width;
            var drawingEffect = span.Foreground ?? textRun.Foreground;

            if (span.FontSize != null || span.Typeface != null)
            {
                var fontSize = span.FontSize ?? textFormat.FontSize;

                var typeFace = _typeface;

                if (span.Typeface != null)
                {
                    var typefaceCollection = SKTypefaceCollectionCache.GetOrAddTypefaceCollection(span.Typeface.FontFamily);

                    typeFace = typefaceCollection.GetTypeFace(span.Typeface);                 
                }

                textFormat = new SKTextFormat(typeFace, (float)fontSize);

                needsUpdate = true;

                return CreateTextRun(text, textFormat, drawingEffect, true);
            }

            needsUpdate = false;

            return new SKTextRun(
                text,
                glyphRun,
                textFormat,
                fontMetrics,
                width,
                drawingEffect);
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
        private List<SKTextLine> CreateTextLines(IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            if (_text.Length == 0)
            {
                var emptyTextLine = CreateEmptyTextLine();

                Size = new Size(emptyTextLine.LineMetrics.Size.Width, emptyTextLine.LineMetrics.Size.Height);

                return new List<SKTextLine> { emptyTextLine };
            }

            var currentTextRuns = CreateTextRuns(_text, 0, _text.Length);

            var textLines = new List<SKTextLine>();

            var currentPosition = 0;

            while (currentTextRuns != null)
            {
                var length = 0;

                foreach (var textRun in currentTextRuns)
                {
                    var lineBreakPosition = GetLineBreakPosition(textRun);

                    if (lineBreakPosition == -1)
                    {
                        if (currentPosition + length + textRun.Text.Length == _text.Length)
                        {
                            var textLine = CreateShapedTextLine(currentTextRuns, currentPosition);

                            var textWrappingResult = PerformTextWrapping(textLine);

                            textLines.AddRange(textWrappingResult);

                            currentTextRuns = null;

                            break;
                        }

                        length += textRun.Text.Length;
                    }
                    else
                    {
                        length += lineBreakPosition + 1;

                        var splitResult = SplitTextRuns(currentTextRuns, length);

                        var textLine = CreateShapedTextLine(splitResult.FirstTextRuns, currentPosition);

                        var textWrappingResult = PerformTextWrapping(textLine);

                        textLines.AddRange(textWrappingResult);

                        currentTextRuns = splitResult.SecondTextRuns;

                        currentPosition += textLine.TextPointer.Length;

                        break;
                    }
                }
            }

            if (spans != null)
            {
                foreach (var textStyleSpan in spans)
                {
                    ApplyTextStyleSpan(textLines, textStyleSpan);
                }
            }

            var sizeX = 0.0f;
            var sizeY = 0.0f;

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
        /// Creates a text run.
        /// </summary>
        /// <param name="textRun">The text run.</param>
        /// <param name="startingIndex">Index of the starting.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        private SKTextRun CreateTextRun(SKTextRun textRun, int startingIndex, int length)
        {
            var text = textRun.Text.Slice(startingIndex, length);

            return CreateTextRun(text, textRun.TextFormat, textRun.Foreground, true);
        }

        /// <summary>
        /// Creates a text run with a specific text format.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="textFormat">The text format.</param>
        /// <param name="foregroundBrush">The foreground brush.</param>
        /// <param name="shapeRun">Indicates whether the run should get shaped or not.</param>
        /// <returns></returns>
        private unsafe SKTextRun CreateTextRun(ReadOnlyMemory<char> text, SKTextFormat textFormat, IBrush foregroundBrush = null, bool shapeRun = false)
        {
            _paint.Typeface = textFormat.Typeface;

            _paint.TextSize = textFormat.FontSize;

            _paint.TextEncoding = SKTextEncoding.Utf16;

            var fontMetrics = _paint.FontMetrics;

            if (text.Length == 0)
            {
                return CreateEmptyTextRun(textFormat);
            }

            if (!shapeRun)
            {
                return new SKTextRun(text, null, textFormat, fontMetrics, 0, foregroundBrush);
            }

            using (var shaper = new SKShaper(textFormat.Typeface))
            using (var buffer = new HarfBuzzSharp.Buffer())
            {
                using (var pinHandle = text.Pin())
                {
                    buffer.AddUtf16((IntPtr)pinHandle.Pointer, text.Length, 0, text.Length);
                }

                buffer.GuessSegmentProperties();

                var result = shaper.Shape(buffer, _paint);

                var glyphsIds = result.Codepoints.Select(x => (ushort)x).ToArray();

                var points = result.Points;

                var clusters = result.Clusters;

                var glyphClusters = CreateGlyphClusters(text.Span, fontMetrics, glyphsIds, clusters, points);

                var glyphs = new SKGlyphRun(glyphsIds, points, glyphClusters);

                var width = glyphs.GlyphClusters.Sum(x => x.Bounds.Width);

                return new SKTextRun(text, glyphs, textFormat, fontMetrics, width, foregroundBrush);
            }
        }

        /// <summary>
        /// Creates the empty text run.
        /// </summary>
        /// <param name="textFormat">The text format.</param>
        /// <returns></returns>
        private SKTextRun CreateEmptyTextRun(SKTextFormat textFormat)
        {
            _paint.Typeface = textFormat.Typeface;

            _paint.TextSize = textFormat.FontSize;

            var fontMetrics = _paint.FontMetrics;

            var height = fontMetrics.Descent - fontMetrics.Ascent + fontMetrics.Leading;

            var glyphClusters = new[] { new SKGlyphCluster(0, 0, new SKRect(0, 0, 0, height)) };

            var glyphs = new SKGlyphRun(Array.Empty<ushort>(), Array.Empty<SKPoint>(), glyphClusters);

            return new SKTextRun(new ReadOnlyMemory<char>(), glyphs, textFormat, fontMetrics, 0);
        }

        /// <summary>
        /// Creates the glyph clusters.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="fontMetrics">The font metrics.</param>
        /// <param name="glyphsIndices">The glyphs indices.</param>
        /// <param name="clusters">The clusters.</param>
        /// <param name="glyphOffsets">The glyph offsets.</param>
        /// <returns></returns>
        private IReadOnlyList<SKGlyphCluster> CreateGlyphClusters(
            ReadOnlySpan<char> text,
            SKFontMetrics fontMetrics,
            ushort[] glyphsIndices,
            uint[] clusters,
            SKPoint[] glyphOffsets)
        {
            var glyphClusters = new List<SKGlyphCluster>();

            _paint.TextEncoding = SKTextEncoding.GlyphId;

            var height = fontMetrics.Descent - fontMetrics.Ascent + fontMetrics.Leading;

            var currentCluster = 0;

            var lastCluster = clusters.Length - 1;

            while (currentCluster <= lastCluster)
            {
                var currentPosition = (int)clusters[currentCluster];

                // ToDo: Need a custom implementation that searches for the next cluster.
                var nextCluster = Array.BinarySearch(clusters, (uint)(currentPosition + 1));

                if (nextCluster < 0)
                {
                    nextCluster = ~nextCluster;
                }

                var width = 0f;

                int length;

                if (nextCluster > lastCluster || currentCluster == lastCluster)
                {
                    length = text.Length - currentPosition;
                }
                else
                {
                    var nextPosition = (int)clusters[nextCluster];

                    length = nextPosition - currentPosition;
                }

                for (var index = currentCluster; index < nextCluster; index++)
                {
                    var c = text[index];

                    // SkiaSharp doesn't handle zero space characters properly and will always return a width > 0.
                    if (IsZeroSpace(c))
                    {
                        if (index >= text.Length - 1)
                        {
                            continue;
                        }

                        // Make sure pairs of \n\r and \n\r form a cluster.
                        switch (c)
                        {
                            case '\r' when text[index + 1] == '\n':
                            case '\n' when text[index + 1] == '\r':
                                nextCluster++;
                                length = 2;
                                break;
                        }
                    }
                    else
                    {
                        var bytes = BitConverter.GetBytes(glyphsIndices[index]);

                        // https://github.com/google/skia/blob/master/include/core/SkFont.h#L407
                        var measuredWidth = _paint.MeasureText(bytes);

                        // ToDo: proper width calculation of clusters with diacritics
                        if (width < measuredWidth)
                        {
                            width = measuredWidth;
                        }
                    }
                }

                var point = glyphOffsets[currentCluster];

                var rect = new SKRect(point.X, point.Y, point.X + width, point.Y + height);

                glyphClusters.Add(new SKGlyphCluster(currentPosition, length, rect));

                currentCluster = nextCluster;
            }

            return glyphClusters;
        }

        /// <summary>
        /// Creates a list of text runs. Each text run only consists of one combination of text properties.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="startingIndex">Index of the starting.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        private unsafe IReadOnlyList<SKTextRun> CreateTextRuns(
            ReadOnlyMemory<char> text,
            int startingIndex,
            int length)
        {
            var textRuns = new List<SKTextRun>();
            var textPosition = 0;

            var runText = text.Length == length ? text : text.Slice(startingIndex, length);

            while (textPosition < length)
            {
                int glyphCount;

                using (var pinHandle = runText.Pin())
                {
                    glyphCount = Math.Min(
                        runText.Length,
                        _typeface.CountGlyphs((IntPtr)pinHandle.Pointer, runText.Length, SKEncoding.Utf16));
                }

                if (runText.Length > glyphCount)
                {
                    var c = runText.Span[glyphCount];

                    if (IsBreakChar(c))
                    {
                        glyphCount++;

                        if (glyphCount < runText.Length)
                        {
                            switch (c)
                            {
                                case '\r' when runText.Span[glyphCount] == '\n':
                                case '\n' when runText.Span[glyphCount] == '\r':
                                    glyphCount++;
                                    break;
                            }
                        }
                    }
                }

                var typeface = _typeface;

                if (glyphCount == 0)
                {
                    var codePoint = char.IsHighSurrogate(runText.Span[0])
                                        ? char.ConvertToUtf32(runText.Span[0], runText.Span[1])
                                        : runText.Span[0];

                    typeface = SKFontManager.Default.MatchCharacter(codePoint);

                    if (codePoint > short.MaxValue)
                    {
                        glyphCount += 2;
                    }
                    else
                    {
                        glyphCount++;
                    }

                    if (typeface != null)
                    {
                        while (glyphCount < runText.Length)
                        {
                            var c = runText.Span[glyphCount];

                            if (char.IsWhiteSpace(c) || IsZeroSpace(c))
                            {
                                glyphCount++;

                                continue;
                            }

                            var isSurrogatePair = false;
                            char[] chars;

                            if (char.IsHighSurrogate(c))
                            {
                                isSurrogatePair = true;

                                var lowSurrogate = runText.Span[glyphCount + 1];

                                chars = new[] { c, lowSurrogate };
                            }
                            else
                            {
                                chars = new[] { c };
                            }

                            var bytes = Encoding.Unicode.GetBytes(chars);

                            fixed (byte* p = bytes)
                            {
                                var ptr = (IntPtr)p;

                                if (typeface.CountGlyphs(ptr, bytes.Length, SKEncoding.Utf16) == 0)
                                {
                                    break;
                                }

                                if (_typeface.CountGlyphs(ptr, bytes.Length, SKEncoding.Utf16) != 0)
                                {
                                    break;
                                }
                            }

                            if (isSurrogatePair)
                            {
                                glyphCount += 2;
                            }
                            else
                            {
                                glyphCount++;
                            }
                        }
                    }
                }

                if (textPosition + glyphCount < length)
                {
                    runText = text.Slice(textPosition, glyphCount);
                }

                var currentRun = CreateTextRun(runText, new SKTextFormat(typeface, _fontSize));

                textRuns.Add(currentRun);

                textPosition += glyphCount;

                if (textPosition != length)
                {
                    runText = text.Slice(startingIndex + textPosition, length - textPosition);
                }
            }

            return textRuns;
        }

        /// <summary>
        /// Performs text wrapping if needed and returns a list of text lines.
        /// </summary>
        /// <param name="textLine">The text line.</param>
        /// <returns></returns>
        private IReadOnlyList<SKTextLine> PerformTextWrapping(SKTextLine textLine)
        {
            if (_textWrapping != TextWrapping.Wrap || textLine.LineMetrics.Size.Width <= _constraint.Width)
            {
                return new[] { textLine };
            }

            var textLines = new List<SKTextLine>();
            var availableLength = (float)_constraint.Width;
            var currentWidth = 0.0f;
            var runIndex = 0;
            var currentPosition = textLine.TextPointer.StartingIndex;

            while (runIndex < textLine.TextRuns.Count)
            {
                var currentRun = textLine.TextRuns[runIndex];

                currentWidth += currentRun.Width;

                if (currentWidth > availableLength)
                {
                    var measuredLength = BreakGlyphs(currentRun.GlyphRun, availableLength);

                    if (measuredLength < currentRun.Text.Length)
                    {
                        for (var i = measuredLength; i >= 0; i--)
                        {
                            var c = currentRun.Text.Span[i];

                            if (!char.IsWhiteSpace(c))
                            {
                                continue;
                            }

                            measuredLength = ++i;

                            break;
                        }
                    }

                    var splitResult = SplitTextRun(currentRun, 0, measuredLength);

                    var textRuns = new List<SKTextRun>();

                    if (runIndex > 0)
                    {
                        textRuns.AddRange(textLine.TextRuns.Take(runIndex));
                    }

                    if (splitResult.SecondTextRun != null)
                    {
                        textRuns.Add(splitResult.FirstTextRun);
                    }

                    var textLineMetrics = CreateTextLineMetrics(textRuns, out measuredLength);

                    textLines.Add(
                        new SKTextLine(new SKTextPointer(currentPosition, measuredLength), textRuns, textLineMetrics));

                    currentPosition += measuredLength;

                    var remainingTextRuns = new List<SKTextRun>(textLine.TextRuns);

                    var runCount = runIndex + 1;

                    while (runCount > 0)
                    {
                        remainingTextRuns.RemoveAt(0);

                        runCount--;
                    }

                    remainingTextRuns.Insert(0, splitResult.SecondTextRun ?? splitResult.FirstTextRun);

                    textLineMetrics = CreateTextLineMetrics(remainingTextRuns, out measuredLength);

                    textLine = new SKTextLine(
                        new SKTextPointer(currentPosition, measuredLength),
                        remainingTextRuns,
                        textLineMetrics);

                    availableLength = (float)_constraint.Width;

                    currentWidth = 0.0f;

                    runIndex = 0;
                }
                else
                {
                    availableLength -= currentRun.Width;

                    runIndex++;
                }
            }

            textLines.Add(textLine);

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
            if (length == 0 || textRun.Text.Length < 2)
            {
                return new SplitTextRunResult(textRun, null);
            }

            var firstTextRun = CreateTextRun(textRun, startingIndex, length);

            var secondTextRun = CreateTextRun(textRun, length, textRun.Text.Length - length);

            return new SplitTextRunResult(firstTextRun, secondTextRun);
        }

        /// <summary>
        /// Splits text runs at a specified length and retains all text properties.
        /// </summary>
        /// <param name="textRuns">The text runs.</param>
        /// <param name="length">The length of the first part.</param>
        /// <returns></returns>
        private SplitTextLineResult SplitTextRuns(IReadOnlyList<SKTextRun> textRuns, int length)
        {
            var firstTextRuns = new List<SKTextRun>();
            var secondTextRuns = new List<SKTextRun>();
            var currentPosition = 0;

            for (var runIndex = 0; runIndex < textRuns.Count; runIndex++)
            {
                var currentRun = textRuns[runIndex];

                if (textRuns.Count == 1 && currentRun.Text.Length == length)
                {
                    return new SplitTextLineResult(textRuns, null);
                }

                if (currentPosition + currentRun.Text.Length < length)
                {
                    currentPosition += currentRun.Text.Length;

                    continue;
                }

                if (currentPosition + currentRun.Text.Length == length)
                {
                    firstTextRuns.AddRange(textRuns.Take(runIndex + 1));

                    if (textRuns.Count == firstTextRuns.Count)
                    {
                        secondTextRuns = null;
                    }
                    else
                    {
                        secondTextRuns.AddRange(textRuns.Skip(firstTextRuns.Count));
                    }
                }
                else
                {
                    if (runIndex > 0)
                    {
                        firstTextRuns.AddRange(textRuns.Take(runIndex));
                    }

                    var splitResult = SplitTextRun(currentRun, 0, length - currentPosition);

                    firstTextRuns.Add(splitResult.FirstTextRun);

                    secondTextRuns.Add(splitResult.SecondTextRun);

                    if (runIndex < textRuns.Count - 1)
                    {
                        secondTextRuns.AddRange(textRuns.Skip(firstTextRuns.Count));
                    }
                }

                break;
            }

            return new SplitTextLineResult(firstTextRuns, secondTextRuns);
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

        private class SplitTextLineResult
        {
            public SplitTextLineResult(IReadOnlyList<SKTextRun> firstTextRuns, IReadOnlyList<SKTextRun> secondTextRuns)
            {
                FirstTextRuns = firstTextRuns;

                SecondTextRuns = secondTextRuns;
            }

            /// <summary>
            /// Gets the first text line.
            /// </summary>
            /// <value>
            /// The first text line.
            /// </value>
            public IReadOnlyList<SKTextRun> FirstTextRuns { get; }

            /// <summary>
            /// Gets the second text line.
            /// </summary>
            /// <value>
            /// The second text line.
            /// </value>
            public IReadOnlyList<SKTextRun> SecondTextRuns { get; }
        }
    }
}
