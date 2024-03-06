namespace Avalonia
{
    /// <summary>
    /// Objects implementing this interface and providing a value for <see cref="Name"/> will be registered in the 
    /// relevant namescope when constructed in XAML.
    /// </summary>
    public interface INamed
    {
        /// <summary>
        /// Gets the element name.
        /// </summary>
        string? Name { get; }
    }
}
