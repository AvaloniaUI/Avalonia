namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// A thin wrapper over an <see cref="System.Array"/> that allows some additional list-like functionality.
    /// </summary>
    /// <remarks>
    /// This is only for internal ColorPicker-related functionality and should not be used elsewhere.
    /// It is added for performance to enjoy the simplicity of the IList.Add() method without requiring
    /// an additional copy to turn a list into an array for bitmaps.
    /// </remarks>
    /// <typeparam name="T">The type of items in the array.</typeparam>
    internal class ArrayList<T>
    {
        private int _nextIndex = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayList{T}"/> class.
        /// </summary>
        public ArrayList(int capacity)
        {
            Capacity = capacity;
            Array = new T[capacity];
        }

        /// <summary>
        /// Provides access to the underlying array by index.
        /// This exists for simplification and the <see cref="Array"/> property
        /// may also be used.
        /// </summary>
        /// <param name="i">The index of the item to get or set.</param>
        /// <returns>The item at the given index.</returns>
        public T this[int i]
        {
            get => Array[i];
            set => Array[i] = value;
        }

        /// <summary>
        /// Gets the underlying array.
        /// </summary>
        public T[] Array { get; private set; }

        /// <summary>
        /// Gets the fixed capacity/size of the array.
        /// This must be set during construction.
        /// </summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// Adds the given item to the array at the next available index.
        /// WARNING: This must be used carefully and only once, in sequence.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            if (_nextIndex >= 0 &&
                _nextIndex < Capacity)
            {
                Array[_nextIndex] = item;
                _nextIndex++;
            }
            else
            {
                // If necessary an exception could be thrown here
                // throw new IndexOutOfRangeException();
            }

            return;
        }
    }
}
