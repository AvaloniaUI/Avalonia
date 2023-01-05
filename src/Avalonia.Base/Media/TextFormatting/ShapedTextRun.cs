using System;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that holds shaped characters.
    /// </summary>
    public sealed class ShapedTextRun : SymbolTextRun
    {
        private GlyphRun? _glyphRun;

        public ShapedTextRun(ShapedBuffer shapedBuffer, TextRunProperties properties) 
            : base(shapedBuffer.CharacterBufferRange.CharacterBufferReference, 
                properties, 
                shapedBuffer.CharacterBufferRange.Length, 
                shapedBuffer.BidiLevel)
        {
            ShapedBuffer = shapedBuffer;
        }


        public ShapedBuffer ShapedBuffer { get; }
        
        /// <inheritdoc/>
        public override CharacterBufferReference CharacterBufferReference { get; }

        public override GlyphRun GlyphRun
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

        internal override void Reverse()
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
        internal override bool TryMeasureCharacters(double availableWidth, out int length)
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

        internal override bool TryMeasureCharactersBackwards(double availableWidth, out int length, out double width)
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

        internal override SplitResult<SymbolTextRun> Split(int length)
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

            var first = new ShapedTextRun(splitBuffer.First, Properties);

            #if DEBUG

            if (first.Length != length)
            {
                throw new InvalidOperationException("Split length mismatch.");
            }

#endif

            var second = new ShapedTextRun(splitBuffer.Second!, Properties);

            return new SplitResult<SymbolTextRun>(first, second);
        }

        internal GlyphRun CreateGlyphRun()
        {
            return new GlyphRun(
                ShapedBuffer.GlyphTypeface,
                ShapedBuffer.FontRenderingEmSize,
                new CharacterBufferRange(CharacterBufferReference, Length),
                ShapedBuffer.GlyphIndices,
                ShapedBuffer.GlyphAdvances,
                ShapedBuffer.GlyphOffsets,
                ShapedBuffer.GlyphClusters,
                BidiLevel);
        }
    }
}
