using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    public sealed class ShapedBuffer : IReadOnlyList<GlyphInfo>, IDisposable
    {
        /// <summary>
        /// Disposable wrapper around an <see cref="ArrayPool{T}"/>-rented array.
        /// Combined with <see cref="IRef{T}"/> this gives <see cref="ShapedBuffer"/>
        /// shared, ref-counted ownership of its glyph and cluster-cache storage so
        /// that <see cref="Split"/> and <see cref="WithBidiLevel"/> can safely alias
        /// the same backing arrays.
        /// </summary>
        internal sealed class PooledArray<T> : IDisposable
        {
            private T[]? _array;
            private int _generation;

            public PooledArray(int minLength)
            {
                _array = ArrayPool<T>.Shared.Rent(minLength);
            }

            public T[] Array => _array ?? throw new ObjectDisposedException(nameof(PooledArray<T>));

            /// <summary>
            /// Monotonically increasing version stamp. Bumped whenever the underlying
            /// data is mutated so siblings sharing this holder can detect that any
            /// cache derived from the data is stale and must be rebuilt.
            /// </summary>
            public int Generation => Volatile.Read(ref _generation);

            public void BumpGeneration() => Interlocked.Increment(ref _generation);

            public void Dispose()
            {
                var arr = Interlocked.Exchange(ref _array, null);
                if (arr is not null)
                {
                    ArrayPool<T>.Shared.Return(arr);
                }
            }
        }

        // Ref-counted handle to the pooled GlyphInfo[] that backs _glyphInfos.
        // Null when the buffer was constructed over caller-owned storage
        // (e.g. an externally allocated array for the empty-line synthetic run).
        // Split children and WithBidiLevel aliases clone this ref so the array
        // survives until every observer has been disposed.
        private IRef<PooledArray<GlyphInfo>>? _glyphRef;

        // Ref-counted handle to the pooled ushort[] that backs _glyphIndices. The
        // parallel glyph-id array exists so consumers (GlyphRunImpl, the new
        // TryGetGlyphBounds batch path, SKFont.GetGlyphWidths) can take a
        // ReadOnlySpan<ushort> over the run's glyph IDs without walking the
        // GlyphInfo struct array. Lifetime mirrors _glyphRef: cloned on Split /
        // WithBidiLevel, disposed in Dispose, null on caller-owned storage.
        private IRef<PooledArray<ushort>>? _glyphIndicesRef;
        private ArraySlice<GlyphInfo> _glyphInfos;
        private ArraySlice<ushort> _glyphIndices;

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
        // Pooling: the cluster-cache arrays are reached through ref-counted
        // <see cref="IRef{PooledArray}"/> handles. The hot-path read fields
        // <c>_clusterPrefix</c> and <c>_clusterStartChars</c> alias the rented
        // arrays directly so measure/split loops stay indirection-free. Split
        // children and <see cref="WithBidiLevel"/> aliases clone the refs, so the
        // arrays survive until every sibling is disposed; the pool may hand back
        // larger arrays than requested so we always read through bounded indices
        // (_clusterStartIdx + [0.._clusterCount]).
        private double[]? _clusterPrefix;
        private int[]? _clusterStartChars;
        private IRef<PooledArray<double>>? _prefixRef;
        private IRef<PooledArray<int>>? _startsRef;
        private int _clusterStartIdx;
        private int _clusterCount;

        // Generation observed on the shared glyph holder when this buffer's
        // cluster cache was built. If the holder's current generation differs,
        // a sibling has mutated the glyph array since and we must rebuild.
        // Zero is the initial value for both fields, so a freshly populated
        // cache against an unmutated holder matches without extra bookkeeping.
        private int _cacheGeneration;

        // Guard so Dispose is idempotent. Multiple Dispose calls (e.g. via
        // cache eviction overlapping with TextLine teardown) only release the
        // ref-counted handles once.
        private bool _disposed;

        private static readonly int[] s_emptyStartChars = new[] { 0 };
        private static readonly double[] s_emptyPrefix = new[] { 0d };

        public ShapedBuffer(ReadOnlyMemory<char> text, int bufferLength, GlyphTypeface glyphTypeface, double fontRenderingEmSize, sbyte bidiLevel)
        {
            Text = text;
            _glyphRef = RefCountable.Create(new PooledArray<GlyphInfo>(bufferLength));
            _glyphInfos = new ArraySlice<GlyphInfo>(_glyphRef.Item.Array, 0, bufferLength);
            _glyphIndicesRef = RefCountable.Create(new PooledArray<ushort>(bufferLength));
            _glyphIndices = new ArraySlice<ushort>(_glyphIndicesRef.Item.Array, 0, bufferLength);
            GlyphTypeface = glyphTypeface;
            FontRenderingEmSize = fontRenderingEmSize;
            BidiLevel = bidiLevel;
        }

        internal ShapedBuffer(ReadOnlyMemory<char> text, ArraySlice<GlyphInfo> glyphInfos, ArraySlice<ushort> glyphIndices, GlyphTypeface glyphTypeface, double fontRenderingEmSize, sbyte bidiLevel)
        {
            Text = text;
            _glyphInfos = glyphInfos;
            _glyphIndices = glyphIndices;
            GlyphTypeface = glyphTypeface;
            FontRenderingEmSize = fontRenderingEmSize;
            BidiLevel = bidiLevel;
        }

        /// <summary>
        /// Internal constructor used by <see cref="Split"/> and <see cref="WithBidiLevel"/> to
        /// alias the source buffer's pooled glyph storage and (optionally) cluster cache.
        /// Each non-null ref is <see cref="IRef{T}.Clone"/>d so the underlying arrays survive
        /// until every sibling has been disposed. When <paramref name="sourcePrefixRef"/>
        /// is null the alias starts without a cluster cache and will build its own lazily.
        /// </summary>
        private ShapedBuffer(ReadOnlyMemory<char> text,
            ArraySlice<GlyphInfo> glyphInfos, ArraySlice<ushort> glyphIndices,
            GlyphTypeface glyphTypeface, double fontRenderingEmSize, sbyte bidiLevel,
            IRef<PooledArray<GlyphInfo>>? sourceGlyphRef,
            IRef<PooledArray<ushort>>? sourceGlyphIndicesRef,
            IRef<PooledArray<double>>? sourcePrefixRef,
            IRef<PooledArray<int>>? sourceStartsRef,
            int clusterStartIdx, int clusterCount, int sourceCacheGeneration)
        {
            Text = text;
            _glyphInfos = glyphInfos;
            _glyphIndices = glyphIndices;
            GlyphTypeface = glyphTypeface;
            FontRenderingEmSize = fontRenderingEmSize;
            BidiLevel = bidiLevel;
            _glyphRef = sourceGlyphRef?.Clone();
            _glyphIndicesRef = sourceGlyphIndicesRef?.Clone();

            if (sourcePrefixRef is not null)
            {
                _prefixRef = sourcePrefixRef.Clone();
                _clusterPrefix = _prefixRef.Item.Array;

                if (sourceStartsRef is not null)
                {
                    _startsRef = sourceStartsRef.Clone();
                    _clusterStartChars = _startsRef.Item.Array;
                }

                _clusterStartIdx = clusterStartIdx;
                _clusterCount = clusterCount;
                _cacheGeneration = sourceCacheGeneration;
            }
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
        /// Contiguous view of the glyph indices for this buffer, kept in sync with
        /// <see cref="GlyphInfos"/> by the indexer setter. Consumers needing a
        /// <see cref="ReadOnlySpan{T}"/> of glyph IDs (e.g. for
        /// <c>GlyphTypeface.TryGetGlyphBounds</c> or
        /// <c>SKFont.GetGlyphWidths</c>) can use this directly without allocating
        /// a parallel array.
        /// </summary>
        public ReadOnlySpan<ushort> GlyphIndices => _glyphIndices.Span;

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
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            _glyphRef?.Dispose();
            _glyphRef = null;
            _glyphInfos = ArraySlice<GlyphInfo>.Empty; // ensure we don't misuse a returned array

            _glyphIndicesRef?.Dispose();
            _glyphIndicesRef = null;
            _glyphIndices = ArraySlice<ushort>.Empty;

            ReleaseClusterCacheRefs();
            _clusterPrefix = null;
            _clusterStartChars = null;
            _clusterStartIdx = 0;
            _clusterCount = 0;
        }

        /// <summary>
        /// Releases this buffer's ref-counted handles to the cluster-cache arrays
        /// (if any). The underlying arrays are only returned to the pool when the
        /// last sibling releases its handle, so this is safe to call from both
        /// <see cref="Dispose"/> and <see cref="InvalidateClusterCache"/>.
        /// </summary>
        private void ReleaseClusterCacheRefs()
        {
            _prefixRef?.Dispose();
            _prefixRef = null;
            _startsRef?.Dispose();
            _startsRef = null;
        }

        public GlyphInfo this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _glyphInfos[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _glyphInfos[index] = value;
                _glyphIndices[index] = value.GlyphIndex;
                // Bump the shared glyph generation so any sibling that built a
                // cluster cache against the pre-mutation glyphs will detect the
                // mismatch on its next EnsureClusterCache call and rebuild.
                _glyphRef?.Item.BumpGeneration();
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
        /// Test hook: exposes the backing cluster-prefix array reference so unit
        /// tests can assert that <see cref="Split"/> / <see cref="WithBidiLevel"/>
        /// aliases share the parent's cache rather than rebuilding their own.
        /// Returns null when no cache has been built yet.
        /// </summary>
        internal double[]? ClusterPrefix => _clusterPrefix;

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
                if (MathUtilities.LessThanOrClose(prefix[startIdx + mid] - basePrefix, availableWidth))
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
            var currentGeneration = _glyphRef?.Item.Generation ?? 0;
            if (_clusterPrefix is not null)
            {
                if (_cacheGeneration == currentGeneration)
                {
                    return _clusterPrefix;
                }
                // A sibling mutated the shared glyph array since this cache was
                // built — drop the stale view (releasing our refs; other
                // siblings keep their clones alive) and rebuild below against
                // the current glyph data.
                InvalidateClusterCache();
            }

            var glyphInfos = _glyphInfos.Span;
            var bufferLength = _glyphInfos.Length;

            if (bufferLength == 0)
            {
                _clusterCount = 0;
                _clusterStartChars = s_emptyStartChars;
                _cacheGeneration = currentGeneration;
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

            // Rent (possibly oversized) ref-counted holders from the shared pools.
            // Split children / WithBidiLevel aliases will Clone these refs so the
            // arrays survive until every sibling has released them.
            var prefixHolder = new PooledArray<double>(clusters + 1);
            _prefixRef = RefCountable.Create(prefixHolder);
            var prefix = prefixHolder.Array;

            int[]? startChars = null;
            if (!simple)
            {
                var startsHolder = new PooledArray<int>(clusters + 1);
                _startsRef = RefCountable.Create(startsHolder);
                startChars = startsHolder.Array;
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
            _cacheGeneration = currentGeneration;
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
            // Releasing our refs is safe even when siblings hold clones — the
            // underlying pool arrays survive until the last ref is disposed.
            // After invalidation a fresh cache will be rented on the next
            // EnsureClusterCache call (independent from any sibling's view).
            ReleaseClusterCacheRefs();
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

            // If we already have a populated cluster cache, hand the alias its
            // own clones of the ref-counted holders so it can read the prefix
            // sums without rebuilding. BidiLevel only affects how callers
            // interpret the buffer, not the shaper's glyph order, so the
            // logical-order prefix sums are identical.
            IRef<PooledArray<double>>? prefixRef = null;
            IRef<PooledArray<int>>? startsRef = null;
            var startIdx = 0;
            var count = 0;
            if (_prefixRef is not null)
            {
                prefixRef = _prefixRef;
                startsRef = _startsRef;
                startIdx = _clusterStartIdx;
                count = _clusterCount;
            }

            return new ShapedBuffer(
                Text, _glyphInfos, _glyphIndices, GlyphTypeface, FontRenderingEmSize, paragraphEmbeddingLevel,
                _glyphRef, _glyphIndicesRef, prefixRef, startsRef, startIdx, count, _cacheGeneration);
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
                    Text.Slice(0, 0),
                    _glyphInfos.Slice(_glyphInfos.Start, 0),
                    _glyphIndices.Slice(_glyphIndices.Start, 0),
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
            var firstGlyphIndices = _glyphIndices.Slice(sliceStart, splitGlyphIndex);
            var secondGlyphIndices = _glyphIndices.Slice(sliceStart + splitGlyphIndex, glyphInfosLength - splitGlyphIndex);

            var firstText = Text.Slice(0, splitCharCount);
            var secondText = Text.Slice(splitCharCount);

            // Ensure parent's cluster cache exists, then hand sub-views of
            // it to the children. Splitting is O(1) per child instead of O(glyphs).
            EnsureClusterCache();
            var leadingClusterCount = FindClusterOffsetForSplit(splitCharCount);

            var leading = new ShapedBuffer(
                firstText, firstGlyphs, firstGlyphIndices,
                GlyphTypeface, FontRenderingEmSize, BidiLevel,
                _glyphRef, _glyphIndicesRef, _prefixRef, _startsRef,
                _clusterStartIdx, leadingClusterCount, _cacheGeneration);

            if (secondText.Length == 0)
            {
                return new SplitResult<ShapedBuffer>(leading, null);
            }

            var trailing = new ShapedBuffer(
                secondText, secondGlyphs, secondGlyphIndices,
                GlyphTypeface, FontRenderingEmSize, BidiLevel,
                _glyphRef, _glyphIndicesRef, _prefixRef, _startsRef,
                _clusterStartIdx + leadingClusterCount, _clusterCount - leadingClusterCount, _cacheGeneration);

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
            var secondGlyphIndices = _glyphIndices.Slice(sliceStart, splitGlyphIndex);
            var firstGlyphIndices = _glyphIndices.Slice(sliceStart + splitGlyphIndex, glyphInfosLength - splitGlyphIndex);

            var firstText = Text.Slice(0, textLength);
            var secondText = Text.Slice(textLength);

            // share the parent's cluster cache. The cache stores
            // clusters in logical order, so "first" (text[0..textLength]) gets the
            // leading slice and "second" gets the trailing slice — same indexing
            // as the LTR case.
            EnsureClusterCache();
            var firstClusterCount = FindClusterOffsetForSplit(textLength);

            var first = new ShapedBuffer(
                firstText, firstGlyphs, firstGlyphIndices,
                GlyphTypeface, FontRenderingEmSize, BidiLevel,
                _glyphRef, _glyphIndicesRef, _prefixRef, _startsRef,
                _clusterStartIdx, firstClusterCount, _cacheGeneration);

            if (secondText.Length == 0 || secondGlyphs.Length == 0)
            {
                return new SplitResult<ShapedBuffer>(first, null);
            }

            var second = new ShapedBuffer(
                secondText, secondGlyphs, secondGlyphIndices,
                GlyphTypeface, FontRenderingEmSize, BidiLevel,
                _glyphRef, _glyphIndicesRef, _prefixRef, _startsRef,
                _clusterStartIdx + firstClusterCount, _clusterCount - firstClusterCount, _cacheGeneration);

            return new SplitResult<ShapedBuffer>(first, second);
        }
    }
}
