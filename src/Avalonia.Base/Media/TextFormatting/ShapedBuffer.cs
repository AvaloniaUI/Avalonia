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
        public sbyte BidiLevel { get; private set; }

        /// <summary>
        /// The buffer's reading direction.
        /// </summary>
        public bool IsLeftToRight => (BidiLevel & 1) == 0;

        /// <summary>
        /// The text that is represended by this buffer.
        /// </summary>
        public ReadOnlyMemory<char> Text { get; }
        
        /// <summary>
        /// Reverses the buffer.
        /// </summary>
        public void Reverse()
        {
            _glyphInfos.Span.Reverse();
        }

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

        internal void ResetBidiLevel(sbyte paragraphEmbeddingLevel) => BidiLevel = paragraphEmbeddingLevel;

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

            var sliceStart = _glyphInfos.Start;
            var glyphInfos = _glyphInfos.Span;
            var glyphInfosLength = _glyphInfos.Length;

            // the first glyph’s cluster is our “zero” for this sub‐buffer.
            // we want an absolute target cluster = baseCluster + textLength
            var baseCluster = glyphInfos[0].GlyphCluster;
            var targetCluster = baseCluster + textLength;

            // binary‐search for a dummy with cluster == targetCluster
            var searchValue = new GlyphInfo(0, targetCluster, 0, default);
            var foundIndex = glyphInfos.BinarySearch(searchValue, GlyphInfo.ClusterAscendingComparer);

            int splitGlyphIndex;    // how many glyph‐slots go into "leading"
            int splitCharCount;   // how many chars go into "leading" Text

            if (foundIndex >= 0)
            {
                // found a glyph info whose cluster == targetCluster
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
                // no exact match need to invert so ~foundIndex is the insertion point
                // the first cluster > targetCluster
                var invertedIndex = ~foundIndex;

                if (invertedIndex >= glyphInfosLength)
                {
                    // happens only if targetCluster ≥ lastCluster
                    // put everything into leading
                    splitGlyphIndex = glyphInfosLength;
                    splitCharCount = Text.Length;
                }
                else
                {
                    // snap to the start of that next cluster
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

            // this happens if we try to find a position inside a cluster and we moved to the end
            if(secondText.Length == 0)
            {
                return new SplitResult<ShapedBuffer>(leading, null);
            }

            var trailing = new ShapedBuffer(
                secondText, secondGlyphs,
                GlyphTypeface, FontRenderingEmSize, BidiLevel);

            return new SplitResult<ShapedBuffer>(leading, trailing);
        }
    }
}
