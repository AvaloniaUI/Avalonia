using Avalonia.Styling;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a themed element.
    /// </summary>
    public interface IThemed
    {
        /// <summary>
        /// Gets the theme style for the element.
        /// </summary>
        public IStyle? Theme { get; }
    }
}
