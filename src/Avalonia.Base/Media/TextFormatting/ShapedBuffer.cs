using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    public sealed class ShapedBuffer : IReadOnlyList<GlyphInfo>, IDisposable
    {
        private GlyphInfo[]? _rentedBuffer;
        private ArraySlice<GlyphInfo> _glyphInfos;

        // Pool-array ownership. The instance that called ArrayPool.Rent owns the
        // array via _rentedBuffer and has _arrayOwner == this with _refCount == 1.
        // Every view that shares the same array (via Split / WithBidiLevel) sets
        // _arrayOwner to that owner and increments owner._refCount. Dispose
        // decrements the owner's refcount; the array is only returned to the
        // pool when the last reference drops. _arrayOwner stays null for
        // instances backed by a GC-managed (non-pooled) array passed in by
        // a caller — disposing those is a no-op.
        private ShapedBuffer? _arrayOwner;
        private int _refCount;

        // Lazily-computed cluster-width cache (A1). MeasureLength and metrics
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
        // Sums in this prefix are in logical (not visual) order, which can
        // differ by ULPs from a visual-order sum for RTL buffers. Consumers
        // comparing layout dimensions should use tolerant equality.
        private double[]? _clusterPrefix;
        private int[]? _clusterStartChars;
        private int _clusterStartIdx;
        private int _clusterCount;

        public ShapedBuffer(ReadOnlyMemory<char> text, int bufferLength, GlyphTypeface glyphTypeface, double fontRenderingEmSize, sbyte bidiLevel)
        {
            Text = text;
            _rentedBuffer = ArrayPool<GlyphInfo>.Shared.Rent(bufferLength);
            _glyphInfos = new ArraySlice<GlyphInfo>(_rentedBuffer, 0, bufferLength);
            GlyphTypeface = glyphTypeface;
            FontRenderingEmSize = fontRenderingEmSize;
            BidiLevel = bidiLevel;
            _arrayOwner = this;
            _refCount = 1;
        }

        /// <summary>
        /// Constructs a buffer backed by a caller-supplied, GC-managed glyph array.
        /// The buffer takes no pool ownership and <see cref="Dispose"/> is a no-op.
        /// Used for synthetic single-glyph runs where the caller owns the array's
        /// lifetime directly.
        /// </summary>
        internal ShapedBuffer(ReadOnlyMemory<char> text, ArraySlice<GlyphInfo> glyphInfos, GlyphTypeface glyphTypeface, double fontRenderingEmSize, sbyte bidiLevel)
        {
            Text = text;
            _glyphInfos = glyphInfos;
            GlyphTypeface = glyphTypeface;
            FontRenderingEmSize = fontRenderingEmSize;
            BidiLevel = bidiLevel;
            // _arrayOwner stays null: nothing pooled, nothing to refcount.
        }

        /// <summary>
        /// Constructs a view over another buffer's array. Used by
        /// <see cref="WithBidiLevel"/> and by <see cref="Split"/> for views that
        /// don't carry the cluster cache. The view shares pool ownership with
        /// <paramref name="arrayOwnerSource"/>: incrementing the owner's
        /// refcount keeps the pool array alive until this view is also disposed.
        /// </summary>
        private ShapedBuffer(ShapedBuffer arrayOwnerSource, ReadOnlyMemory<char> text, ArraySlice<GlyphInfo> glyphInfos, GlyphTypeface glyphTypeface, double fontRenderingEmSize, sbyte bidiLevel)
        {
            Text = text;
            _glyphInfos = glyphInfos;
            GlyphTypeface = glyphTypeface;
            FontRenderingEmSize = fontRenderingEmSize;
            BidiLevel = bidiLevel;
            AcquireArrayOwner(arrayOwnerSource);
        }

        /// <summary>
        /// Constructs a view over another buffer's array that also inherits the
        /// shared cluster-width cache. Used by <see cref="Split"/>'s
        /// <c>SplitAscending</c> / <c>SplitDescending</c> paths so the cache
        /// (built once on the owner) is shared across all post-split sub-buffers.
        /// Same lifetime semantics as the other view constructor.
        /// </summary>
        private ShapedBuffer(ShapedBuffer arrayOwnerSource, ReadOnlyMemory<char> text, ArraySlice<GlyphInfo> glyphInfos,
            GlyphTypeface glyphTypeface, double fontRenderingEmSize, sbyte bidiLevel,
            double[] sharedClusterPrefix, int[] sharedClusterStartChars,
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
            AcquireArrayOwner(arrayOwnerSource);
        }

        /// <summary>
        /// Adopt <paramref name="source"/>'s pool-array owner (chain-root) and
        /// take a reference on it. No-op when the source isn't backed by a
        /// pooled array.
        /// </summary>
        private void AcquireArrayOwner(ShapedBuffer source)
        {
            var owner = source._arrayOwner;
            if (owner != null)
            {
                Interlocked.Increment(ref owner._refCount);
                _arrayOwner = owner;
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

        /// <summary>
        /// Test hook: <c>true</c> while this instance still holds an
        /// <see cref="ArrayPool{T}"/>-rented <c>GlyphInfo[]</c>. Flips to
        /// <c>false</c> once the array is returned to the pool — either when
        /// the last reference to a pool-backed buffer is disposed, or for
        /// instances that were constructed with a caller-supplied GC-managed
        /// array (which never had pool ownership in the first place).
        /// </summary>
        internal bool IsPoolArrayRented => _rentedBuffer is not null;

        public void Dispose()
        {
            var owner = _arrayOwner;
            if (owner == null)
            {
                // Either already disposed (idempotent path) or a non-pooled
                // (GC-managed) buffer that owns no ArrayPool resource.
                return;
            }

            // Release THIS view's reference. Two-step so a double-Dispose on the
            // same instance is a no-op the second time.
            _arrayOwner = null;
            _glyphInfos = ArraySlice<GlyphInfo>.Empty;

            if (Interlocked.Decrement(ref owner._refCount) != 0)
            {
                // Other views still hold the array; don't return it yet.
                return;
            }

            // Last reference — actually return the pool array.
            if (owner._rentedBuffer is not null)
            {
                ArrayPool<GlyphInfo>.Shared.Return(owner._rentedBuffer);
                owner._rentedBuffer = null;
                owner._glyphInfos = ArraySlice<GlyphInfo>.Empty;
            }
        }

        public GlyphInfo this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _glyphInfos[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                _glyphInfos[index] = value;
                InvalidateClusterCache();
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
                var prefix = _clusterPrefix ?? EnsureClusterCache();
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
            var prefix = _clusterPrefix ?? EnsureClusterCache();
            var starts = _clusterStartChars!;
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
            var baseChar = starts[startIdx];

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
            return starts[startIdx + lo] - baseChar;
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
                _ = _clusterPrefix ?? EnsureClusterCache();
                if (_clusterCount == 0)
                {
                    return 0;
                }
                var starts = _clusterStartChars!;
                var startIdx = _clusterStartIdx;
                return starts[startIdx + 1] - starts[startIdx];
            }
        }

        /// <summary>
        /// Returns the width consumed by the first <paramref name="charCount"/> characters,
        /// using the cluster cache. The result is the prefix advance up to the first cluster
        /// whose start character is &gt;= <paramref name="charCount"/>.
        /// </summary>
        internal double GetWidthForCharCount(int charCount)
        {
            if (charCount <= 0)
            {
                return 0d;
            }

            var prefix = _clusterPrefix ?? EnsureClusterCache();
            var starts = _clusterStartChars!;
            var startIdx = _clusterStartIdx;
            var count = _clusterCount;

            var basePrefix = prefix[startIdx];
            var baseChar = starts[startIdx];

            // Cluster starts are non-decreasing within the sub-buffer's range.
            // Linear search from the end; typical callers pass the line's full length
            // and hit the last cluster immediately.
            for (var i = count; i >= 0; i--)
            {
                if (starts[startIdx + i] - baseChar <= charCount)
                {
                    return prefix[startIdx + i] - basePrefix;
                }
            }
            return 0d;
        }

        /// <summary>
        /// Finds the largest <c>N</c> such that the first <c>N</c> logical
        /// characters of this sub-buffer fit within <paramref name="availableWidth"/>.
        /// Cluster-atomic: a multi-glyph cluster either fits completely or not at
        /// all. Returns 0 if <paramref name="availableWidth"/> is non-positive
        /// or the first cluster's width already exceeds it.
        /// </summary>
        /// <remarks>
        /// Walks the cluster cache (built in logical order for both LTR and RTL
        /// buffers) via binary search, so each call is O(log clusters) and the
        /// returned count is the correct logical-leading char count regardless
        /// of the buffer's visual direction.
        /// </remarks>
        internal int FindLeadingCharCountWithinWidth(double availableWidth)
        {
            if (availableWidth <= 0)
            {
                return 0;
            }

            var prefix = _clusterPrefix ?? EnsureClusterCache();
            var starts = _clusterStartChars!;
            var startIdx = _clusterStartIdx;
            var count = _clusterCount;

            var basePrefix = prefix[startIdx];
            var baseChar = starts[startIdx];

            // Largest k in [0, count] with prefix[startIdx + k] - basePrefix <= availableWidth.
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

            return starts[startIdx + lo] - baseChar;
        }

        /// <summary>
        /// Finds the largest <c>N</c> such that the last <c>N</c> logical
        /// characters of this sub-buffer fit within <paramref name="availableWidth"/>.
        /// Cluster-atomic; <paramref name="consumedWidth"/> reports the actual
        /// cumulative advance of those <c>N</c> chars.
        /// </summary>
        /// <remarks>
        /// O(log clusters) via the cluster cache; direction-agnostic (cache is
        /// always in logical order). The returned count is the logical-trailing
        /// char count regardless of whether the buffer is LTR or RTL.
        /// </remarks>
        internal int FindTrailingCharCountWithinWidth(double availableWidth, out double consumedWidth)
        {
            consumedWidth = 0;

            if (availableWidth <= 0)
            {
                return 0;
            }

            var prefix = _clusterPrefix ?? EnsureClusterCache();
            var starts = _clusterStartChars!;
            var startIdx = _clusterStartIdx;
            var count = _clusterCount;

            var endPrefix = prefix[startIdx + count];
            var endChar = starts[startIdx + count];

            // Smallest k in [0, count] with endPrefix - prefix[startIdx + k] <= availableWidth.
            // (That cluster index marks where the trailing-fitting suffix starts.)
            var lo = 0;
            var hi = count;
            while (lo < hi)
            {
                var mid = (lo + hi) >> 1;
                if (endPrefix - prefix[startIdx + mid] <= availableWidth)
                {
                    hi = mid;
                }
                else
                {
                    lo = mid + 1;
                }
            }

            consumedWidth = endPrefix - prefix[startIdx + lo];
            return endChar - starts[startIdx + lo];
        }

        /// <summary>
        /// Returns the cumulative glyph advance for the logical character range
        /// <c>[<paramref name="startChar"/>, <paramref name="endChar"/>)</c>
        /// within this sub-buffer. Uses the cluster cache via binary search, so
        /// each call is O(log clusters) regardless of how big the buffer is or
        /// where the range sits inside it.
        /// </summary>
        /// <remarks>
        /// The cluster cache is built in <i>logical</i> order for both LTR and
        /// RTL buffers (see <see cref="EnsureClusterCache"/>), so callers pass
        /// logical char offsets and the same code path serves both directions.
        /// Out-of-range arguments are clamped to <c>[0, Text.Length]</c>.
        /// </remarks>
        internal double GetCharRangeWidth(int startChar, int endChar)
        {
            if (endChar <= startChar)
            {
                return 0d;
            }

            var prefix = _clusterPrefix ?? EnsureClusterCache();
            var starts = _clusterStartChars!;
            var startIdx = _clusterStartIdx;
            var count = _clusterCount;

            var basePrefix = prefix[startIdx];
            var baseChar = starts[startIdx];

            var startBoundary = FindLargestClusterAtOrBefore(starts, startIdx, count, baseChar, startChar);
            var endBoundary = FindLargestClusterAtOrBefore(starts, startIdx, count, baseChar, endChar);

            return prefix[startIdx + endBoundary] - prefix[startIdx + startBoundary];
        }

        /// <summary>
        /// Binary-search the largest cluster boundary index <c>i ∈ [0, count]</c>
        /// such that <c>starts[startIdx + i] - baseChar ≤ charPos</c>. Cluster
        /// starts are non-decreasing within the sub-buffer range, so a standard
        /// upper-bound search works in both LTR and RTL buffers (the cache is
        /// always built in logical order).
        /// </summary>
        private static int FindLargestClusterAtOrBefore(int[] starts, int startIdx, int count, int baseChar, int charPos)
        {
            if (charPos < 0)
            {
                return 0;
            }

            // Standard "rightmost <= charPos" upper-bound shape.
            var lo = 0;
            var hi = count;
            while (lo < hi)
            {
                var mid = (lo + hi + 1) >> 1;
                if (starts[startIdx + mid] - baseChar <= charPos)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            return lo;
        }

        /// <summary>
        /// Builds the cluster cache in <em>logical</em> order. LTR buffers store glyphs
        /// in ascending cluster order so logical order is the same as visual order
        /// (walk forward from index 0). RTL buffers store glyphs in descending cluster
        /// order — visual order is reverse-logical — so logical order means walking
        /// the underlying array backwards. The output `prefix` and `startChars` are
        /// always in logical order: `startChars[0] == 0`, `startChars[count] == Text.Length`,
        /// and `prefix[count]` equals the total advance.
        /// </summary>
        private double[] EnsureClusterCache()
        {
            var glyphInfos = _glyphInfos.Span;
            var bufferLength = _glyphInfos.Length;

            if (bufferLength == 0)
            {
                _clusterCount = 0;
                _clusterStartChars = new[] { 0 };
                return _clusterPrefix = new[] { 0d };
            }

            var isLtr = IsLeftToRight;
            var step = isLtr ? 1 : -1;
            var start = isLtr ? 0 : bufferLength - 1;
            var end = isLtr ? bufferLength : -1;

            // First pass: count clusters by counting cluster-id transitions in
            // logical order.
            var clusters = 1;
            for (int j = start + step, prevId = glyphInfos[start].GlyphCluster; j != end; j += step)
            {
                var id = glyphInfos[j].GlyphCluster;
                if (id != prevId)
                {
                    clusters++;
                    prevId = id;
                }
            }

            var prefix = new double[clusters + 1];
            var startChars = new int[clusters + 1];

            // The first logical glyph's cluster value is the absolute source-text
            // offset of the run's first character. We anchor character offsets here.
            var baseCluster = glyphInfos[start].GlyphCluster;
            var textLength = Text.Length;

            var clusterIndex = 0;
            var currentClusterId = baseCluster;
            var currentWidth = 0d;
            startChars[0] = 0;

            for (var j = start; j != end; j += step)
            {
                var info = glyphInfos[j];

                if (info.GlyphCluster != currentClusterId)
                {
                    prefix[clusterIndex + 1] = prefix[clusterIndex] + currentWidth;
                    // Cluster IDs increase in logical order in both directions: for LTR
                    // we walk forward over ascending IDs; for RTL we walk backward over
                    // (visually descending = logically ascending) IDs.
                    startChars[clusterIndex + 1] = info.GlyphCluster - baseCluster;
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
            startChars[clusterIndex + 1] = textLength;

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
            var starts = _clusterStartChars!;
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

            // View into our glyph array — share pool ownership so the array
            // outlives both `this` and the new view independently.
            return new ShapedBuffer(this, Text, _glyphInfos, GlyphTypeface, FontRenderingEmSize, paragraphEmbeddingLevel);
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
                    this, Text.Slice(0, 0), _glyphInfos.Slice(_glyphInfos.Start, 0),
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

            // A1-followup: ensure parent's cluster cache exists, then hand sub-views of
            // it to the children. Splitting is O(1) per child instead of O(glyphs).
            var sharedPrefix = _clusterPrefix ?? EnsureClusterCache();
            var sharedStarts = _clusterStartChars!;
            var leadingClusterCount = FindClusterOffsetForSplit(splitCharCount);

            var leading = new ShapedBuffer(
                this, firstText, firstGlyphs,
                GlyphTypeface, FontRenderingEmSize, BidiLevel,
                sharedPrefix, sharedStarts, _clusterStartIdx, leadingClusterCount);

            if (secondText.Length == 0)
            {
                return new SplitResult<ShapedBuffer>(leading, null);
            }

            var trailing = new ShapedBuffer(
                this, secondText, secondGlyphs,
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

            // A1-followup: share the parent's cluster cache. The cache stores
            // clusters in logical order, so "first" (text[0..textLength]) gets the
            // leading slice and "second" gets the trailing slice — same indexing
            // as the LTR case.
            var sharedPrefix = _clusterPrefix ?? EnsureClusterCache();
            var sharedStarts = _clusterStartChars!;
            var firstClusterCount = FindClusterOffsetForSplit(textLength);

            var first = new ShapedBuffer(
                this, firstText, firstGlyphs,
                GlyphTypeface, FontRenderingEmSize, BidiLevel,
                sharedPrefix, sharedStarts, _clusterStartIdx, firstClusterCount);

            if (secondText.Length == 0 || secondGlyphs.Length == 0)
            {
                return new SplitResult<ShapedBuffer>(first, null);
            }

            var second = new ShapedBuffer(
                this, secondText, secondGlyphs,
                GlyphTypeface, FontRenderingEmSize, BidiLevel,
                sharedPrefix, sharedStarts,
                _clusterStartIdx + firstClusterCount, _clusterCount - firstClusterCount);

            return new SplitResult<ShapedBuffer>(first, second);
        }
    }
}
