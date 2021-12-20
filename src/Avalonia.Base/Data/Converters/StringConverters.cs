namespace Avalonia.Data.Converters
{
    /// <summary>
    /// Provides a set of useful <see cref="IValueConverter"/>s for working with string values.
    /// </summary>
    public static class StringConverters
    {
        /// <summary>
        /// A value converter that returns true if the input string is null or an empty string.
        /// </summary>
        public static readonly IValueConverter IsNullOrEmpty =
            new FuncValueConverter<string?, bool>(string.IsNullOrEmpty);

        /// <summary>
        /// A value converter that returns true if the input string is not null or empty.
        /// </summary>
        public static readonly IValueConverter IsNotNullOrEmpty =
            new FuncValueConverter<string?, bool>(x => !string.IsNullOrEmpty(x));
    }
}
