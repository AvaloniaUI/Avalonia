namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the layout behavior of the drawer page.
    /// </summary>
    public enum DrawerLayoutBehavior
    {
        /// <summary>
        /// The drawer overlays the detail content when open.
        /// </summary>
        Overlay,
        /// <summary>
        /// The drawer and detail content are shown side by side when open.
        /// </summary>
        Split,
        /// <summary>
        /// A narrow rail strip is always visible; opening expands as an overlay over the detail content.
        /// </summary>
        CompactOverlay,
        /// <summary>
        /// A narrow rail strip is always visible; opening expands and pushes the detail content aside.
        /// </summary>
        CompactInline,
    }
}
