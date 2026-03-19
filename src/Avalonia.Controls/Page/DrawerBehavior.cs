namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the behavior of the drawer pane.
    /// </summary>
    public enum DrawerBehavior
    {
        /// <summary>
        /// The drawer adapts its display mode based on the current <see cref="DrawerLayoutBehavior"/>.
        /// </summary>
        Auto,
        /// <summary>
        /// The drawer always opens as a flyout overlay, regardless of layout behavior.
        /// </summary>
        Flyout,
        /// <summary>
        /// The drawer is permanently open and cannot be closed.
        /// </summary>
        /// <remarks>
        /// Setting this value forces <see cref="DrawerPage.IsOpen"/> to <see langword="true"/>.
        /// </remarks>
        Locked,
        /// <summary>
        /// The drawer is hidden and cannot be opened.
        /// </summary>
        /// <remarks>
        /// Setting this value forces <see cref="DrawerPage.IsOpen"/> to <see langword="false"/>.
        /// The <see cref="DrawerPage.Closing"/> event is not raised.
        /// </remarks>
        Disabled
    }
}
