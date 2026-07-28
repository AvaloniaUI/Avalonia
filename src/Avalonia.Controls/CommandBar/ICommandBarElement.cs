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

        /// <summary>
        /// Gets or sets whether the element is currently displayed inside the overflow popup.
        /// Set automatically by <see cref="CommandBar"/> when moving items between primary and overflow.
        /// </summary>
        bool IsInOverflow { get; set; }
    }
}
