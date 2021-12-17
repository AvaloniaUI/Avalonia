namespace Avalonia
{
    /// <summary>
    /// Untyped interface to <see cref="StyledPropertyMetadata{TValue}"/>
    /// </summary>
    public interface IStyledPropertyMetadata
    {
        /// <summary>
        /// Gets the default value for the property.
        /// </summary>
        object? DefaultValue { get; }
    }
}
