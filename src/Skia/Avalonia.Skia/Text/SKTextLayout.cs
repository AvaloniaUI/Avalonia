// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Media;

using HarfBuzzSharp;

using SkiaSharp;

using Buffer = HarfBuzzSharp.Buffer;

namespace Avalonia.Skia.Text
{
    internal class SKTextLayout
    {
        private static readonly char[] s_ellipsis = { '\u2026' };

        private readonly SKTypeface _typeface;

        private readonly float _fontSize;

        private readonly TextAlignment _textAlignment;

        private readonly TextWrapping _textWrapping;

        private readonly TextTrimming _textTrimming;

        private readonly Size _constraint;

        private readonly SKPaint _paint;

        private readonly int _textLength;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SKTextLayout" /> class.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="typeface">The typeface.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="textAlignment">The text alignment.</param>
        /// <param name="textWrapping">The text wrapping.</param>
        /// <param name="textTrimming">The text trimming.</param>
        /// <param name="constraint">The constraint.</param>
        /// <param name="spans">The spans.</param>
        public SKTextLayout(
            string text,
            SKTypeface typeface,
            float fontSize,
            TextAlignment textAlignment,
            TextWrapping textWrapping,
            TextTrimming textTrimming,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans = null)
        {
            _typeface = typeface;
            _fontSize = fontSize;
            _textAlignment = textAlignment;
            _textWrapping = textWrapping;
            _textTrimming = textTrimming;
            _constraint = constraint;
            _paint = CreatePaint(typeface, fontSize);
            _textLength = text.Length;
            TextLines = CreateTextLines(text.AsSpan(), spans);
            Bounds = CreateLayoutBounds(TextLines);
        }

        /// <summary>
        ///     Gets the text lines.
        /// </summary>
        /// <value>
        ///     The text lines.
        /// </value>
        public IReadOnlyList<SKTextLine> TextLines { get; }

        /// <summary>
        ///     Gets the text metrics of the layout.
        /// </summary>
        /// <value>
        ///     The size.
        /// </value>
        public SKRect Bounds { get; }

        /// <summary>
        ///     Draws the layout.
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

            using (var foregroundWrapper = context.CreatePaint(foreground, new Size(Bounds.Width, Bounds.Height)))
            {
                foreach (var textLine in TextLines)
                {
                    var baselineOrigin = textLine.LineMetrics.BaselineOrigin;

                    canvas.Translate(baselineOrigin.X, baselineOrigin.Y);

                    foreach (var textRun in textLine.TextRuns)
                    {
#if DEBUG_TEXTLAYOUT
                        // _paint.StrokeWidth = 0.5f;

                        var posX = textLine.LineMetrics.BaselineOrigin.X;
                        var posY = textLine.LineMetrics.BaselineOrigin.Y;

                        canvas.Translate(0, -textLine.LineMetrics.BaselineOrigin.Y);

                        // Overline
                        canvas.DrawLine(
                            new SKPoint(posX, posY + textRun.FontMetrics.Ascent), 
                            new SKPoint(posX + textRun.Width, posY + textRun.FontMetrics.Ascent),
                            _paint);                      

                        // Strikeout
                        var strikeoutPosY = posY + textRun.FontMetrics.StrikeoutPosition ?? 0;
                        var strikeoutRect =
 new SKRect(posX, strikeoutPosY, posX + textRun.Width, strikeoutPosY + textRun.FontMetrics.StrikeoutThickness ?? 1);

                        canvas.DrawRect(strikeoutRect, _paint);

                        // Base line
                        canvas.DrawLine(
                            textLine.LineMetrics.BaselineOrigin,
                            new SKPoint(posX + textRun.Width, posY),
                            _paint);

                        // Underline
                        var underlinePosY = posY + textRun.FontMetrics.UnderlinePosition ?? 0;
                        var underlineRect =
 new SKRect(posX, underlinePosY, posX + textRun.Width, underlinePosY + textRun.FontMetrics.UnderlineThickness ?? 1);

                        canvas.DrawRect(underlineRect, _paint);

                        // Bounds
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

                                // This expects an byte array so we need to pass the right length
                                canvas.DrawPositionedText(
                                    p,
                                    textRun.GlyphRun.GlyphIndices.Length * 2,
                                    textRun.GlyphRun.GlyphOffsets,
                                    _paint);
                            }
                        }

                        canvas.Translate(textRun.Width, 0);
                    }

                    canvas.Translate(-textLine.LineMetrics.Size.Width, textLine.LineMetrics.Descent);
                }
            }

            canvas.SetMatrix(currentMatrix);
        }

#if DEBUG_TEXTLAYOUT
        private static readonly Random s_random = new Random();

        private static Color GetRandomColor()
        {
            return Color.FromArgb(128, (byte)s_random.Next(256), (byte)s_random.Next(256), (byte)s_random.Next(256));
        }
#endif

        /// <summary>
        ///     Hit tests the specified point.
        /// </summary>
        /// <param name="point">The point to hit test against.</param>
        /// <returns></returns>
        public TextHitTestResult HitTestPoint(Point point)
        {
            if (_textLength == 0)
            {
                return new TextHitTestResult();
            }

            var pointY = (float)point.Y;

            var currentY = 0.0f;

            bool isTrailing;

            foreach (var textLine in TextLines)
            {
                if (pointY < currentY + textLine.LineMetrics.Size.Height)
                {
                    var currentX = textLine.LineMetrics.BaselineOrigin.X;

                    foreach (var textRun in textLine.TextRuns)
                    {
                        if (currentX + textRun.Width < point.X)
                        {
                            currentX += textRun.Width;

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

                            return new TextHitTestResult
                            {
                                IsInside = isInside,
                                TextPosition = glyphCluster.TextPosition,
                                Length = glyphCluster.Length,
                                Bounds = new Rect(
                                           currentX,
                                           currentY,
                                           glyphCluster.Bounds.Width,
                                           glyphCluster.Bounds.Height),
                                IsTrailing = isTrailing
                            };
                        }
                    }

                    if (point.X > currentX && textLine.TextPointer.Length > 0)
                    {
                        var textRun = textLine.TextRuns.LastOrDefault();

                        var glyphCluster = textRun?.GlyphRun.GlyphClusters.LastOrDefault();

                        if (glyphCluster != null)
                        {
                            isTrailing = _textLength == glyphCluster.TextPosition + glyphCluster.Length;

                            return new TextHitTestResult
                            {
                                IsInside = false,
                                IsTrailing = isTrailing,
                                TextPosition = glyphCluster.TextPosition,
                                Length = glyphCluster.Length,
                            };
                        }
                    }
                }

                currentY += textLine.LineMetrics.Size.Height;
            }

            isTrailing = point.X > Bounds.Width || point.Y > Bounds.Height;

            var lastLine = TextLines.Last();

            var lastRun = lastLine.TextRuns.Last();

            var lastCluster = lastRun.GlyphRun.GlyphClusters.Last();

            return new TextHitTestResult
            {
                IsInside = false,
                IsTrailing = true,
                TextPosition = isTrailing ? _textLength - lastCluster.Length : 0,
                Length = lastCluster.Length
            };
        }

        /// <summary>
        ///     Get the pixel location relative to the top-left of the layout box given the text position.
        /// </summary>
        /// <param name="textPosition">The text position.</param>
        /// <returns></returns>
        public Rect HitTestTextPosition(int textPosition)
        {
            if (!TextLines.Any())
            {
                return new Rect();
            }

            if (textPosition < 0 || textPosition >= _textLength)
            {
                var lastLine = TextLines.Last();

                var offsetX = lastLine.LineMetrics.BaselineOrigin.X;

                var lineX = offsetX + lastLine.LineMetrics.Size.Width;

                var lineY = Bounds.Height - lastLine.LineMetrics.Size.Height;

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

                var currentX = textLine.LineMetrics.BaselineOrigin.X;

                foreach (var textRun in textLine.TextRuns)
                {
                    if (textRun.TextPointer.StartingIndex + textRun.TextPointer.Length - 1 < textPosition)
                    {
                        currentX += textRun.Width;

                        continue;
                    }

                    foreach (var glyphCluster in textRun.GlyphRun.GlyphClusters)
                    {
                        if (glyphCluster.TextPosition + glyphCluster.Length - 1 < textPosition)
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
        ///     Get a set of hit-test rectangles corresponding to a range of text positions.
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

                var lineX = textLine.LineMetrics.BaselineOrigin.X;
                var startX = -1f;

                foreach (var textRun in textLine.TextRuns)
                {
                    if (textRun.TextPointer.StartingIndex + textRun.TextPointer.Length - 1 < textPosition)
                    {
                        lineX += textRun.Width;

                        continue;
                    }

                    foreach (var glyphCluster in textRun.GlyphRun.GlyphClusters)
                    {
                        if (glyphCluster.TextPosition < textPosition)
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
        ///     Creates the paint.
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
        ///     Initializes the paint for text run.
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
        ///     Determines whether [c] is a break char.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns>
        ///     <c>true</c> if [is break character] [the specified c]; otherwise, <c>false</c>.
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
        ///     Determines whether [c] is a zero space char.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns>
        ///     <c>true</c> if [is zero space character] [the specified c]; otherwise, <c>false</c>.
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

        private static Blob GetHarfBuzzBlob(SKStreamAsset asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            var size = asset.Length;

            Blob blob;

            var memoryBase = asset.GetMemoryBase();

            if (memoryBase != IntPtr.Zero)
            {
                blob = new Blob(memoryBase, size, MemoryMode.ReadOnly, asset, p => ((SKStreamAsset)p).Dispose());
            }
            else
            {
                var ptr = Marshal.AllocCoTaskMem(size);

                asset.Read(ptr, size);

                blob = new Blob(ptr, size, MemoryMode.ReadOnly, ptr, p => Marshal.FreeCoTaskMem((IntPtr)p));
            }

            blob.MakeImmutable();

            return blob;
        }

        private static IReadOnlyList<SKGlyphCluster> CreateGlyphClusters(
            Buffer buffer,
            SKTextPointer textPointer,
            SKTextFormat textFormat,
            SKFontMetrics fontMetrics,
            out ushort[] glyphIndices,
            out SKPoint[] glyphPositions,
            out float width)
        {
            Font font;

            using (var blob = GetHarfBuzzBlob(textFormat.Typeface.OpenStream(out var index)))
            using (var face = new Face(blob, index))
            {
                face.Index = index;

                face.UnitsPerEm = textFormat.Typeface.UnitsPerEm;

                font = new Font(face);

                font.SetFunctionsOpenType();
            }

            font.Shape(buffer);

            var len = buffer.Length;

            var info = buffer.GetGlyphInfoReferences();

            var pos = buffer.GetGlyphPositionReferences();

            font.GetScale(out var scaleX, out _);

            var textScale = textFormat.FontSize / scaleX;

            glyphPositions = new SKPoint[len];

            var glyphAdvances = new float[len];

            var clusters = new int[len];

            glyphIndices = new ushort[len];

            var currentX = 0.0f;
            var currentY = 0.0f;

            for (var i = 0; i < len; i++)
            {
                glyphIndices[i] = (ushort)info[i].Codepoint;

                clusters[i] = (int)info[i].Cluster;

                var offsetX = pos[i].XOffset * textScale;
                var offsetY = pos[i].YOffset * textScale;

                glyphPositions[i] = new SKPoint(currentX + offsetX, currentY + offsetY);

                var advanceX = pos[i].XAdvance * textScale;
                var advanceY = pos[i].YAdvance * textScale;

                glyphAdvances[i] = advanceX;

                currentX += advanceX;
                currentY += advanceY;
            }

            width = currentX;

            return CreateGlyphClusters(textPointer, fontMetrics, clusters, glyphAdvances, glyphPositions);
        }

        /// <summary>
        ///     Creates the glyph clusters.
        /// </summary>
        /// <param name="textPointer"></param>
        /// <param name="fontMetrics"></param>
        /// <param name="clusters">The clusters.</param>
        /// <param name="glyphAdvances"></param>
        /// <param name="glyphPositions">The glyph offsets.</param>
        /// <returns></returns>
        private static IReadOnlyList<SKGlyphCluster> CreateGlyphClusters(
            SKTextPointer textPointer,
            SKFontMetrics fontMetrics,
            int[] clusters,
            IReadOnlyList<float> glyphAdvances,
            IReadOnlyList<SKPoint> glyphPositions)
        {
            var glyphClusters = new List<SKGlyphCluster>();

            var height = fontMetrics.Descent - fontMetrics.Ascent + fontMetrics.Leading;

            var currentCluster = 0;

            var lastCluster = clusters.Length - 1;

            while (currentCluster <= lastCluster)
            {
                var currentPosition = clusters[currentCluster];

                // ToDo: Need a custom implementation that searches for the next cluster.
                var nextCluster = Array.BinarySearch(clusters, currentPosition + 1);

                if (nextCluster < 0)
                {
                    nextCluster = ~nextCluster;
                }

                int length;

                if (nextCluster > lastCluster || currentCluster == lastCluster)
                {
                    length = textPointer.StartingIndex + textPointer.Length - currentPosition;
                }
                else
                {
                    var nextPosition = clusters[nextCluster];

                    length = nextPosition - currentPosition;
                }

                var clusterWidth = 0f;

                for (var clusterIndex = currentCluster; clusterIndex < nextCluster; clusterIndex++)
                {
                    clusterWidth += glyphAdvances[clusterIndex];
                }

                var point = glyphPositions[currentCluster];

                var rect = new SKRect(point.X, point.Y, point.X + clusterWidth, point.Y + height);

                glyphClusters.Add(new SKGlyphCluster(currentPosition, length, rect));

                currentCluster = nextCluster;
            }

            return glyphClusters;
        }

        /// <summary>
        ///     Gets the line break position that is indicated by a unicode break char.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="textRun">The text run.</param>
        /// <returns></returns>
        private static int GetLineBreakPosition(ReadOnlySpan<char> text, SKTextRun textRun)
        {
            var length = textRun.TextPointer.StartingIndex + textRun.TextPointer.Length;

            for (var index = textRun.TextPointer.StartingIndex; index < length; index++)
            {
                var c = text[index];

                if (!IsBreakChar(c))
                {
                    continue;
                }

                if (index < length - 1)
                {
                    switch (c)
                    {
                        case '\r' when text[index + 1] == '\n':
                        case '\n' when text[index + 1] == '\r':
                            return ++index;
                    }
                }

                return index;
            }

            return -1;
        }

        /// <summary>Creates the layout bounds.</summary>
        /// <param name="textLines">The text lines.</param>
        /// <returns>Bounds</returns>
        private static SKRect CreateLayoutBounds(IEnumerable<SKTextLine> textLines)
        {
            float left = 0.0f, right = 0.0f, bottom = 0.0f;

            foreach (var textLine in textLines)
            {
                if (right < textLine.LineMetrics.BaselineOrigin.X + textLine.LineMetrics.Size.Width)
                {
                    right = textLine.LineMetrics.BaselineOrigin.X + textLine.LineMetrics.Size.Width;
                }

                if (left < textLine.LineMetrics.BaselineOrigin.X)
                {
                    left = textLine.LineMetrics.BaselineOrigin.X;
                }

                bottom += textLine.LineMetrics.Size.Height;
            }

            return new SKRect(left, 0, right, bottom);
        }

        /// <summary>
        /// Creates an ellipsis.
        /// </summary>
        /// <param name="textFormat">The text format.</param>
        /// <param name="foreground">The foreground.</param>
        /// <returns></returns>
        private SKTextRun CreateEllipsisRun(SKTextFormat textFormat, IBrush foreground)
        {
            return CreateTextRun( s_ellipsis, new SKTextPointer(0, 1), textFormat, foreground, true);
        }

        /// <summary>
        ///     Creates the text line metrics.
        /// </summary>
        /// <param name="textRuns">The text runs.</param>
        /// <param name="length">Text length</param>
        /// <returns></returns>
        private SKTextLineMetrics CreateTextLineMetrics(IEnumerable<SKTextRun> textRuns, out int length)
        {
            var width = 0.0f;
            var ascent = 0.0f;
            var descent = 0.0f;
            var leading = 0.0f;

            length = 0;

            foreach (var textRun in textRuns)
            {
                length += textRun.TextPointer.Length;

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

            var xOrigin = GetTextLineOffsetX(width);

            return new SKTextLineMetrics(width, xOrigin, ascent, descent, leading);
        }

        /// <summary>
        ///     Creates a new text line of specified text runs.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="textRuns">The text runs.</param>
        /// <param name="startingIndex">The starting index.</param>
        /// <returns></returns>
        private SKTextLine CreateShapedTextLine(
            ReadOnlySpan<char> text,
            IEnumerable<SKTextRun> textRuns,
            int startingIndex)
        {
            var shapedTextRuns = new List<SKTextRun>();

            foreach (var textRun in textRuns)
            {
                if (textRun.GlyphRun == null)
                {
                    var shapedRun = CreateTextRun(
                        text,
                        textRun.TextPointer,
                        textRun.TextFormat,
                        textRun.Foreground,
                        true);

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
        ///     Creates a empty text line.
        /// </summary>
        /// <returns></returns>
        private SKTextLine CreateEmptyTextLine()
        {
            _paint.Typeface = _typeface;

            _paint.TextSize = _fontSize;

            var fontMetrics = _paint.FontMetrics;

            var textLineMetrics = new SKTextLineMetrics(
                0f,
                0f,
                fontMetrics.Ascent,
                fontMetrics.Descent,
                fontMetrics.Leading);

            return new SKTextLine(new SKTextPointer(), new List<SKTextRun>(), textLineMetrics);
        }

        /// <summary>
        ///     Applies a text style span to the layout.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="textLines">The text lines to apply the text style span to.</param>
        /// <param name="span">The text style span.</param>
        private void ApplyTextStyleSpan(
            ReadOnlySpan<char> text,
            IList<SKTextLine> textLines,
            FormattedTextStyleSpan span)
        {
            if (span.Length < 1)
            {
                return;
            }

            var currentPosition = 0;
            var appliedLength = 0;

            for (var lineIndex = 0; lineIndex < textLines.Count; lineIndex++)
            {
                var currentTextLine = textLines[lineIndex];

                if (currentTextLine.TextPointer.Length == 0)
                {
                    continue;
                }

                if (currentPosition + currentTextLine.TextPointer.Length - 1 < span.StartIndex)
                {
                    currentPosition += currentTextLine.TextPointer.Length;

                    continue;
                }

                var textRuns = new List<SKTextRun>(currentTextLine.TextRuns);

                for (var runIndex = 0; runIndex < textRuns.Count; runIndex++)
                {
                    bool needsUpdate;

                    var currentTextRun = textRuns[runIndex];

                    var currentText = text.Slice(
                        currentTextRun.TextPointer.StartingIndex,
                        currentTextRun.TextPointer.Length);

                    if (currentTextRun.TextPointer.Length == 0)
                    {
                        continue;
                    }

                    if (currentPosition + currentTextRun.TextPointer.Length - 1 < span.StartIndex)
                    {
                        currentPosition += currentTextRun.TextPointer.Length;

                        continue;
                    }

                    if (currentPosition == span.StartIndex + appliedLength)
                    {
                        var splitLength = span.Length - appliedLength;

                        // Make sure we don't split a surrogate pair
                        if (splitLength < currentTextRun.TextPointer.Length && char.IsSurrogatePair(
                                currentText[splitLength - 1],
                                currentText[splitLength]))
                        {
                            splitLength++;
                        }

                        if (splitLength >= currentTextRun.TextPointer.Length)
                        {
                            // Apply to the whole run 
                            textRuns.RemoveAt(runIndex);

                            var updatedTextRun = ApplyTextStyleSpan(text, span, currentTextRun, out needsUpdate);

                            appliedLength += updatedTextRun.TextPointer.Length;

                            textRuns.Insert(runIndex, updatedTextRun);
                        }
                        else
                        {
                            // Apply at start of the run 
                            var start = SplitTextRun(text, currentTextRun, splitLength);

                            textRuns.RemoveAt(runIndex);

                            var updatedTextRun = ApplyTextStyleSpan(text, span, start.FirstTextRun, out needsUpdate);

                            appliedLength += updatedTextRun.TextPointer.Length;

                            textRuns.Insert(runIndex, updatedTextRun);

                            runIndex++;

                            textRuns.Insert(runIndex, start.SecondTextRun);
                        }
                    }
                    else
                    {
                        var splitLength = Math.Min(
                            span.StartIndex + appliedLength - currentPosition,
                            currentTextRun.TextPointer.Length);

                        if (splitLength > 0)
                        {
                            var start = SplitTextRun(text, currentTextRun, splitLength);

                            if (splitLength + span.Length - appliedLength >= currentTextRun.TextPointer.Length)
                            {
                                // Apply at the end of the run      
                                textRuns.RemoveAt(runIndex);

                                textRuns.Insert(runIndex, start.FirstTextRun);

                                runIndex++;

                                var updatedTextRun = ApplyTextStyleSpan(
                                    text,
                                    span,
                                    start.SecondTextRun,
                                    out needsUpdate);

                                appliedLength += updatedTextRun.TextPointer.Length;

                                textRuns.Insert(runIndex, updatedTextRun);
                            }
                            else
                            {
                                splitLength = span.Length;

                                // Apply in between the run
                                var end = SplitTextRun(text, start.SecondTextRun, splitLength);

                                textRuns.RemoveAt(runIndex);

                                textRuns.Insert(runIndex, start.FirstTextRun);

                                runIndex++;

                                var updatedTextRun = ApplyTextStyleSpan(text, span, end.FirstTextRun, out needsUpdate);

                                appliedLength += updatedTextRun.TextPointer.Length;

                                textRuns.Insert(runIndex, updatedTextRun);

                                runIndex++;

                                textRuns.Insert(runIndex, end.SecondTextRun);
                            }
                        }
                        else
                        {
                            textRuns.RemoveAt(runIndex);

                            var updatedTextRun = ApplyTextStyleSpan(text, span, currentTextRun, out needsUpdate);

                            textRuns.Insert(runIndex, updatedTextRun);
                        }
                    }

                    textLines.RemoveAt(lineIndex);

                    if (needsUpdate)
                    {
                        currentTextLine = CreateShapedTextLine(
                            text,
                            textRuns,
                            currentTextLine.TextPointer.StartingIndex);
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

                    currentPosition += currentTextRun.TextPointer.Length;
                }
            }
        }

        /// <summary>
        ///     Applies the text style span to a text run.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="span">The text span.</param>
        /// <param name="textRun">The text run.</param>
        /// <param name="needsUpdate">Indicates whether the layout needs to update measures.</param>
        /// <returns></returns>
        private SKTextRun ApplyTextStyleSpan(
            ReadOnlySpan<char> text,
            FormattedTextStyleSpan span,
            SKTextRun textRun,
            out bool needsUpdate)
        {
            var textPointer = textRun.TextPointer;
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
                    typeFace = TypefaceCache.GetSKTypeface(span.Typeface);
                }

                textFormat = new SKTextFormat(typeFace, (float)fontSize);

                needsUpdate = true;

                return CreateTextRun(text, textPointer, textFormat, drawingEffect, true);
            }

            needsUpdate = false;

            return new SKTextRun(textPointer, glyphRun, textFormat, fontMetrics, width, drawingEffect);
        }

        /// <summary>
        ///     Gets the text line offset x.
        /// </summary>
        /// <param name="lineWidth">The line width.</param>
        /// <returns></returns>
        private float GetTextLineOffsetX(float lineWidth)
        {
            var availableWidth = _constraint.Width > 0 && !double.IsPositiveInfinity(_constraint.Width)
                                     ? (float)_constraint.Width
                                     : Bounds.Width;

            switch (_textAlignment)
            {
                case TextAlignment.Center:
                    return (availableWidth - lineWidth) / 2;
                case TextAlignment.Right:
                    return availableWidth - lineWidth;
                default:
                    return 0.0f;
            }
        }

        /// <summary>
        ///     Creates the initial text lines.
        /// </summary>
        /// <returns></returns>
        private List<SKTextLine> CreateTextLines(ReadOnlySpan<char> text, IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            if (text.Length == 0)
            {
                var emptyTextLine = CreateEmptyTextLine();

                return new List<SKTextLine>
                       {
                           emptyTextLine
                       };
            }

            var currentTextRuns = CreateTextRuns(text);

            var textLines = new List<SKTextLine>();

            var currentPosition = 0;

            while (currentTextRuns != null)
            {
                var length = 0;

                foreach (var textRun in currentTextRuns)
                {
                    var lineBreakPosition = GetLineBreakPosition(text, textRun);

                    if (lineBreakPosition == -1)
                    {
                        if (currentPosition + length + textRun.TextPointer.Length == text.Length)
                        {
                            var textLine = CreateShapedTextLine(text, currentTextRuns, currentPosition);

                            var textWrappingResult = BreakTextLine(text, textLine);

                            textLines.AddRange(textWrappingResult);

                            currentTextRuns = null;

                            break;
                        }

                        length += textRun.TextPointer.Length;
                    }
                    else
                    {
                        length += lineBreakPosition - currentPosition + 1;

                        var splitResult = SplitTextRuns(text, currentTextRuns, length);

                        var textLine = CreateShapedTextLine(text, splitResult.FirstTextRuns, currentPosition);

                        var textWrappingResult = BreakTextLine(text, textLine);

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
                    ApplyTextStyleSpan(text, textLines, textStyleSpan);
                }
            }

            return textLines;
        }

        /// <summary>
        ///     Creates a text run with a specific text format.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="textPointer">The text pointer.</param>
        /// <param name="textFormat">The text format.</param>
        /// <param name="foreground">The foreground brush.</param>
        /// <param name="shapeRun">Indicates whether the run should get shaped or not.</param>
        /// <returns></returns>
        private SKTextRun CreateTextRun(
            ReadOnlySpan<char> text,
            SKTextPointer textPointer,
            SKTextFormat textFormat,
            IBrush foreground = null,
            bool shapeRun = false)
        {
            _paint.Typeface = textFormat.Typeface;

            _paint.TextSize = textFormat.FontSize;

            var fontMetrics = _paint.FontMetrics;

            if (textPointer.Length == 0)
            {
                return CreateEmptyTextRun(textFormat);
            }

            if (!shapeRun)
            {
                return new SKTextRun(textPointer, null, textFormat, fontMetrics, 0, foreground);
            }

            using (var buffer = new Buffer())
            {
                buffer.Language = new Language(CultureInfo.CurrentCulture);

                var breakCharCount = 0;

                if (textPointer.Length >= 2)
                {
                    var lastPosition = textPointer.StartingIndex + textPointer.Length - 1;

                    if (IsBreakChar(text[lastPosition]))
                    {
                        if ((text[lastPosition] == '\r' && text[lastPosition - 1] == '\n')
                            || (text[lastPosition] == '\n' && text[lastPosition - 1] == '\r'))
                        {
                            breakCharCount = 2;
                        }
                        else
                        {
                            breakCharCount = 1;
                        }
                    }
                }

                // Replace break char with zero width space
                if (breakCharCount > 0)
                {
                    buffer.AddUtf16(text, textPointer.StartingIndex, textPointer.Length - breakCharCount);

                    var cluster = buffer.GlyphInfos.Length > 0
                                      ? buffer.GlyphInfos[buffer.Length - 1].Cluster + 1
                                      : (uint)textPointer.StartingIndex;

                    for (var i = 0; i < breakCharCount; i++)
                    {
                        buffer.Add('\u200C', cluster);
                    }
                }
                else
                {
                    buffer.AddUtf16(text, textPointer.StartingIndex, textPointer.Length);
                }

                buffer.GuessSegmentProperties();

                var glyphClusters = CreateGlyphClusters(
                    buffer,
                    textPointer,
                    textFormat,
                    fontMetrics,
                    out var glyphIndices,
                    out var glyphPositions,
                    out var width);

                var glyphs = new SKGlyphRun(glyphIndices, glyphPositions, glyphClusters);

                return new SKTextRun(textPointer, glyphs, textFormat, fontMetrics, width, foreground);
            }
        }

        /// <summary>
        ///     Creates the empty text run.
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

            return new SKTextRun(new SKTextPointer(), glyphs, textFormat, fontMetrics, 0);
        }

        /// <summary>
        ///     Creates a list of text runs. Each text run only consists of one combination of text properties.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>A list of text runs.</returns>
        private unsafe IReadOnlyList<SKTextRun> CreateTextRuns(ReadOnlySpan<char> text)
        {
            var textRuns = new List<SKTextRun>();
            var textPosition = 0;

            var runText = text;

            while (textPosition < text.Length)
            {
                int glyphCount;
                var typeface = _typeface;

                fixed (char* chars = runText)
                {
                    glyphCount = _typeface.CountGlyphs((IntPtr)chars, runText.Length, SKEncoding.Utf16);
                }

                if (glyphCount > 0)
                {
                    while (glyphCount < runText.Length)
                    {
                        var c = runText[glyphCount];

                        if (IsZeroSpace(c))
                        {
                            glyphCount++;
                            continue;
                        }

                        if (IsBreakChar(c))
                        {
                            glyphCount++;

                            if (glyphCount < runText.Length)
                            {
                                switch (c)
                                {
                                    case '\r' when runText[glyphCount] == '\n':
                                    case '\n' when runText[glyphCount] == '\r':
                                        glyphCount++;
                                        break;
                                }
                            }

                            continue;
                        }

                        var charCount = 1;

                        if (char.IsHighSurrogate(c) && char.IsLowSurrogate(runText[glyphCount + 1]))
                        {
                            charCount = 2;
                        }

                        var symbol = runText.Slice(glyphCount, charCount);

                        fixed (char* chars = symbol)
                        {
                            var ptr = (IntPtr)chars;

                            if (_typeface.CountGlyphs(ptr, charCount, SKEncoding.Utf16) == 0)
                            {
                                break;
                            }

                            glyphCount += charCount;
                        }
                    }
                }
                else
                {
                    var codePoint = char.IsHighSurrogate(runText[0])
                                        ? char.ConvertToUtf32(runText[0], runText[1])
                                        : runText[0];

                    typeface = SKFontManager.Default.MatchCharacter(codePoint);

                    if (codePoint > ushort.MaxValue)
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

                            if (IsZeroSpace(c))
                            {
                                glyphCount++;
                                continue;
                            }

                            if (IsBreakChar(c))
                            {
                                glyphCount++;

                                if (glyphCount < runText.Length)
                                {
                                    switch (c)
                                    {
                                        case '\r' when runText[glyphCount] == '\n':
                                        case '\n' when runText[glyphCount] == '\r':
                                            glyphCount++;
                                            break;
                                    }
                                }

                                continue;
                            }

                            var charCount = char.IsHighSurrogate(c) && glyphCount + 1 < runText.Length ? 2 : 1;

                            var symbol = runText.Slice(glyphCount, charCount);

                            fixed (char* chars = symbol)
                            {
                                var ptr = (IntPtr)chars;

                                if (typeface.CountGlyphs(ptr, charCount, SKEncoding.Utf16) == 0)
                                {
                                    break;
                                }

                                if (_typeface.CountGlyphs(ptr, charCount, SKEncoding.Utf16) != 0)
                                {
                                    break;
                                }
                            }

                            glyphCount += charCount;
                        }
                    }
                }

                if (textPosition + glyphCount < text.Length)
                {
                    runText = text.Slice(textPosition, glyphCount);
                }

                var currentRun = CreateTextRun(
                    text,
                    new SKTextPointer(textPosition, glyphCount),
                    new SKTextFormat(typeface, _fontSize));

                textRuns.Add(currentRun);

                textPosition += glyphCount;

                if (textPosition != text.Length)
                {
                    runText = text.Slice(textPosition, text.Length - textPosition);
                }
            }

            return textRuns;
        }

        /// <summary>
        /// Breaks a text line into multiple text lines.
        /// Performs text trimming and text wrapping if applicable.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="textLine">The text line.</param>
        /// <returns></returns>
        private IEnumerable<SKTextLine> BreakTextLine(ReadOnlySpan<char> text, SKTextLine textLine)
        {
            if (!(textLine.LineMetrics.Size.Width > _constraint.Width))
            {
                return new[] { textLine };
            }

            if (_textTrimming != TextTrimming.None)
            {
                return PerformTextTrimming(text, textLine);
            }

            return _textWrapping == TextWrapping.Wrap ? PerformTextWrapping(text, textLine) : new[] { textLine };
        }

        /// <summary>
        ///     Performs text trimming returns a list of text lines. 
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="textLine">The text line.</param>
        /// <returns></returns>
        private IEnumerable<SKTextLine> PerformTextTrimming(ReadOnlySpan<char> text, SKTextLine textLine)
        {
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
                    var ellipsisRun = CreateEllipsisRun(currentRun.TextFormat, currentRun.Foreground);

                    var measuredLength = BreakGlyphs(currentRun.GlyphRun, availableLength - ellipsisRun.Width);

                    if (_textTrimming == TextTrimming.WordEllipsis && measuredLength < currentRun.TextPointer.Length)
                    {
                        for (var i = measuredLength; i >= 0; i--)
                        {
                            var c = text[currentRun.TextPointer.StartingIndex + i];

                            if (!char.IsWhiteSpace(c))
                            {
                                continue;
                            }

                            measuredLength = i;

                            break;
                        }
                    }

                    var splitResult = SplitTextRun(text, currentRun, measuredLength);

                    var textRuns = new List<SKTextRun>();

                    if (runIndex > 0)
                    {
                        textRuns.AddRange(textLine.TextRuns.Take(runIndex));
                    }

                    if (splitResult.SecondTextRun != null)
                    {
                        textRuns.Add(splitResult.FirstTextRun);
                    }

                    textRuns.Add(ellipsisRun);

                    var textLineMetrics = CreateTextLineMetrics(textRuns, out measuredLength);

                    textLines.Add(
                        new SKTextLine(new SKTextPointer(currentPosition, measuredLength), textRuns, textLineMetrics));

                    break;
                }

                availableLength -= currentRun.Width;

                runIndex++;
            }

            textLines.Add(textLine);

            return textLines;
        }

        /// <summary>
        ///     Performs text wrapping returns a list of text lines.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="textLine">The text line.</param>
        /// <returns></returns>
        private IEnumerable<SKTextLine> PerformTextWrapping(ReadOnlySpan<char> text, SKTextLine textLine)
        {
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

                    if (measuredLength < currentRun.TextPointer.Length)
                    {
                        for (var i = measuredLength; i >= 0; i--)
                        {
                            var c = text[currentRun.TextPointer.StartingIndex + i];

                            if (!char.IsWhiteSpace(c))
                            {
                                continue;
                            }

                            measuredLength = ++i;

                            break;
                        }
                    }

                    var splitResult = SplitTextRun(text, currentRun, measuredLength);

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
        ///     Splits a text run at a specific length and retains all text properties.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="textRun">The text run.</param>
        /// <param name="length">The length of the first run.</param>
        /// <returns></returns>
        private SplitTextRunResult SplitTextRun(ReadOnlySpan<char> text, SKTextRun textRun, int length)
        {
            if (length == 0 || textRun.TextPointer.Length < 2)
            {
                return new SplitTextRunResult(textRun, null);
            }

            var startingIndex = textRun.TextPointer.StartingIndex;

            var textFormat = textRun.TextFormat;

            var foreground = textRun.Foreground;

            SKTextRun firstTextRun;
            SKTextRun secondTextRun;

            if (textRun.GlyphRun != null)
            {
                var firstGlyphRun = new SKGlyphRun(
                    textRun.GlyphRun.GlyphIndices.Take(length).ToArray(),
                    textRun.GlyphRun.GlyphOffsets.Take(length).ToArray(),
                    textRun.GlyphRun.GlyphClusters.Take(length).ToArray());
                firstTextRun = new SKTextRun(
                    new SKTextPointer(startingIndex, length),
                    firstGlyphRun,
                    textFormat,
                    textRun.FontMetrics,
                    firstGlyphRun.GlyphClusters.Sum(x => x.Bounds.Width),
                    textRun.Foreground);

                var secondGlyphClusters = textRun.GlyphRun.GlyphClusters.Skip(length).Select(
                    cluster => new SKGlyphCluster(
                        cluster.TextPosition,
                        cluster.Length,
                        new SKRect(
                            cluster.Bounds.Left - firstTextRun.Width,
                            cluster.Bounds.Top,
                            cluster.Bounds.Right - firstTextRun.Width,
                            cluster.Bounds.Bottom))).ToArray();

                var secondGlyphOffsets = secondGlyphClusters.Select(
                    cluster => new SKPoint(cluster.Bounds.Left, cluster.Bounds.Top)).ToArray();

                var secondGlyphRun = new SKGlyphRun(
                    textRun.GlyphRun.GlyphIndices.Skip(length).ToArray(),
                    secondGlyphOffsets,
                    secondGlyphClusters);

                secondTextRun = new SKTextRun(
                    new SKTextPointer(startingIndex + length, textRun.TextPointer.Length - length),
                    secondGlyphRun,
                    textFormat,
                    textRun.FontMetrics,
                    secondGlyphRun.GlyphClusters.Sum(x => x.Bounds.Width),
                    textRun.Foreground);
            }
            else
            {
                firstTextRun = CreateTextRun(
                    text,
                    new SKTextPointer(startingIndex, length),
                    textFormat,
                    foreground,
                    true);

                secondTextRun = CreateTextRun(
                    text,
                    new SKTextPointer(startingIndex + length, textRun.TextPointer.Length - length),
                    textFormat,
                    foreground,
                    true);
            }

            return new SplitTextRunResult(firstTextRun, secondTextRun);
        }

        /// <summary>
        ///     Splits text runs at a specified length and retains all text properties.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="textRuns">The text runs.</param>
        /// <param name="length">The length of the first part.</param>
        /// <returns></returns>
        private SplitTextLineResult SplitTextRuns(
            ReadOnlySpan<char> text,
            IReadOnlyList<SKTextRun> textRuns,
            int length)
        {
            var firstTextRuns = new List<SKTextRun>();
            var secondTextRuns = new List<SKTextRun>();
            var currentPosition = 0;

            for (var runIndex = 0; runIndex < textRuns.Count; runIndex++)
            {
                var currentRun = textRuns[runIndex];

                if (textRuns.Count == 1 && currentRun.TextPointer.Length == length)
                {
                    return new SplitTextLineResult(textRuns, null);
                }

                if (currentPosition + currentRun.TextPointer.Length < length)
                {
                    currentPosition += currentRun.TextPointer.Length;

                    continue;
                }

                if (currentPosition + currentRun.TextPointer.Length == length)
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

                    var splitResult = SplitTextRun(text, currentRun, length - currentPosition);

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
            ///     Gets the first text run.
            /// </summary>
            /// <value>
            ///     The first text run.
            /// </value>
            public SKTextRun FirstTextRun { get; }

            /// <summary>
            ///     Gets the second text run.
            /// </summary>
            /// <value>
            ///     The second text run.
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
            ///     Gets the first text line.
            /// </summary>
            /// <value>
            ///     The first text line.
            /// </value>
            public IReadOnlyList<SKTextRun> FirstTextRuns { get; }

            /// <summary>
            ///     Gets the second text line.
            /// </summary>
            /// <value>
            ///     The second text line.
            /// </value>
            public IReadOnlyList<SKTextRun> SecondTextRuns { get; }
        }
    }
}
