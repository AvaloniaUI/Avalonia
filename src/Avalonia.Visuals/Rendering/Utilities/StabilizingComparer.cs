using System.Collections.Generic;

namespace Avalonia.Rendering.Utilities
{
    /// <summary>
    /// Comparer used for stable sorting of an Array by matching it up with their current positions using a KeyValuePair.
    /// The stabilized comparison will be the array of values.
    /// </summary>
    internal sealed class StabilizingComparer<T> : IComparer<(int, T)>
    {
        private readonly IComparer<T> _comparer;

        /// <summary>
        /// Initializes a stabilizing comparer with any other comparison criteria required.
        /// </summary>
        /// <param name="comparer"></param>
        public StabilizingComparer(IComparer<T> comparer)
        {
            _comparer = comparer;
        }

        /// <summary>
        /// Compares according to the provided comparer. In case of a tie preserves the old order.
        /// </summary>
        public int Compare((int, T) x, (int, T) y)
        {
            var result = _comparer.Compare(x.Item2, y.Item2);
            return result != 0 ? result : x.Item1.CompareTo(y.Item1);
        }
    }
}
