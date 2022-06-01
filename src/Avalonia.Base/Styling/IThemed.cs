namespace Avalonia.Styling
{
    /// <summary>
    /// Represents a themed element.
    /// </summary>
    public interface IThemed
    {
        /// <summary>
        /// Gets the theme style for the element.
        /// </summary>
        public ControlTheme? Theme { get; }
    }
}
