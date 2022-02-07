namespace Avalonia.Data.Converters
{
    /// <summary>
    /// Provides a set of useful <see cref="IValueConverter"/>s for working with objects.
    /// </summary>
    public static class ObjectConverters
    {
        /// <summary>
        /// A value converter that returns true if the input object is a null reference.
        /// </summary>
        public static readonly IValueConverter IsNull =
            new FuncValueConverter<object?, bool>(x => x is null);

        /// <summary>
        /// A value converter that returns true if the input object is not null.
        /// </summary>
        public static readonly IValueConverter IsNotNull =
            new FuncValueConverter<object?, bool>(x => x is not null);
    }
}
