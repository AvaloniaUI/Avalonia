namespace Avalonia.Controls
{
    /// <summary>
    /// Specifies the position of the tab strip within a <see cref="TabbedPage"/> layout.
    /// </summary>
    public enum TabPlacement
    {
        /// <summary>
        /// Automatically determines the tab placement based on the target platform.
        /// Resolves to <see cref="Bottom"/> on iOS and Android, and <see cref="Top"/> on all other platforms.
        /// </summary>
        Auto,

        /// <summary>
        /// Displays tabs at the top of the content area.
        /// </summary>
        Top,

        /// <summary>
        /// Displays tabs at the bottom of the content area.
        /// </summary>
        Bottom,

        /// <summary>
        /// Displays tabs along the left side of the content area.
        /// </summary>
        Left,

        /// <summary>
        /// Displays tabs along the right side of the content area.
        /// </summary>
        Right
    }
}
