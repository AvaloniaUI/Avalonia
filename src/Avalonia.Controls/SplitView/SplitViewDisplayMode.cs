namespace Avalonia.Controls
{
    /// <summary>
    /// Defines constants for how the SplitView Pane should display
    /// </summary>
    public enum SplitViewDisplayMode
    {
        /// <summary>
        /// Pane is displayed next to content, and does not auto collapse
        /// when tapped outside
        /// </summary>
        Inline,
        /// <summary>
        /// Pane is displayed next to content. When collapsed, pane is still
        /// visible according to CompactPaneLength. Pane does not auto collapse
        /// when tapped outside
        /// </summary>
        CompactInline,
        /// <summary>
        /// Pane is displayed above content. Pane collapses when tapped outside
        /// </summary>
        Overlay,
        /// <summary>
        /// Pane is displayed above content. When collapsed, pane is still
        /// visible according to CompactPaneLength. Pane collapses when tapped outside
        /// </summary>
        CompactOverlay
    }
}
