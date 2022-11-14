using System;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that holds shaped characters.
    /// </summary>
    public sealed class ShapedTextCharacters : DrawableTextRun
    {
        private GlyphRun? _glyphRun;

        public ShapedTextCharacters(ShapedBuffer shapedBuffer, TextRunProperties properties)
        {
            ShapedBuffer = shapedBuffer;
            Text = shapedBuffer.Text;
            Properties = properties;
            TextSourceLength = Text.Length;
            TextMetrics = new TextMetrics(properties.Typeface, properties.FontRenderingEmSize);
        }

        public bool IsReversed { get; private set; }

        public sbyte BidiLevel => ShapedBuffer.BidiLevel;

        public ShapedBuffer ShapedBuffer { get; }

        /// <inheritdoc/>
        public override ReadOnlySlice<char> Text { get; }

        /// <inheritdoc/>
        public override TextRunProperties Properties { get; }

        /// <inheritdoc/>
        public override int TextSourceLength { get; }

        public TextMetrics TextMetrics { get; }

        public override double Baseline => -TextMetrics.Ascent;

        public override Size Size => GlyphRun.Size;

        public GlyphRun GlyphRun
        {
            get
            {
                if(_glyphRun is null)
                {
                    _glyphRun = CreateGlyphRun();
                }

                return _glyphRun;
            }
        }

        /// <inheritdoc/>
        public override void Draw(DrawingContext drawingContext, Point origin)
        {
            using (drawingContext.PushPreTransform(Matrix.CreateTranslation(origin)))
            {
                if (GlyphRun.GlyphIndices.Count == 0)
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
                    drawingContext.DrawRectangle(Properties.BackgroundBrush, null, new Rect(Size));
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

        internal void Reverse()
        {
            _glyphRun = null;

            ShapedBuffer.GlyphInfos.Span.Reverse();

            IsReversed = !IsReversed;
        }

        /// <summary>
        /// Measures the number of characters that fit into available width.
        /// </summary>
        /// <param name="availableWidth">The available width.</param>
        /// <param name="length">The count of fitting characters.</param>
        /// <returns>
        /// <c>true</c> if characters fit into the available width; otherwise, <c>false</c>.
        /// </returns>
        internal bool TryMeasureCharacters(double availableWidth, out int length)
        {
            length = 0;
            var currentWidth = 0.0;

            for (var i = 0; i < ShapedBuffer.Length; i++)
            {
                var advance = ShapedBuffer.GlyphAdvances[i];

                if (currentWidth + advance > availableWidth)
                {
                    break;
                }

                Codepoint.ReadAt(GlyphRun.Characters, length, out var count);

                length += count;
                currentWidth += advance;
            }

            return length > 0;
        }

        internal bool TryMeasureCharactersBackwards(double availableWidth, out int length, out double width)
        {
            length = 0;
            width = 0;

            for (var i = ShapedBuffer.Length - 1; i >= 0; i--)
            {
                var advance = ShapedBuffer.GlyphAdvances[i];

                if (width + advance > availableWidth)
                {
                    break;
                }

                Codepoint.ReadAt(GlyphRun.Characters, length, out var count);

                length += count;
                width += advance;
            }

            return length > 0;
        }

        internal SplitResult<ShapedTextCharacters> Split(int length)
        {
            if (IsReversed)
            {
                Reverse();
            }

#if DEBUG
            if(length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "length must be greater than zero.");
            }
#endif
            
            var splitBuffer = ShapedBuffer.Split(length);

            var first = new ShapedTextCharacters(splitBuffer.First, Properties);

            #if DEBUG

            if (first.Text.Length != length)
            {
                throw new InvalidOperationException("Split length mismatch.");
            }
            
            #endif

            var second = new ShapedTextCharacters(splitBuffer.Second!, Properties);

            return new SplitResult<ShapedTextCharacters>(first, second);
        }

        internal GlyphRun CreateGlyphRun()
        {
            return new GlyphRun(
                ShapedBuffer.GlyphTypeface,
                ShapedBuffer.FontRenderingEmSize,
                Text,
                ShapedBuffer.GlyphIndices,
                ShapedBuffer.GlyphAdvances,
                ShapedBuffer.GlyphOffsets,
                ShapedBuffer.GlyphClusters,
                BidiLevel);
        }
    }
}
