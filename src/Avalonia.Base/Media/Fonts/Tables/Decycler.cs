using System;
using System.Collections.Generic;

namespace Avalonia.Media.Fonts.Tables
{
    /// <summary>
    /// Errors that can occur during graph traversal with cycle detection.
    /// </summary>
    internal enum DecyclerError
    {
        /// <summary>
        /// A cycle was detected in the graph.
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
    /// A guard that tracks entry into a node and ensures proper cleanup.
    /// </summary>
    /// <typeparam name="T">The type of the node identifier.</typeparam>
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
        /// Exits the guard, removing the node ID from the visited set.
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
    /// Tracks visited nodes to detect cycles in a graph (composite glyphs, paint graphs, etc.).
    /// Uses a depth limit to prevent stack overflow during recursive traversal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instances are <b>not thread-safe</b>: <see cref="Enter"/>, <see cref="Exit"/>, and
    /// <see cref="Reset"/> all mutate shared state without synchronization. The intended
    /// usage pattern is "one instance per traversal" — rent an instance from a pool at
    /// the start of a single-threaded walk and return it when done. Sharing one
    /// <see cref="Decycler{T}"/> across concurrent traversals will corrupt the visited
    /// set.
    /// </para>
    /// <para>
    /// <see cref="Enter"/> uses exceptions to signal cycle / depth-limit failures.
    /// Callers that traverse user-supplied data (e.g. font tables) should wrap the
    /// outermost traversal in a <c>try</c> / <c>catch (DecyclerException)</c> and treat
    /// the failure as "stop traversing this subgraph".
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the node identifier.</typeparam>
    internal class Decycler<T> where T : struct
    {
        private readonly HashSet<T> _visited;
        private readonly int _maxDepth;
        private int _currentDepth;

        /// <summary>
        /// Creates a new Decycler with the specified maximum depth.
        /// </summary>
        /// <param name="maxDepth">Maximum traversal depth before <see cref="Enter"/> throws a
        /// <see cref="DecyclerException"/>. Must be at least 1.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDepth"/> is less than 1.</exception>
        public Decycler(int maxDepth)
        {
            if (maxDepth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDepth), maxDepth, "maxDepth must be at least 1.");
            }

            _visited = new HashSet<T>();
            _maxDepth = maxDepth;
            _currentDepth = 0;
        }

        /// <summary>
        /// Attempts to enter a node with the given ID.
        /// Returns a guard that will automatically exit when disposed.
        /// </summary>
        /// <param name="id">The node identifier to enter.</param>
        /// <returns>A guard that will clean up on disposal.</returns>
        /// <exception cref="DecyclerException">Thrown if a cycle is detected or depth limit exceeded.</exception>
        public CycleGuard<T> Enter(T id)
        {
            if (_currentDepth >= _maxDepth)
            {
                throw new DecyclerException(
                    DecyclerError.DepthLimitExceeded,
                    $"Graph depth limit of {_maxDepth} exceeded");
            }

            if (_visited.Contains(id))
            {
                throw new DecyclerException(
                    DecyclerError.CycleDetected,
                    "Cycle detected in graph");
            }

            _visited.Add(id);
            _currentDepth++;

            return new CycleGuard<T>(this, id);
        }

        /// <summary>
        /// Exits a node, removing it from the visited set.
        /// Called automatically by CycleGuard.Dispose().
        /// </summary>
        /// <param name="id">The node identifier to exit.</param>
        internal void Exit(T id)
        {
            // CycleGuard is a copyable ref struct, so a copied guard can double-exit; only
            // give depth budget back for an id that was actually in the visited set.
            if (_visited.Remove(id))
            {
                _currentDepth--;
            }
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
