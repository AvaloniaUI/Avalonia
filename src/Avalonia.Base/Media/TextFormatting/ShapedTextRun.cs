using System;
using System.Threading;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that holds shaped characters.
    /// </summary>
    /// <remarks>
    /// Glyph data in the underlying <see cref="ShapedBuffer"/> is immutable after shaping:
    /// LTR buffers are in ascending-cluster (logical) order, RTL buffers are in
    /// descending-cluster (visual) order. <see cref="BidiReorderer"/> only reorders runs,
    /// it never mutates glyph order.
    ///
    /// Ref-counted: the initial constructor call establishes one reference. Call
    /// <see cref="AddRef"/> when taking an additional reference (e.g., when a
    /// <see cref="TextRunCache"/> stores a shaped run a formatter is also about to use),
    /// and <see cref="Dispose"/> to release. The underlying shaped buffer is disposed only
    /// when the last reference is released.
    /// </remarks>
    public sealed class ShapedTextRun : DrawableTextRun, IDisposable
    {
        private GlyphRun? _glyphRun;
        private int _refCount = 1;

        public ShapedTextRun(ShapedBuffer shapedBuffer, TextRunProperties properties)
        {
            ShapedBuffer = shapedBuffer;
            Properties = properties;
            TextMetrics = new TextMetrics(properties.CachedGlyphTypeface, properties.FontRenderingEmSize);
        }

        public sbyte BidiLevel => ShapedBuffer.BidiLevel;

        public ShapedBuffer ShapedBuffer { get; }

        /// <inheritdoc/>
        public override ReadOnlyMemory<char> Text
            => ShapedBuffer.Text;

        /// <inheritdoc/>
        public override TextRunProperties Properties { get; }

        /// <inheritdoc/>
        public override int Length
            => ShapedBuffer.Text.Length;

        public TextMetrics TextMetrics { get; }

        public override double Baseline => -TextMetrics.Ascent + TextMetrics.LineGap * 0.5;

        public override Size Size => GlyphRun.Bounds.Size;

        public GlyphRun GlyphRun => _glyphRun ??= CreateGlyphRun();

        /// <summary>
        /// Takes an additional reference to this run. Must be paired with <see cref="Dispose"/>.
        /// </summary>
        internal ShapedTextRun AddRef()
        {
            Interlocked.Increment(ref _refCount);
            return this;
        }

        /// <inheritdoc/>
        public override void Draw(DrawingContext drawingContext, Point origin)
        {
            using (drawingContext.PushTransform(Matrix.CreateTranslation(origin)))
            {
                if (GlyphRun.GlyphInfos.Count == 0)
                {
                    return;
                }

                if (Properties.Typeface == default)
                {
                    return;
                }

                if (Properties.ForegroundBrush == null)
                {
                    return;
                }

                if (Properties.BackgroundBrush != null)
                {
                    drawingContext.DrawRectangle(Properties.BackgroundBrush, null, GlyphRun.Bounds);
                }

                drawingContext.DrawGlyphRun(Properties.ForegroundBrush, GlyphRun);

                if (Properties.TextDecorations == null)
                {
                    return;
                }

                foreach (var textDecoration in Properties.TextDecorations)
                {
                    textDecoration.Draw(drawingContext, GlyphRun, TextMetrics, Properties.ForegroundBrush);
                }
            }
        }

        /// <summary>
        /// Measures the number of characters that fit into available width.
        /// </summary>
        /// <param name="availableWidth">The available width.</param>
        /// <param name="length">The count of fitting characters.</param>
        /// <returns>
        /// <c>true</c> if characters fit into the available width; otherwise, <c>false</c>.
        /// </returns>
        public bool TryMeasureCharacters(double availableWidth, out int length)
        {
            length = 0;

            if (ShapedBuffer.Length == 0)
            {
                return false;
            }

            var currentWidth = 0.0;
            var charactersSpan = GlyphRun.Characters.Span;
            var isLeftToRight = ShapedBuffer.IsLeftToRight;
            var bufferLength = ShapedBuffer.Length;
            var textLength = Text.Length;

            // Previous visual glyph's cluster — used in RTL mode to compute the char count
            // contributed by the current glyph (which spans [currentCluster, prevCluster) logically).
            var previousCluster = 0;

            for (var i = 0; i < bufferLength; i++)
            {
                var advance = ShapedBuffer[i].GlyphAdvance;
                var currentCluster = ShapedBuffer[i].GlyphCluster;

                if (currentWidth + advance > availableWidth)
                {
                    break;
                }

                int count;

                if (isLeftToRight)
                {
                    if (i + 1 < bufferLength)
                    {
                        var nextCluster = ShapedBuffer[i + 1].GlyphCluster;
                        count = nextCluster - currentCluster;
                    }
                    else
                    {
                        Codepoint.ReadAt(charactersSpan, length, out count);
                    }
                }
                else
                {
                    if (i == 0)
                    {
                        count = textLength - currentCluster;
                    }
                    else
                    {
                        count = previousCluster - currentCluster;
                    }
                }

                length += count;
                currentWidth += advance;
                previousCluster = currentCluster;
            }

            return length > 0;
        }

        internal bool TryMeasureCharactersBackwards(double availableWidth, out int length, out double width)
        {
            length = 0;
            width = 0;
            var charactersSpan = GlyphRun.Characters.Span;

            for (var i = ShapedBuffer.Length - 1; i >= 0; i--)
            {
                var advance = ShapedBuffer[i].GlyphAdvance;

                if (width + advance > availableWidth)
                {
                    break;
                }

                Codepoint.ReadAt(charactersSpan, length, out var count);

                length += count;
                width += advance;
            }

            return length > 0;
        }

        internal SplitResult<ShapedTextRun> Split(int length)
        {
            if (length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "length must be greater than zero.");
            }

            var splitBuffer = ShapedBuffer.Split(length);

            // first cannot be null as length > 0
            var first = new ShapedTextRun(splitBuffer.First!, Properties);

            if (first.Length < length)
            {
                throw new InvalidOperationException("Split length too small.");
            }

            if (splitBuffer.Second == null)
            {
                // The requested split position falls inside an unbreakable cluster that extends
                // to the end of the run (e.g. a ligature or grapheme shaped as a single glyph),
                // so the run cannot be split any further. Return the whole run as a single part
                // instead of throwing: the caller treats a null counterpart as "nothing left to split".
                return isReversed
                    ? new SplitResult<ShapedTextRun>(null, first)
                    : new SplitResult<ShapedTextRun>(first, null);
            }

            var second = new ShapedTextRun(splitBuffer.Second, Properties);

            return new SplitResult<ShapedTextRun>(first, second);
        }

        internal GlyphRun CreateGlyphRun()
        {
            return new GlyphRun(
                ShapedBuffer.GlyphTypeface,
                ShapedBuffer.FontRenderingEmSize,
                Text,
                ShapedBuffer,
                biDiLevel: BidiLevel);
        }

        public void Dispose()
        {
            if (Interlocked.Decrement(ref _refCount) != 0)
            {
                return;
            }

            _glyphRun?.Dispose();
            ShapedBuffer.Dispose();
        }
    }
}
