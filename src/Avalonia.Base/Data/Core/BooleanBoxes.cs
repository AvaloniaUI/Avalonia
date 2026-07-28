namespace Avalonia.Data.Core
{
    /// <summary>
    /// Cached boxed <see cref="bool"/> values, referenced by compiled XAML binding accessors
    /// to avoid allocating a new box on every boolean property read.
    /// </summary>
    public static class BooleanBoxes
    {
        /// <summary>
        /// A cached boxed <c>true</c> value.
        /// </summary>
        public static readonly object True = true;

        /// <summary>
        /// A cached boxed <c>false</c> value.
        /// </summary>
        public static readonly object False = false;
    }
}
