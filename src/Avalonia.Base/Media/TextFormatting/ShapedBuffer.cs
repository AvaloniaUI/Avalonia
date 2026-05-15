using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    public sealed class ShapedBuffer : IReadOnlyList<GlyphInfo>, IDisposable
    {
        private GlyphInfo[]? _rentedBuffer;
        private ArraySlice<GlyphInfo> _glyphInfos;

        public ShapedBuffer(ReadOnlyMemory<char> text, int bufferLength, GlyphTypeface glyphTypeface, double fontRenderingEmSize, sbyte bidiLevel)
        {
            Text = text;
            _rentedBuffer = ArrayPool<GlyphInfo>.Shared.Rent(bufferLength);
            _glyphInfos = new ArraySlice<GlyphInfo>(_rentedBuffer, 0, bufferLength);
            GlyphTypeface = glyphTypeface;
            FontRenderingEmSize = fontRenderingEmSize;
            BidiLevel = bidiLevel;
        }

        internal ShapedBuffer(ReadOnlyMemory<char> text, ArraySlice<GlyphInfo> glyphInfos, GlyphTypeface glyphTypeface, double fontRenderingEmSize, sbyte bidiLevel)
        {
            Text = text;
            _glyphInfos = glyphInfos;
            GlyphTypeface = glyphTypeface;
            FontRenderingEmSize = fontRenderingEmSize;
            BidiLevel = bidiLevel;
        }

        /// <summary>
        /// The buffer's length.
        /// </summary>
        public int Length => _glyphInfos.Length;

        /// <summary>
        /// The buffer's glyph infos.
        /// </summary>
        internal ArraySlice<GlyphInfo> GlyphInfos => _glyphInfos;

        /// <summary>
        /// The buffer's glyph typeface.
        /// </summary>
        public GlyphTypeface GlyphTypeface { get; }

        /// <summary>
        /// The buffers font rendering em size.
        /// </summary>
        public double FontRenderingEmSize { get; }

        /// <summary>
        /// The buffer's bidi level.
        /// </summary>
        public sbyte BidiLevel { get; }

        /// <summary>
        /// The buffer's reading direction.
        /// </summary>
        public bool IsLeftToRight => (BidiLevel & 1) == 0;

        /// <summary>
        /// The text that is represended by this buffer.
        /// </summary>
        public ReadOnlyMemory<char> Text { get; }

        public void Dispose()
        {
            if (_rentedBuffer is not null)
            {
                ArrayPool<GlyphInfo>.Shared.Return(_rentedBuffer);
                _rentedBuffer = null;
                _glyphInfos = ArraySlice<GlyphInfo>.Empty; // ensure we don't misuse the returned array
            }
        }

        public GlyphInfo this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _glyphInfos[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _glyphInfos[index] = value;
        }

        public IEnumerator<GlyphInfo> GetEnumerator() => _glyphInfos.GetEnumerator();

        /// <summary>
        /// Creates a view of this buffer that reports the given <paramref name="paragraphEmbeddingLevel"/>
        /// as its <see cref="BidiLevel"/> while sharing the same underlying glyphs.
        /// Used for trailing-whitespace handling where the visual direction follows the paragraph.
        /// </summary>
        internal ShapedBuffer WithBidiLevel(sbyte paragraphEmbeddingLevel)
        {
            if (BidiLevel == paragraphEmbeddingLevel)
            {
                return this;
            }

            return new ShapedBuffer(Text, _glyphInfos, GlyphTypeface, FontRenderingEmSize, paragraphEmbeddingLevel);
        }

        int IReadOnlyCollection<GlyphInfo>.Count => _glyphInfos.Length;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Splits the <see cref="TextRun"/> at specified length.
        /// </summary>
        /// <param name="textLength">The text length.</param>
        /// <returns>The split result.</returns>
        public SplitResult<ShapedBuffer> Split(int textLength)
        {
            // make sure we do not overshoot
            textLength = Math.Min(Text.Length, textLength);

            if (textLength <= 0)
            {
                var emptyBuffer = new ShapedBuffer(
                    Text.Slice(0, 0), _glyphInfos.Slice(_glyphInfos.Start, 0),
                    GlyphTypeface, FontRenderingEmSize, BidiLevel);

                return new SplitResult<ShapedBuffer>(emptyBuffer, this);
            }

            // nothing to split
            if (textLength == Text.Length)
            {
                return new SplitResult<ShapedBuffer>(this, null);
            }

            return IsLeftToRight ? SplitAscending(textLength) : SplitDescending(textLength);
        }

        /// <summary>
        /// Split a buffer whose glyphs are ordered by ascending cluster (LTR visual / logical order).
        /// </summary>
        private SplitResult<ShapedBuffer> SplitAscending(int textLength)
        {
            var sliceStart = _glyphInfos.Start;
            var glyphInfos = _glyphInfos.Span;
            var glyphInfosLength = _glyphInfos.Length;

            // the first glyph’s cluster is our “zero” for this sub-buffer.
            var baseCluster = glyphInfos[0].GlyphCluster;
            var targetCluster = baseCluster + textLength;

            var searchValue = new GlyphInfo(0, targetCluster, 0, default);
            var foundIndex = glyphInfos.BinarySearch(searchValue, GlyphInfo.ClusterAscendingComparer);

            int splitGlyphIndex;
            int splitCharCount;

            if (foundIndex >= 0)
            {
                // back up to the start of the cluster
                var i = foundIndex;

                while (i > 0 && glyphInfos[i - 1].GlyphCluster == targetCluster)
                {
                    i--;
                }

                splitGlyphIndex = i;
                splitCharCount = targetCluster - baseCluster;
            }
            else
            {
                var invertedIndex = ~foundIndex;

                if (invertedIndex >= glyphInfosLength)
                {
                    splitGlyphIndex = glyphInfosLength;
                    splitCharCount = Text.Length;
                }
                else
                {
                    splitGlyphIndex = invertedIndex;
                    var nextCluster = glyphInfos[invertedIndex].GlyphCluster;
                    splitCharCount = nextCluster - baseCluster;
                }
            }

            var firstGlyphs = _glyphInfos.Slice(sliceStart, splitGlyphIndex);
            var secondGlyphs = _glyphInfos.Slice(sliceStart + splitGlyphIndex, glyphInfosLength - splitGlyphIndex);

            var firstText = Text.Slice(0, splitCharCount);
            var secondText = Text.Slice(splitCharCount);

            var leading = new ShapedBuffer(
                firstText, firstGlyphs,
                GlyphTypeface, FontRenderingEmSize, BidiLevel);

            if (secondText.Length == 0)
            {
                return new SplitResult<ShapedBuffer>(leading, null);
            }

            var trailing = new ShapedBuffer(
                secondText, secondGlyphs,
                GlyphTypeface, FontRenderingEmSize, BidiLevel);

            return new SplitResult<ShapedBuffer>(leading, trailing);
        }

        /// <summary>
        /// Split a buffer whose glyphs are ordered by descending cluster (RTL visual order).
        /// The leading <see cref="ShapedBuffer"/> corresponds to text[0..textLength], whose glyphs
        /// live at the <em>tail</em> of the visual array.
        /// </summary>
        private SplitResult<ShapedBuffer> SplitDescending(int textLength)
        {
            var sliceStart = _glyphInfos.Start;
            var glyphInfos = _glyphInfos.Span;
            var glyphInfosLength = _glyphInfos.Length;

            // Cluster values are anchored to the original text and can start from any value
            // after a previous split. The logical-first cluster of this buffer is at the
            // tail of the visual array (smallest cluster). The split happens at logical
            // offset textLength from there.
            var baseCluster = glyphInfos[glyphInfosLength - 1].GlyphCluster;
            var targetCluster = baseCluster + textLength;

            var searchValue = new GlyphInfo(0, targetCluster, 0, default);
            var foundIndex = glyphInfos.BinarySearch(searchValue, GlyphInfo.ClusterDescendingComparer);

            int splitGlyphIndex; // boundary between "second" (head/leading-visual) and "first" (tail/trailing-visual)

            if (foundIndex >= 0)
            {
                // Snap to the last glyph that still has cluster == targetCluster; that glyph
                // belongs to the trailing chunk (text[textLength..]) — i.e. still "second".
                while (foundIndex + 1 < glyphInfosLength
                       && glyphInfos[foundIndex + 1].GlyphCluster == targetCluster)
                {
                    foundIndex++;
                }

                splitGlyphIndex = foundIndex + 1;
            }
            else
            {
                splitGlyphIndex = ~foundIndex;
            }

            // Visual leading = glyphs [0, splitGlyphIndex) → logically text[textLength..]  (our "second")
            // Visual trailing = glyphs [splitGlyphIndex, end) → logically text[0..textLength] (our "first")
            var secondGlyphs = _glyphInfos.Slice(sliceStart, splitGlyphIndex);
            var firstGlyphs = _glyphInfos.Slice(sliceStart + splitGlyphIndex, glyphInfosLength - splitGlyphIndex);

            var firstText = Text.Slice(0, textLength);
            var secondText = Text.Slice(textLength);

            var first = new ShapedBuffer(
                firstText, firstGlyphs,
                GlyphTypeface, FontRenderingEmSize, BidiLevel);

            if (secondText.Length == 0 || secondGlyphs.Length == 0)
            {
                return new SplitResult<ShapedBuffer>(first, null);
            }

            var second = new ShapedBuffer(
                secondText, secondGlyphs,
                GlyphTypeface, FontRenderingEmSize, BidiLevel);

            return new SplitResult<ShapedBuffer>(first, second);
        }
    }
}
