namespace Avalonia.Media.TextFormatting
{
    public readonly struct SplitResult<T>
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
    }
}
