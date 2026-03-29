using System;
using System.Collections.Generic;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// Caches shaped text runs and bidi processing results to avoid redundant shaping
    /// when only the paragraph width constraint changes (e.g., between Measure and Arrange).
    /// </summary>
    public class TextRunCache : IDisposable
    {
        private Dictionary<int, CachedShapingResult>? _entries;

        /// <summary>
        /// Invalidates all cached entries and disposes their shaped buffers.
        /// </summary>
        public void Invalidate()
        {
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
            if (_entries == null)
            {
                return;
            }

            var keysToRemove = new List<int>();

            foreach (var key in _entries.Keys)
            {
                if (key >= textSourceIndex)
                {
                    keysToRemove.Add(key);
                }
            }

            for (var i = 0; i < keysToRemove.Count; i++)
            {
                DisposeCachedRuns(_entries[keysToRemove[i]]);
                _entries.Remove(keysToRemove[i]);
            }
        }

        /// <summary>
        /// Tries to retrieve cached shaped runs for the given text source index.
        /// </summary>
        internal bool TryGetShapedRuns(int firstTextSourceIndex, out CachedShapingResult result)
        {
            if (_entries != null && _entries.TryGetValue(firstTextSourceIndex, out result))
            {
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Adds shaped runs to the cache for the given text source index.
        /// </summary>
        internal void Add(int firstTextSourceIndex, CachedShapingResult result)
        {
            _entries ??= new Dictionary<int, CachedShapingResult>();
            _entries[firstTextSourceIndex] = result;
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
