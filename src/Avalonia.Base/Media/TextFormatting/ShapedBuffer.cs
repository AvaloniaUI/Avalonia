using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    public sealed class ShapedBuffer : IReadOnlyList<GlyphInfo>, IDisposable
    {
        private GlyphInfo[]? _rentedBuffer;

        public ShapedBuffer(ReadOnlyMemory<char> text, int bufferLength, IGlyphTypeface glyphTypeface, double fontRenderingEmSize, sbyte bidiLevel)
        {
            _rentedBuffer = ArrayPool<GlyphInfo>.Shared.Rent(bufferLength);
            Text = text;
            GlyphInfos = new ArraySlice<GlyphInfo>(_rentedBuffer, 0, bufferLength);
            GlyphTypeface = glyphTypeface;
            FontRenderingEmSize = fontRenderingEmSize;
            BidiLevel = bidiLevel;
        }

        internal ShapedBuffer(ReadOnlyMemory<char> text, ArraySlice<GlyphInfo> glyphInfos, IGlyphTypeface glyphTypeface, double fontRenderingEmSize, sbyte bidiLevel)
        {
            Text = text;
            GlyphInfos = glyphInfos;
            GlyphTypeface = glyphTypeface;
            FontRenderingEmSize = fontRenderingEmSize;
            BidiLevel = bidiLevel;
        }

        internal ArraySlice<GlyphInfo> GlyphInfos { get; private set; }

        public int Length
            => GlyphInfos.Length;

        public IGlyphTypeface GlyphTypeface { get; }

        public double FontRenderingEmSize { get; }

        public sbyte BidiLevel { get; }

        public bool IsLeftToRight => (BidiLevel & 1) == 0;

        public ReadOnlyMemory<char> Text { get; }
        
        /// <summary>
        /// Finds a glyph index for given character index.
        /// </summary>
        /// <param name="characterIndex">The character index.</param>
        /// <returns>
        /// The glyph index.
        /// </returns>
        private int FindGlyphIndex(int characterIndex)
        {
            if (characterIndex < GlyphInfos[0].GlyphCluster)
            {
                return 0;
            }

            if (characterIndex > GlyphInfos[GlyphInfos.Length - 1].GlyphCluster)
            {
                return GlyphInfos.Length - 1;
            }


            var comparer = GlyphInfo.ClusterAscendingComparer;

            var glyphInfos = GlyphInfos.Span;

            var searchValue = new GlyphInfo(default, characterIndex, default);

            var start = glyphInfos.BinarySearch(searchValue, comparer);

            if (start < 0)
            {
                while (characterIndex > 0 && start < 0)
                {
                    characterIndex--;

                    searchValue = new GlyphInfo(default, characterIndex, default);

                    start = glyphInfos.BinarySearch(searchValue, comparer);
                }

                if (start < 0)
                {
                    return -1;
                }
            }

            while (start > 0 && glyphInfos[start - 1].GlyphCluster == glyphInfos[start].GlyphCluster)
            {
                start--;
            }

            return start;
        }

        /// <summary>
        /// Splits the <see cref="TextRun"/> at specified length.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>The split result.</returns>
        internal SplitResult<ShapedBuffer> Split(int length)
        {
            if (Text.Length == length)
            {
                return new SplitResult<ShapedBuffer>(this, null);
            }

            var firstCluster = GlyphInfos[0].GlyphCluster;
            var lastCluster = GlyphInfos[GlyphInfos.Length - 1].GlyphCluster;

            var start = firstCluster < lastCluster ? firstCluster : lastCluster;

            var glyphCount = FindGlyphIndex(start + length);

            var first = new ShapedBuffer(Text.Slice(0, length),
                GlyphInfos.Take(glyphCount), GlyphTypeface, FontRenderingEmSize, BidiLevel);

            var second = new ShapedBuffer(Text.Slice(length),
                GlyphInfos.Skip(glyphCount), GlyphTypeface, FontRenderingEmSize, BidiLevel);

            return new SplitResult<ShapedBuffer>(first, second);
        }

        int IReadOnlyCollection<GlyphInfo>.Count => GlyphInfos.Length;

        public GlyphInfo this[int index]
        {
            get => GlyphInfos[index];
            set => GlyphInfos[index] = value;
        }

        public IEnumerator<GlyphInfo> GetEnumerator() => GlyphInfos.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            if (_rentedBuffer is not null)
            {
                ArrayPool<GlyphInfo>.Shared.Return(_rentedBuffer);
                _rentedBuffer = null;
                GlyphInfos = ArraySlice<GlyphInfo>.Empty; // ensure we don't misuse the returned array
            }
        }
    }
}
