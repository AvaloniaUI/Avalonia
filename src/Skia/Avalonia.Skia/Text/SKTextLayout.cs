// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Avalonia.Media;

using SkiaSharp;
using SkiaSharp.HarfBuzz;

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
            if (span.Length < 1)
            {
                return;
            }

            var currentLength = 0;
            var appliedLength = 0;

            for (var lineIndex = 0; lineIndex < _textLines.Count; lineIndex++)
            {
                var currentTextLine = _textLines[lineIndex];

                if (currentTextLine.Length == 0)
                {
                    continue;
                }

                if (currentLength + currentTextLine.Length < span.StartIndex)
                {
                    currentLength += currentTextLine.Length;

                    continue;
                }

                var textRuns = new List<SKTextRun>(currentTextLine.TextRuns);

                for (var runIndex = 0; runIndex < textRuns.Count; runIndex++)
                {
                    bool needsUpdate;

                    var currentTextRun = textRuns[runIndex];

                    if (currentLength + currentTextRun.Text.Length < span.StartIndex)
                    {
                        currentLength += currentTextRun.Text.Length;

                        continue;
                    }

                    if (currentLength == span.StartIndex + appliedLength)
                    {
                        var splitLength = span.Length - appliedLength;

                        // Make sure we don't split a surrogate pair
                        if (splitLength < currentTextRun.Text.Length && char.IsSurrogatePair(
                                currentTextRun.Text[splitLength - 1],
                                currentTextRun.Text[splitLength]))
                        {
                            splitLength++;
                        }

                        if (splitLength >= currentTextRun.Text.Length)
                        {
                            // Apply to the whole run 
                            textRuns.RemoveAt(runIndex);

                            var updatedTextRun = ApplyTextSpan(span, currentTextRun, out needsUpdate);

                            appliedLength += updatedTextRun.Text.Length;

                            textRuns.Insert(runIndex, updatedTextRun);
                        }
                        else
                        {
                            // Apply at start of the run 
                            var start = SplitTextRun(currentTextRun, 0, splitLength);

                            textRuns.RemoveAt(runIndex);

                            var updatedTextRun = ApplyTextSpan(span, start.FirstTextRun, out needsUpdate);

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
                        if (char.IsHighSurrogate(currentTextRun.Text[splitLength - 1]))
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

                                var updatedTextRun = ApplyTextSpan(span, start.SecondTextRun, out needsUpdate);

                                appliedLength += updatedTextRun.Text.Length;

                                textRuns.Insert(runIndex, updatedTextRun);
                            }
                            else
                            {
                                // Make sure we don't split a surrogate pair
                                if ((splitWithinSurrogatePair && span.Length < 2)
                                    || char.IsHighSurrogate(start.SecondTextRun.Text[span.Length - 1]))
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

                                var updatedTextRun = ApplyTextSpan(span, end.FirstTextRun, out needsUpdate);

                                appliedLength += updatedTextRun.Text.Length;

                                textRuns.Insert(runIndex, updatedTextRun);

                                runIndex++;

                                textRuns.Insert(runIndex, end.SecondTextRun);
                            }
                        }
                        else
                        {
                            textRuns.RemoveAt(runIndex);

                            var updatedTextRun = ApplyTextSpan(span, currentTextRun, out needsUpdate);

                            textRuns.Insert(runIndex, updatedTextRun);
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

                    if (appliedLength >= span.Length)
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
            if (!TextLines.Any())
            {
                return;
            }

            _paint.TextEncoding = SKTextEncoding.GlyphId;

            var currentMatrix = canvas.TotalMatrix;

            canvas.Translate(origin.X, origin.Y);

            using (var foregroundWrapper = context.CreatePaint(foreground, Size))
            {
                foreach (var textLine in TextLines)
                {
                    var lineOffsetX = (float)GetTextLineOffsetX(_textAlignment, textLine.LineMetrics.Size.Width);

                    canvas.Translate(lineOffsetX, textLine.LineMetrics.BaselineOrigin.Y);

                    foreach (var textRun in textLine.TextRuns)
                    {
                        if (textRun.TextFormat.Typeface != null)
                        {
                            InitializePaintForTextRun(_paint, context, textLine, textRun, foregroundWrapper);

                            canvas.DrawPositionedText(textRun.Glyphs.GlyphIds, textRun.Glyphs.GlyphPositions, _paint);
                        }

                        canvas.Translate(textRun.Width, 0);
                    }

                    canvas.Translate(-textLine.LineMetrics.Size.Width, textLine.LineMetrics.Descent);
                }
            }

            canvas.SetMatrix(currentMatrix);
        }

        /// <summary>
        /// Hit tests the specified point.
        /// </summary>
        /// <param name="point">The point to hit test against.</param>
        /// <returns></returns>
        public TextHitTestResult HitTestPoint(Point point)
        {
            var pointY = (float)point.Y;

            var currentY = 0.0f;

            bool isTrailing;

            foreach (var textLine in TextLines)
            {
                if (pointY <= currentY + textLine.LineMetrics.Size.Height)
                {
                    var currentX = 0.0f;

                    var textPosition = textLine.StartingIndex;

                    foreach (var textRun in textLine.TextRuns)
                    {
                        if (currentX + textRun.Width < point.X)
                        {
                            currentX += textRun.Width;

                            textPosition += textRun.Text.Length;

                            continue;
                        }

                        foreach (var glyphCluster in textRun.Glyphs.GlyphClusters)
                        {
                            if (currentX + glyphCluster.Bounds.Width < point.X)
                            {
                                currentX += glyphCluster.Bounds.Width;

                                continue;
                            }

                            isTrailing = point.X - currentX > glyphCluster.Bounds.Width / 2;

                            var isInside = point.X > 0 && point.X <= textRun.Width;

                            return new TextHitTestResult
                            {
                                IsInside = isInside,
                                TextPosition = textPosition + glyphCluster.TextPosition,
                                Length = glyphCluster.Length,
                                Bounds = new Rect(currentX, currentY, glyphCluster.Bounds.Width, glyphCluster.Bounds.Height),
                                IsTrailing = isTrailing
                            };
                        }
                    }

                    if (point.X > currentX + textLine.LineMetrics.Size.Width && textLine.Length > 0)
                    {
                        var lastRun = textLine.TextRuns.LastOrDefault();

                        var lastCluster = lastRun?.Glyphs.GlyphClusters.LastOrDefault();

                        if (lastCluster != null)
                        {
                            isTrailing = _text.Length == lastCluster.TextPosition + lastCluster.Length;

                            return new TextHitTestResult
                            {
                                IsInside = false,
                                TextPosition = textLine.StartingIndex + lastCluster.TextPosition,
                                Length = lastCluster.Length,
                                IsTrailing = isTrailing
                            };
                        }
                    }
                }

                currentY += textLine.LineMetrics.Size.Height;
            }

            isTrailing = point.X > Size.Width || point.Y > Size.Height;

            if (isTrailing)
            {
                var lastCluster = TextLines.LastOrDefault()?.TextRuns.LastOrDefault()?.Glyphs.GlyphClusters.LastOrDefault();

                if (lastCluster != null)
                {
                    return new TextHitTestResult
                    {
                        IsInside = false,
                        IsTrailing = true,
                        TextPosition = lastCluster.TextPosition,
                        Length = lastCluster.Length
                    };
                }
            }

            return new TextHitTestResult
            {
                IsInside = false,
                IsTrailing = false
            };
        }

        /// <summary>
        /// Get the pixel location relative to the top-left of the layout box given the text position.
        /// </summary>
        /// <param name="textPosition">The text position.</param>
        /// <returns></returns>
        public Rect HitTestTextPosition(int textPosition)
        {
            var currentX = 0.0f;
            var currentY = 0.0f;

            foreach (var textLine in TextLines)
            {
                if (textLine.StartingIndex + textLine.Length < textPosition)
                {
                    currentY += textLine.LineMetrics.Size.Height;

                    continue;
                }

                foreach (var textRun in textLine.TextRuns)
                {
                    if (textLine.StartingIndex + textRun.Text.Length < textPosition)
                    {
                        currentX += textRun.Width;

                        continue;
                    }

                    foreach (var glyphCluster in textRun.Glyphs.GlyphClusters)
                    {
                        if (glyphCluster.TextPosition + glyphCluster.Length < textPosition)
                        {
                            currentX += glyphCluster.Bounds.Width;

                            continue;
                        }

                        return new Rect(currentX, currentY, glyphCluster.Bounds.Width, glyphCluster.Bounds.Height);
                    }
                }
            }

            return new Rect(0, 0, Size.Width, Size.Height);
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

            var currentY = 0.0f;

            foreach (var textLine in TextLines)
            {
                if (textLine.StartingIndex + textLine.Length > textPosition)
                {
                    var currentX = 0.0f;
                    var runIndex = 0;

                    while (textLength > 0 && runIndex < textLine.TextRuns.Count)
                    {
                        var currentTextRun = textLine.TextRuns[runIndex];

                        if (textLine.StartingIndex + currentTextRun.Text.Length > textPosition && textLength > 0)
                        {
                            var clusterIndex = 0;

                            while (textLength > 0 && clusterIndex < currentTextRun.Glyphs.GlyphClusters.Count)
                            {
                                var glyphCluster = currentTextRun.Glyphs.GlyphClusters[clusterIndex];

                                if (glyphCluster.TextPosition >= textPosition && textLength > 0)
                                {
                                    result.Add(
                                        new Rect(
                                            currentX,
                                            currentY,
                                            glyphCluster.Bounds.Width,
                                            textLine.LineMetrics.Size.Height));

                                    textPosition += glyphCluster.Length;

                                    textLength -= glyphCluster.Length;
                                }

                                currentX += glyphCluster.Bounds.Width;

                                clusterIndex++;
                            }
                        }

                        currentX += currentTextRun.Width;

                        runIndex++;
                    }
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
                textRun.Glyphs,
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
        /// <param name="c">The character.</param>
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
                                if (index < _text.Length - 1 && _text[index + 1] == '\n')
                                {
                                    index++;
                                }

                                var length = index - currentPosition + 1;

                                var breakLines = PerformLineBreak(_text, currentPosition, length);

                                textLines.AddRange(breakLines);

                                currentPosition = index + 1;
                                break;
                            }

                        case '\n':
                            {
                                if (index < _text.Length - 1 && _text[index + 1] == '\r')
                                {
                                    index++;
                                }

                                var length = index - currentPosition + 1;

                                var breakLines = PerformLineBreak(_text, currentPosition, length);

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

                var fontMetrics = _paint.FontMetrics;

                var textLineMetrics = new SKTextLineMetrics(0, fontMetrics.Ascent, fontMetrics.Descent, fontMetrics.Leading);

                return new SKTextLine(startingIndex, length, new List<SKTextRun>(), textLineMetrics);
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

            using (var shaper = new SKShaper(textFormat.Typeface))
            {
                var result = shaper.Shape(text, _paint);

                var glyphsIds = result.Codepoints.Select(cp => BitConverter.GetBytes((ushort)cp)).SelectMany(b => b).ToArray();

                var glyphClusters = CreateGlyphClusters(text, glyphsIds, result.Clusters, result.Points);

                var glyphs = new SKGlyphRun(glyphsIds, result.Points, glyphClusters);

                var width = glyphs.GlyphClusters.Sum(x => x.Bounds.Width);

                return new SKTextRun(text, glyphs, textFormat, fontMetrics, width);
            }
        }

        private List<SKGlyphCluster> CreateGlyphClusters(string text, byte[] glyphsIds, uint[] clusters, IReadOnlyList<SKPoint> points)
        {
            var glyphClusters = new List<SKGlyphCluster>();

            _paint.TextEncoding = SKTextEncoding.GlyphId;

            var height = _paint.FontMetrics.Descent - _paint.FontMetrics.Ascent + _paint.FontMetrics.Leading;

            var current = 0;

            var lastIndex = clusters.Length - 1;

            while (current <= lastIndex)
            {
                // ToDo: Need a custom implementation that searches for the next cluster.
                var next = Array.BinarySearch(clusters, clusters[current] + 1);

                if (next < 0)
                {
                    next = ~next;
                }

                var width = 0.0f;

                for (var index = current; index < next; index++)
                {
                    var byteIndex = index * 2;

                    var bufferHandle = GCHandle.Alloc(glyphsIds, GCHandleType.Pinned);
                    var bufferPtr = Marshal.UnsafeAddrOfPinnedArrayElement(glyphsIds, byteIndex);

                    var measuredWidth = _paint.MeasureText(bufferPtr, new IntPtr(2));

                    bufferHandle.Free();

                    var glyphWidth = points[index].X + measuredWidth;

                    if (width < glyphWidth)
                    {
                        width = glyphWidth;
                    }
                }

                var point = points[current];

                var rect = new SKRect(point.X, point.Y, width, height);

                var length = next - current;

                if (current == lastIndex)
                {
                    length = text.Length - current;
                }

                var clusterInfo = new SKGlyphCluster(current, length, rect);

                glyphClusters.Add(clusterInfo);

                current = next;
            }

            return glyphClusters;
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
            var textPosition = 0;

            var runText = text.Substring(startingIndex, length);

            while (textPosition < length)
            {
                var glyphCount = Math.Min(runText.Length, _typeface.CountGlyphs(runText));

                if (glyphCount < runText.Length && IsBreakChar(runText[glyphCount]))
                {
                    var c = runText[glyphCount];

                    glyphCount++;

                    if (glyphCount < runText.Length)
                    {
                        // Make sure we don't split a combination of \r and \n
                        if (c == '\r' && runText[glyphCount] == '\n')
                        {
                            glyphCount++;
                        }

                        if (c == '\n' && runText[glyphCount] == '\r')
                        {
                            glyphCount++;
                        }
                    }
                }

                var typeface = _typeface;

                if (glyphCount == 0)
                {
                    var codePoint = char.ConvertToUtf32(runText, 0);

                    typeface = SKFontManager.Default.MatchCharacter(codePoint);

                    if (codePoint > sizeof(short))
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
                            var c = runText[glyphCount];

                            if (char.IsWhiteSpace(c) || IsZeroSpace(c))
                            {
                                glyphCount++;

                                continue;
                            }

                            if (char.IsHighSurrogate(c))
                            {
                                var lowSurrogate = runText[glyphCount + 1];

                                var bytes = Encoding.Unicode.GetBytes(new[] { c, lowSurrogate });

                                var symbol = Encoding.Unicode.GetString(bytes);

                                // ToDo: Use CharsToGlyphs when new SkiaSharp is released
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

                if (textPosition + glyphCount < length)
                {
                    runText = text.Substring(startingIndex + textPosition, glyphCount);
                }

                var currentRun = CreateTextRun(runText, new SKTextFormat(typeface, _fontSize));

                textRuns.Add(currentRun);

                textPosition += glyphCount;

                if (textPosition != length)
                {
                    runText = text.Substring(startingIndex + textPosition);
                }
            }

            textLineMetrics = CreateTextLineMetrics(textRuns, out _);

            return textRuns;
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
                var currentPosition = startingIndex;

                while (runIndex < textLine.TextRuns.Count)
                {
                    var textRun = textLine.TextRuns[runIndex];

                    currentWidth += textRun.Width;

                    if (currentWidth > availableLength)
                    {
                        var measuredLength = BreakGlyphs(textRun.Glyphs, availableLength);

                        if (measuredLength < textRun.Text.Length)
                        {
                            var hasFoundWordStart = false;

                            for (var i = measuredLength; i > 0; i--)
                            {
                                var c = textRun.Text[i];

                                if (char.IsWhiteSpace(c))
                                {
                                    measuredLength = ++i;

                                    hasFoundWordStart = true;

                                    break;
                                }
                            }

                            if (!hasFoundWordStart)
                            {
                                measuredLength = 0;
                            }
                        }

                        var splitResult = SplitTextRun(textLine.TextRuns[runIndex], 0, measuredLength);

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

                        textLines.Add(new SKTextLine(currentPosition, measuredLength, textRuns, textLineMetrics));

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

                        textLine = new SKTextLine(currentPosition, measuredLength, remainingTextRuns, textLineMetrics);

                        availableLength = (float)_constraint.Width;

                        currentWidth = 0.0f;

                        runIndex = 0;
                    }
                    else
                    {
                        availableLength -= textRun.Width;

                        runIndex++;
                    }
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
