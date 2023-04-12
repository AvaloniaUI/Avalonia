using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    public sealed class ShapedBuffer : IReadOnlyList<GlyphInfo>, IDisposable
    {
        private GlyphInfo[]? _rentedBuffer;
        private ArraySlice<GlyphInfo> _glyphInfos;

        public ShapedBuffer(ReadOnlyMemory<char> text, int bufferLength, IGlyphTypeface glyphTypeface, double fontRenderingEmSize, sbyte bidiLevel)
        {
            Text = text;
            _rentedBuffer = ArrayPool<GlyphInfo>.Shared.Rent(bufferLength);
            _glyphInfos = new ArraySlice<GlyphInfo>(_rentedBuffer, 0, bufferLength);      
            GlyphTypeface = glyphTypeface;
            FontRenderingEmSize = fontRenderingEmSize;
            BidiLevel = bidiLevel;
        }

        internal ShapedBuffer(ReadOnlyMemory<char> text, ArraySlice<GlyphInfo> glyphInfos, IGlyphTypeface glyphTypeface, double fontRenderingEmSize, sbyte bidiLevel)
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
        public IGlyphTypeface GlyphTypeface { get; }

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

        /// <summary>
        /// Finds a glyph index for given character index.
        /// </summary>
        /// <param name="characterIndex">The character index.</param>
        /// <returns>
        /// The glyph index.
        /// </returns>
        private int FindGlyphIndex(int characterIndex)
        {
            if (characterIndex < _glyphInfos[0].GlyphCluster)
            {
                return 0;
            }

            if (characterIndex > _glyphInfos[_glyphInfos.Length - 1].GlyphCluster)
            {
                return _glyphInfos.Length - 1;
            }

            var comparer = GlyphInfo.ClusterAscendingComparer;

            var glyphInfos = _glyphInfos.Span;

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

            var firstCluster = _glyphInfos[0].GlyphCluster;
            var lastCluster = _glyphInfos[_glyphInfos.Length - 1].GlyphCluster;

            var start = firstCluster < lastCluster ? firstCluster : lastCluster;

            var glyphCount = FindGlyphIndex(start + length);

            var first = new ShapedBuffer(Text.Slice(0, length),
                _glyphInfos.Take(glyphCount), GlyphTypeface, FontRenderingEmSize, BidiLevel);

            var second = new ShapedBuffer(Text.Slice(length),
                _glyphInfos.Skip(glyphCount), GlyphTypeface, FontRenderingEmSize, BidiLevel);

            return new SplitResult<ShapedBuffer>(first, second);
        }

        int IReadOnlyCollection<GlyphInfo>.Count => _glyphInfos.Length;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();       
    }
}
