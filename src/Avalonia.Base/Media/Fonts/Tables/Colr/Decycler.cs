using System;
using System.Collections.Generic;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Errors that can occur during paint graph traversal with cycle detection.
    /// </summary>
    internal enum DecyclerError
    {
        /// <summary>
        /// A cycle was detected in the paint graph.
        /// </summary>
        CycleDetected,
        
        /// <summary>
        /// The maximum depth limit was exceeded.
        /// </summary>
        DepthLimitExceeded
    }

    /// <summary>
    /// Exception thrown when a decycler error occurs.
    /// </summary>
    internal class DecyclerException : Exception
    {
        public DecyclerError Error { get; }

        public DecyclerException(DecyclerError error, string message) : base(message)
        {
            Error = error;
        }
    }

    /// <summary>
    /// A guard that tracks entry into a paint node and ensures proper cleanup.
    /// </summary>
    /// <typeparam name="T">The type of the paint identifier.</typeparam>
    internal ref struct CycleGuard<T> where T : struct
    {
        private readonly Decycler<T> _decycler;
        private readonly T _id;
        private bool _exited;

        internal CycleGuard(Decycler<T> decycler, T id)
        {
            _decycler = decycler;
            _id = id;
            _exited = false;
        }

        /// <summary>
        /// Exits the guard, removing the paint ID from the visited set.
        /// </summary>
        public void Dispose()
        {
            if (!_exited)
            {
                _decycler.Exit(_id);
                _exited = true;
            }
        }
    }

    /// <summary>
    /// Tracks visited paint nodes to detect cycles in the paint graph.
    /// Uses a depth limit to prevent stack overflow even without a HashSet in no_std builds.
    /// 
    /// Usage example:
    /// <code>
    /// var decycler = new PaintDecycler();
    /// 
    /// void TraversePaint(Paint paint, ushort glyphId)
    /// {
    ///     using var guard = decycler.Enter(glyphId);
    ///     
    ///     // Process the paint node here
    ///     // The guard will automatically clean up when the using block exits
    ///     
    ///     // If this paint references other paints, traverse them recursively:
    ///     if (paint is ColrGlyph colrGlyph)
    ///     {
    ///         var childPaint = GetPaint(colrGlyph.GlyphId);
    ///         TraversePaint(childPaint, (ushort)colrGlyph.GlyphId);
    ///     }
    /// }
    /// </code>
    /// </summary>
    /// <typeparam name="T">The type of the paint identifier (typically ushort for GlyphId).</typeparam>
    internal class Decycler<T> where T : struct
    {
        private readonly HashSet<T> _visited;
        private readonly int _maxDepth;
        private int _currentDepth;

        /// <summary>
        /// Creates a new Decycler with the specified maximum depth.
        /// </summary>
        /// <param name="maxDepth">Maximum traversal depth before returning an error.</param>
        public Decycler(int maxDepth)
        {
            _visited = new HashSet<T>();
            _maxDepth = maxDepth;
            _currentDepth = 0;
        }

        /// <summary>
        /// Attempts to enter a paint node with the given ID.
        /// Returns a guard that will automatically exit when disposed.
        /// </summary>
        /// <param name="id">The paint identifier to enter.</param>
        /// <returns>A guard that will clean up on disposal.</returns>
        /// <exception cref="DecyclerException">Thrown if a cycle is detected or depth limit exceeded.</exception>
        public CycleGuard<T> Enter(T id)
        {
            if (_currentDepth >= _maxDepth)
            {
                throw new DecyclerException(
                    DecyclerError.DepthLimitExceeded,
                    $"Paint graph depth limit of {_maxDepth} exceeded");
            }

            if (_visited.Contains(id))
            {
                throw new DecyclerException(
                    DecyclerError.CycleDetected,
                    "Cycle detected in paint graph");
            }

            _visited.Add(id);
            _currentDepth++;

            return new CycleGuard<T>(this, id);
        }

        /// <summary>
        /// Exits a paint node, removing it from the visited set.
        /// Called automatically by CycleGuard.Dispose().
        /// </summary>
        /// <param name="id">The paint identifier to exit.</param>
        internal void Exit(T id)
        {
            _visited.Remove(id);
            _currentDepth--;
        }

        /// <summary>
        /// Returns the current traversal depth.
        /// </summary>
        public int CurrentDepth => _currentDepth;

        /// <summary>
        /// Returns the maximum allowed traversal depth.
        /// </summary>
        public int MaxDepth => _maxDepth;

        /// <summary>
        /// Resets the decycler to its initial state, clearing all visited nodes.
        /// </summary>
        public void Reset()
        {
            _visited.Clear();
            _currentDepth = 0;
        }
    }
}
