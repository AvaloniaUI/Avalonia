using Avalonia.Metadata;

namespace Avalonia
{
    /// <summary>
    /// Defines an element with a data context that can be used for binding.
    /// </summary>
    [NotClientImplementable]
    public interface IDataContextProvider
    {
        /// <summary>
        /// Gets or sets the element's data context.
        /// </summary>
        object? DataContext { get; set; }
    }
}
