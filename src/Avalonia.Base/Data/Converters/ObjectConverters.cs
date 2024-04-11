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

        /// <summary>
        /// A value converter that returns true if the input object is equal to a parameter object.
        /// </summary>
        public static readonly IValueConverter Equal =
            new FuncValueConverter<object?, object?, bool>((a, b) => a?.Equals(b) ?? b is null);
 
        /// <summary>
        /// A value converter that returns true if the input object is not equal to a parameter object.
        /// </summary>
        public static readonly IValueConverter NotEqual =
            new FuncValueConverter<object?, object?, bool>((a, b) => !a?.Equals(b) ?? b is not null);
    }
}
