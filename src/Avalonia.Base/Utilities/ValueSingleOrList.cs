using System.Collections.Generic;

namespace Avalonia.Utilities
{
    /// <summary>
    /// A list like struct optimized for holding zero or one items.
    /// </summary>
    /// <typeparam name="T">The type of items held in the list.</typeparam>
    /// <remarks>
    /// Once more than value has been added to this storage it will switch to using <see cref="List"/> internally.
    /// </remarks>
    internal ref struct ValueSingleOrList<T>
    {
        private bool _isSingleSet;

        /// <summary>
        /// Single contained value. Only valid if <see cref="IsSingle"/> is set.
        /// </summary>
        public T Single { get; private set; }

        /// <summary>
        /// List of values.
        /// </summary>
        public List<T> List { get; private set; }

        /// <summary>
        /// If this struct is backed by a list.
        /// </summary>
        public bool HasList => List != null;

        /// <summary>
        /// If this struct contains only single value and storage was not promoted to a list.
        /// </summary>
        public bool IsSingle => List == null && _isSingleSet;

        /// <summary>
        /// Adds a value.
        /// </summary>
        /// <param name="value">Value to add.</param>
        public void Add(T value)
        {
            if (List != null)
            {
                List.Add(value);
            }
            else
            {
                if (!_isSingleSet)
                {
                    Single = value;

                    _isSingleSet = true;
                }
                else
                {
                    List = new List<T>();

                    List.Add(Single);
                    List.Add(value);

                    Single = default!;
                }
            }
        }

        /// <summary>
        /// Removes a value.
        /// </summary>
        /// <param name="value">Value to remove.</param>
        public bool Remove(T value)
        {
            if (List != null)
            {
                return List.Remove(value);
            }

            if (!_isSingleSet)
            {
                return false;
            }

            if (EqualityComparer<T>.Default.Equals(Single, value))
            {
                Single = default!;

                _isSingleSet = false;

                return true;
            }

            return false;
        }
    }
}
