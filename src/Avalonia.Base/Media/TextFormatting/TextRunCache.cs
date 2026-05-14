using System;
using System.Buffers;
using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Caches shaped text runs and bidi processing results to avoid redundant shaping
    /// when only the paragraph width constraint changes (e.g., between Measure and Arrange).
    /// </summary>
    /// <remarks>
    /// Uses an inline single-entry store for the common case of a single paragraph,
    /// and only promotes to a dictionary when multiple entries are added.
    /// </remarks>
    [Unstable("This API is in preview and subject to change without deprecation.")]
    public class TextRunCache : IDisposable
    {
        // Single-entry inline store (avoids Dictionary allocation for the common single-paragraph case).
        private bool _hasSingleEntry;
        private int _singleKey;
        private CachedShapingResult _singleValue;

        // Multi-entry store (only allocated when 2+ distinct keys are added).
        private Dictionary<int, CachedShapingResult>? _entries;

        /// <summary>
        /// Invalidates all cached entries and disposes their shaped buffers.
        /// </summary>
        public void Invalidate()
        {
            if (_hasSingleEntry)
            {
                DisposeCachedRuns(_singleValue);
                _hasSingleEntry = false;
                _singleValue = default;
                return;
            }

            if (_entries == null)
            {
                return;
            }

            foreach (var entry in _entries.Values)
            {
                DisposeCachedRuns(entry);
            }

            _entries.Clear();
        }

        /// <summary>
        /// Invalidates all cached entries at or after the specified text source index.
        /// </summary>
        /// <param name="textSourceIndex">The text source index from which to invalidate.</param>
        public void InvalidateFrom(int textSourceIndex)
        {
            if (_hasSingleEntry)
            {
                if (_singleKey >= textSourceIndex)
                {
                    DisposeCachedRuns(_singleValue);
                    _hasSingleEntry = false;
                    _singleValue = default;
                }

                return;
            }

            if (_entries == null || _entries.Count == 0)
            {
                return;
            }

            var count = _entries.Count;
            int[]? rented = null;

            Span<int> keysToRemove = count <= 16
                ? stackalloc int[16]
                : (rented = ArrayPool<int>.Shared.Rent(count));

            var removeCount = 0;

            foreach (var key in _entries.Keys)
            {
                if (key >= textSourceIndex)
                {
                    keysToRemove[removeCount++] = key;
                }
            }

            for (var i = 0; i < removeCount; i++)
            {
                if (_entries.Remove(keysToRemove[i], out var result))
                {
                    DisposeCachedRuns(result);
                }
            }

            if (rented != null)
            {
                ArrayPool<int>.Shared.Return(rented);
            }
        }

        /// <summary>
        /// Tries to retrieve cached shaped runs for the given text source index.
        /// </summary>
        internal bool TryGetShapedRuns(int firstTextSourceIndex, out CachedShapingResult result)
        {
            if (_hasSingleEntry && _singleKey == firstTextSourceIndex)
            {
                result = _singleValue;
                return true;
            }

            if (_entries != null && _entries.TryGetValue(firstTextSourceIndex, out result))
            {
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Adds shaped runs to the cache for the given text source index. The cache takes
        /// its own reference to each <see cref="ShapedTextRun"/>; the caller retains its
        /// original references unchanged.
        /// </summary>
        internal void Add(int firstTextSourceIndex, CachedShapingResult result)
        {
            AddRefShapedRuns(result.ShapedRuns);

            if (_entries != null)
            {
                if (_entries.TryGetValue(firstTextSourceIndex, out var existing))
                {
                    DisposeCachedRuns(existing);
                }

                _entries[firstTextSourceIndex] = result;
                return;
            }

            if (!_hasSingleEntry)
            {
                _singleKey = firstTextSourceIndex;
                _singleValue = result;
                _hasSingleEntry = true;
                return;
            }

            if (_singleKey == firstTextSourceIndex)
            {
                DisposeCachedRuns(_singleValue);
                _singleValue = result;
                return;
            }

            // Second distinct key: promote to dictionary.
            _entries = new Dictionary<int, CachedShapingResult>
            {
                { _singleKey, _singleValue },
                { firstTextSourceIndex, result }
            };
            _hasSingleEntry = false;
            _singleValue = default;
        }

        private static void AddRefShapedRuns(TextRun[] runs)
        {
            for (var i = 0; i < runs.Length; i++)
            {
                if (runs[i] is ShapedTextRun shaped)
                {
                    shaped.AddRef();
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Invalidate();
            _entries = null;
        }

        private static void DisposeCachedRuns(CachedShapingResult result)
        {
            var runs = result.ShapedRuns;

            for (var i = 0; i < runs.Length; i++)
            {
                if (runs[i] is ShapedTextRun shaped)
                {
                    shaped.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// Stores the result of text shaping for a paragraph segment starting at a given text source index.
    /// </summary>
    internal readonly struct CachedShapingResult
    {
        public CachedShapingResult(TextRun[] shapedRuns, FlowDirection resolvedFlowDirection,
            TextEndOfLine? textEndOfLine, int textSourceLength)
        {
            ShapedRuns = shapedRuns;
            ResolvedFlowDirection = resolvedFlowDirection;
            TextEndOfLine = textEndOfLine;
            TextSourceLength = textSourceLength;
        }

        /// <summary>
        /// The shaped text runs (output of ShapeTextRuns).
        /// </summary>
        public readonly TextRun[] ShapedRuns;

        /// <summary>
        /// The resolved flow direction for the paragraph.
        /// </summary>
        public readonly FlowDirection ResolvedFlowDirection;

        /// <summary>
        /// The end of line marker, if any.
        /// </summary>
        public readonly TextEndOfLine? TextEndOfLine;

        /// <summary>
        /// The total text source length consumed.
        /// </summary>
        public readonly int TextSourceLength;
    }
}
