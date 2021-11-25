namespace Avalonia.Controls
{
    /// <summary>
    /// Describes the item virtualization method to use for a list.
    /// </summary>
    public enum ItemVirtualizationMode
    {
        /// <summary>
        /// Do not virtualize items.
        /// </summary>
        None,

        /// <summary>
        /// Virtualize items without smooth scrolling.
        /// </summary>
        Simple,

        /// <summary>
        /// Virtualize items with smooth scrolling.
        /// </summary>
        Smooth,
    }
}
