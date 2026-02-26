namespace Avalonia.Controls
{
    /// <summary>
    /// Interface implemented by all command bar elements.
    /// </summary>
    public interface ICommandBarElement
    {
        /// <summary>
        /// Gets or sets whether the element is in compact mode (icon only, no label).
        /// </summary>
        bool IsCompact { get; set; }
    }
}
