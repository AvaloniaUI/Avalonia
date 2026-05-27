using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    public sealed class ShapedBuffer : IReadOnlyList<GlyphInfo>, IDisposable
    {
        private GlyphInfo[]? _rentedBuffer;
        private ArraySlice<GlyphInfo> _glyphInfos;

        // Lazily-computed cluster-width cache. MeasureLength and metrics
        // queries both fold multi-glyph clusters and accumulate per-cluster
        // advances — the result depends only on the shaped glyphs and the
        // text, both immutable for a given ShapedBuffer instance.
        //
        // To make wrap-time splits cheap, the cache is shared by reference
        // across split halves: when this buffer was produced by Split, the
        // arrays point at the parent's cache and _clusterStartIdx is the offset
        // at which this buffer's first cluster lives. That keeps post-split
        // wrap iterations O(1) per query instead of re-walking glyphs.
        //
        // _clusterPrefix[i] = sum of cluster advances [0..i) in logical order
        // over the full source buffer (parent's view); width of this sub-buffer
        // is `_clusterPrefix[_clusterStartIdx + _clusterCount] - _clusterPrefix[_clusterStartIdx]`.
        // _clusterStartChars[i] = char offset into the parent's Text where the
        // i-th cluster begins; this sub-buffer's start char in parent space is
        // `_clusterStartChars[_clusterStartIdx]`.
        //
        // Fast path: when every cluster is exactly one character wide (the
        // common Latin / single-codepoint case where bufferLength == Text.Length
        // == clusters), the start-chars array is omitted entirely because
        // `startChars[k] == k` is implicit. In that mode `_clusterStartChars`
        // is null but `_clusterPrefix` is still populated. This is detected
        // during cache build and preserved across <see cref="Split"/>.
        //
        // Sums in this prefix are in logical (not visual) order, which can
        // differ by ULPs from a visual-order sum for RTL buffers. Consumers
        // comparing layout dimensions should use tolerant equality.
        //
        // Pooling: the cache arrays are rented from ArrayPool<T>.Shared in
        // EnsureClusterCache. Ownership follows the same pattern as
        // _rentedBuffer for the glyph-info array: only the buffer that rented
        // the array tracks it in _rentedClusterPrefix / _rentedClusterStartChars
        // and returns it on Dispose. Split children copy the data-array
        // references via _clusterPrefix / _clusterStartChars but leave the
        // _rented* handles null, so they don't return arrays they don't own.
        // The pool may hand back larger arrays than requested — we always read
        // through bounded indices (_clusterStartIdx + [0.._clusterCount]) so the
        // unused tail is harmless.
        private double[]? _clusterPrefix;
        private int[]? _clusterStartChars;
        private double[]? _rentedClusterPrefix;
        private int[]? _rentedClusterStartChars;
        private int _clusterStartIdx;
        private int _clusterCount;

        // Set when this buffer has published its cluster-cache to a sibling
        // (a Split child or a WithBidiLevel alias). Once shared, the buffer is
        // expected to be observationally immutable — mutating the glyph array
        // via the indexer setter would silently corrupt the siblings' view.
        // The guard fires in DEBUG only; release builds keep the existing
        // contract that the shaper never mutates a buffer after splitting.
        private bool _cacheShared;

        private static readonly int[] s_emptyStartChars = new[] { 0 };
        private static readonly double[] s_emptyPrefix = new[] { 0d };

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
        /// Internal constructor used by <see cref="Split"/> to hand off a shared cluster
        /// cache to the new sub-buffer. The cache arrays are owned by the parent buffer;
        /// this sub-buffer's queries adjust by <paramref name="clusterStartIdx"/>.
        /// </summary>
        private ShapedBuffer(ReadOnlyMemory<char> text, ArraySlice<GlyphInfo> glyphInfos,
            GlyphTypeface glyphTypeface, double fontRenderingEmSize, sbyte bidiLevel,
            double[] sharedClusterPrefix, int[]? sharedClusterStartChars,
            int clusterStartIdx, int clusterCount)
        {
            Text = text;
            _glyphInfos = glyphInfos;
            GlyphTypeface = glyphTypeface;
            FontRenderingEmSize = fontRenderingEmSize;
            BidiLevel = bidiLevel;
            _clusterPrefix = sharedClusterPrefix;
            _clusterStartChars = sharedClusterStartChars;
            _clusterStartIdx = clusterStartIdx;
            _clusterCount = clusterCount;
            // this buffer inherited the cache from a parent and now also
            // sees it as shared (any mutation would be visible to the parent).
            _cacheShared = true;
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

            ReturnRentedClusterCache();
        }

        /// <summary>
        /// Returns this buffer's owned cluster-cache arrays (if any) to their
        /// shared pools. No-op for split children, which never own the arrays.
        /// Callers must ensure no sibling buffer is still reading the cache;
        /// today that's guaranteed because clearing only happens via
        /// <see cref="Dispose"/> and <see cref="InvalidateClusterCache"/>, the
        /// latter being triggered only by the indexer setter which the contract
        /// already forbids after splits.
        /// </summary>
        private void ReturnRentedClusterCache()
        {
            if (_rentedClusterPrefix is { } prefix)
            {
                ArrayPool<double>.Shared.Return(prefix);
                _rentedClusterPrefix = null;
            }

            if (_rentedClusterStartChars is { } startChars)
            {
                ArrayPool<int>.Shared.Return(startChars);
                _rentedClusterStartChars = null;
            }
        }

        public GlyphInfo this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _glyphInfos[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Debug.Assert(!_cacheShared,
                    "ShapedBuffer indexer must not be written after Split or WithBidiLevel has published the cluster cache to a sibling.");
                _glyphInfos[index] = value;
                InvalidateClusterCache();
            }
        }

        /// <summary>
        /// Test hook: indicates that the cluster cache is using the
        /// one-char-per-cluster fast path (no <c>_clusterStartChars</c> allocation).
        /// Materialises the cache as a side effect, so call after the buffer is
        /// fully populated.
        /// </summary>
        internal bool IsClusterCacheSimple
        {
            get
            {
                EnsureClusterCache();
                return _clusterStartChars is null;
            }
        }

        /// <summary>
        /// Returns the total advance of all glyphs in the buffer (i.e. the buffer's
        /// rendered width). Cached after first access; the value is summed in
        /// logical cluster order, which can differ by ULPs from a visual-order sum
        /// on RTL buffers — fine for layout but tests should use FP-tolerant equality.
        /// </summary>
        internal double TotalGlyphAdvance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var prefix = EnsureClusterCache();
                var start = _clusterStartIdx;
                return prefix[start + _clusterCount] - prefix[start];
            }
        }

        /// <summary>
        /// Finds how many text characters from the start of this buffer fit within
        /// <paramref name="availableWidth"/>, walking in logical cluster order.
        /// Returns the character count and the width consumed (always &lt;= availableWidth
        /// unless the very first cluster overflows, in which case the caller is expected
        /// to honour the overflow contract documented in <c>TextFormatterImpl.MeasureLength</c>).
        /// </summary>
        internal int MeasureCharactersThatFit(double availableWidth, out double widthConsumed)
        {
            var prefix = EnsureClusterCache();
            var startIdx = _clusterStartIdx;
            var count = _clusterCount;

            if (count == 0)
            {
                widthConsumed = 0d;
                return 0;
            }

            // Find the largest k such that prefix[startIdx + k] - prefix[startIdx] <=
            // availableWidth. Standard binary search on the prefix-sum array, with the
            // sub-buffer's base width subtracted out.
            var basePrefix = prefix[startIdx];

            var lo = 0;
            var hi = count;
            while (lo < hi)
            {
                var mid = (lo + hi + 1) >> 1;
                if (prefix[startIdx + mid] - basePrefix <= availableWidth)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            widthConsumed = prefix[startIdx + lo] - basePrefix;

            // Simple-mode fast path: 1 char per cluster, so the consumed char count
            // equals the cluster offset we just resolved (no start-chars table needed).
            var starts = _clusterStartChars;
            if (starts is null)
            {
                return lo;
            }
            return starts[startIdx + lo] - starts[startIdx];
        }

        /// <summary>
        /// Returns the character length of the first logical cluster in this buffer.
        /// Used by <c>MeasureLength</c> to satisfy the "include at least one cluster"
        /// rule when even the first cluster does not fit the paragraph width.
        /// </summary>
        internal int FirstClusterCharLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                EnsureClusterCache();

                if (_clusterCount == 0)
                {
                    return 0;
                }
                var starts = _clusterStartChars;
                if (starts is null)
                {
                    // Simple mode: every cluster is one character wide.
                    return 1;
                }
                var startIdx = _clusterStartIdx;
                return starts[startIdx + 1] - starts[startIdx];
            }
        }

        /// <summary>
        /// Ensures the cluster cache is built in <em>logical</em> order. LTR buffers store glyphs
        /// in ascending cluster order so logical order is the same as visual order
        /// (walk forward from index 0). RTL buffers store glyphs in descending cluster
        /// order — visual order is reverse-logical — so logical order means walking
        /// the underlying array backwards. The output `prefix` and `startChars` are
        /// always in logical order: `startChars[0] == 0`, `startChars[count] == Text.Length`,
        /// and `prefix[count]` equals the total advance.
        /// </summary>
        private double[] EnsureClusterCache()
        {
            if(_clusterPrefix is not null)
            {
                return _clusterPrefix;
            }

            var glyphInfos = _glyphInfos.Span;
            var bufferLength = _glyphInfos.Length;

            if (bufferLength == 0)
            {
                _clusterCount = 0;
                _clusterStartChars = s_emptyStartChars;
                return _clusterPrefix = s_emptyPrefix;
            }

            var isLtr = IsLeftToRight;
            var step = isLtr ? 1 : -1;
            var start = isLtr ? 0 : bufferLength - 1;
            var end = isLtr ? bufferLength : -1;

            var baseCluster = glyphInfos[start].GlyphCluster;
            var textLength = Text.Length;

            // First pass: count clusters by counting cluster-id transitions in
            // logical order. Also track whether this is the "simple" case where
            // bufferLength == textLength == clusters and cluster ids are exactly
            // baseCluster + logicalIndex — i.e. one glyph per cluster, one char
            // per cluster. The check piggybacks the existing iteration so it adds
            // a few cheap comparisons but no allocations.
            var clusters = 1;
            var canBeSimple = bufferLength == textLength;
            // Logical index 0 trivially matches: glyphInfos[start].GlyphCluster - baseCluster == 0.

            {
                var prevId = glyphInfos[start].GlyphCluster;
                var logicalIndex = 1;
                for (var j = start + step; j != end; j += step, logicalIndex++)
                {
                    var id = glyphInfos[j].GlyphCluster;
                    if (id != prevId)
                    {
                        clusters++;
                        prevId = id;
                    }
                    if (canBeSimple && id - baseCluster != logicalIndex)
                    {
                        canBeSimple = false;
                    }
                }
            }

            var simple = canBeSimple && clusters == bufferLength;

            // Rent (possibly oversized) from the shared pools. We track the rented
            // arrays separately from the view references so that splits can share
            // the data without taking ownership.
            var prefix = ArrayPool<double>.Shared.Rent(clusters + 1);
            _rentedClusterPrefix = prefix;

            int[]? startChars = null;
            if (!simple)
            {
                startChars = ArrayPool<int>.Shared.Rent(clusters + 1);
                _rentedClusterStartChars = startChars;
            }

            // Pool-provided arrays come pre-populated with old data; the first
            // prefix slot must be zero (the loop below overwrites every other
            // slot we read, and startChars[0] is set explicitly when needed).
            prefix[0] = 0d;

            var clusterIndex = 0;
            var currentClusterId = baseCluster;
            var currentWidth = 0d;
            if (!simple)
            {
                startChars![0] = 0;
            }

            for (var j = start; j != end; j += step)
            {
                var info = glyphInfos[j];

                if (info.GlyphCluster != currentClusterId)
                {
                    prefix[clusterIndex + 1] = prefix[clusterIndex] + currentWidth;
                    if (!simple)
                    {
                        // Cluster IDs increase in logical order in both directions: for LTR
                        // we walk forward over ascending IDs; for RTL we walk backward over
                        // (visually descending = logically ascending) IDs.
                        startChars![clusterIndex + 1] = info.GlyphCluster - baseCluster;
                    }
                    clusterIndex++;
                    currentClusterId = info.GlyphCluster;
                    currentWidth = info.GlyphAdvance;
                }
                else
                {
                    currentWidth += info.GlyphAdvance;
                }
            }

            // Close the final cluster.
            prefix[clusterIndex + 1] = prefix[clusterIndex] + currentWidth;
            if (!simple)
            {
                startChars![clusterIndex + 1] = textLength;
            }

            _clusterCount = clusters;
            _clusterStartChars = startChars;
            _clusterPrefix = prefix;
            return prefix;
        }

        /// <summary>
        /// Returns the cluster index (relative to this sub-buffer's cache view) at
        /// which a logical text-character offset lands. Split methods snap their
        /// boundaries to whole clusters, so this is an exact match in the cluster
        /// starts table.
        /// </summary>
        private int FindClusterOffsetForSplit(int splitCharCount)
        {
            var starts = _clusterStartChars;
            if (starts is null)
            {
                // Simple mode: cluster index == char offset within this sub-buffer.
                return splitCharCount;
            }

            var startIdx = _clusterStartIdx;
            var count = _clusterCount;

            var targetChar = starts[startIdx] + splitCharCount;

            // Binary search for the first cluster whose start char >= targetChar.
            var lo = 0;
            var hi = count;
            while (lo < hi)
            {
                var mid = (lo + hi) >> 1;
                if (starts[startIdx + mid] < targetChar)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }
            return lo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvalidateClusterCache()
        {
            // Only this buffer's owned arrays go back to the pool; if we
            // inherited the cache from a parent via Split, the parent still
            // owns those arrays and is responsible for returning them.
            ReturnRentedClusterCache();
            _clusterPrefix = null;
            _clusterStartChars = null;
            _clusterStartIdx = 0;
            _clusterCount = 0;
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

            var alias = new ShapedBuffer(Text, _glyphInfos, GlyphTypeface, FontRenderingEmSize, paragraphEmbeddingLevel);

            // if the source buffer has already built its cluster cache,
            // hand the data references to the alias so it doesn't have to
            // rebuild. The alias shares the same glyph order (BidiLevel only
            // affects how callers interpret the buffer, not the underlying
            // layout produced by the shaper), so the logical-order prefix sums
            // are identical. The rented handles stay with the original owner.
            if (_clusterPrefix is not null)
            {
                alias._clusterPrefix = _clusterPrefix;
                alias._clusterStartChars = _clusterStartChars;
                alias._clusterStartIdx = _clusterStartIdx;
                alias._clusterCount = _clusterCount;
                _cacheShared = true;
                alias._cacheShared = true;
            }

            return alias;
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

            // Ensure parent's cluster cache exists, then hand sub-views of
            // it to the children. Splitting is O(1) per child instead of O(glyphs).
            var sharedPrefix = EnsureClusterCache();
            var sharedStarts = _clusterStartChars; // may be null in simple mode
            var leadingClusterCount = FindClusterOffsetForSplit(splitCharCount);
            _cacheShared = true;

            var leading = new ShapedBuffer(
                firstText, firstGlyphs,
                GlyphTypeface, FontRenderingEmSize, BidiLevel,
                sharedPrefix, sharedStarts, _clusterStartIdx, leadingClusterCount);

            if (secondText.Length == 0)
            {
                return new SplitResult<ShapedBuffer>(leading, null);
            }

            var trailing = new ShapedBuffer(
                secondText, secondGlyphs,
                GlyphTypeface, FontRenderingEmSize, BidiLevel,
                sharedPrefix, sharedStarts,
                _clusterStartIdx + leadingClusterCount, _clusterCount - leadingClusterCount);

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

            // share the parent's cluster cache. The cache stores
            // clusters in logical order, so "first" (text[0..textLength]) gets the
            // leading slice and "second" gets the trailing slice — same indexing
            // as the LTR case.
            var sharedPrefix = EnsureClusterCache();
            var sharedStarts = _clusterStartChars; // may be null in simple mode
            var firstClusterCount = FindClusterOffsetForSplit(textLength);
            _cacheShared = true;

            var first = new ShapedBuffer(
                firstText, firstGlyphs,
                GlyphTypeface, FontRenderingEmSize, BidiLevel,
                sharedPrefix, sharedStarts, _clusterStartIdx, firstClusterCount);

            if (secondText.Length == 0 || secondGlyphs.Length == 0)
            {
                return new SplitResult<ShapedBuffer>(first, null);
            }

            var second = new ShapedBuffer(
                secondText, secondGlyphs,
                GlyphTypeface, FontRenderingEmSize, BidiLevel,
                sharedPrefix, sharedStarts,
                _clusterStartIdx + firstClusterCount, _clusterCount - firstClusterCount);

            return new SplitResult<ShapedBuffer>(first, second);
        }
    }
}
