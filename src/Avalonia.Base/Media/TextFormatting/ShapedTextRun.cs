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
        private ShapedBuffer? _shapedBufferWithoutSpacing = null;

        public ShapedTextRun(ShapedBuffer shapedBuffer, TextRunProperties properties)
        {
            ShapedBuffer = shapedBuffer;
            Properties = properties;
            TextMetrics = new TextMetrics(properties.CachedGlyphTypeface, properties.FontRenderingEmSize);
        }

        public sbyte BidiLevel => ShapedBuffer.BidiLevel;

        public ShapedBuffer ShapedBuffer { get; }

        /// <summary>
        /// Gets the underlying shaped buffer without any spacing adjustments applied.
        /// This is needed for measuring the original text metrics before any justification or
        /// adjustments are made.
        /// </summary>
        internal ShapedBuffer ShapedBufferWithoutSpacing {
            get => _shapedBufferWithoutSpacing ?? ShapedBuffer;
            set => _shapedBufferWithoutSpacing = value; 
        }
        /// <summary>
        /// Resets the ShapedBufferWithoutSpacing
        /// </summary>
        internal void InvalidateShapedBufferWithoutSpacing()
        {
            _shapedBufferWithoutSpacing = null;
        }


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
        /// Returns the largest count of <b>logical leading</b> characters of this
        /// run that fit within <paramref name="availableWidth"/>. Cluster-atomic
        /// and direction-agnostic — for RTL runs the result is the count of chars
        /// from the logical start (not the visually-leftmost chars, which would
        /// be the logical tail).
        /// </summary>
        /// <param name="availableWidth">The available width.</param>
        /// <param name="length">The count of fitting characters.</param>
        /// <returns>
        /// <c>true</c> if at least one character fits within
        /// <paramref name="availableWidth"/>; otherwise <c>false</c>.
        /// </returns>
        public bool TryMeasureCharacters(double availableWidth, out int length)
        {
            length = ShapedBuffer.FindLeadingCharCountWithinWidth(availableWidth);
            return length > 0;
        }

        /// <summary>
        /// Returns the largest count of <b>logical trailing</b> characters of
        /// this run that fit within <paramref name="availableWidth"/>, along
        /// with the cumulative advance they consume. Cluster-atomic and
        /// direction-agnostic.
        /// </summary>
        internal bool TryMeasureCharactersBackwards(double availableWidth, out int length, out double width)
        {
            length = ShapedBuffer.FindTrailingCharCountWithinWidth(availableWidth, out width);
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
                return new SplitResult<ShapedTextRun>(first, null);
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
