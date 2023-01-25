namespace Avalonia.Media.TextFormatting
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public readonly struct SplitResult<T>
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public SplitResult(T first, T? second)
        {
            First = first;

            Second = second;
        }

        /// <summary>
        /// Gets the first part.
        /// </summary>
        /// <value>
        /// The first part.
        /// </value>
        public T First { get; }

        /// <summary>
        /// Gets the second part.
        /// </summary>
        /// <value>
        /// The second part.
        /// </value>
        public T? Second { get; }

        /// <summary>
        /// Deconstructs the split results into its components.
        /// </summary>
        /// <param name="first">On return, contains the first part.</param>
        /// <param name="second">On return, contains the second part.</param>
        public void Deconstruct(out T first, out T? second)
        {
            first = First;
            second = Second;
        }
    }
}
