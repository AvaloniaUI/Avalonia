namespace Avalonia.Media
{
    /// <summary>
    /// Defines how a background is drawn relative to its border.
    /// </summary>
    public enum BackgroundSizing
    {
        /// <summary>
        /// The background is drawn up to the inside edge of the border.
        /// </summary>
        /// <remarks>
        /// The background will never be drawn under the border itself and will not be visible
        /// underneath the border regardless of border transparency.
        /// </remarks>
        InnerBorderEdge = 0,

        /// <summary>
        /// The background is drawn completely to the outside edge of the border.
        /// </summary>
        /// <remarks>
        /// The background will be visible underneath the border if the border has transparency.
        /// </remarks>
        OuterBorderEdge = 1,

        /// <summary>
        /// The background is drawn to the midpoint (center) of the border.
        /// </summary>
        /// <remarks>
        /// The background will be visible underneath half of the border if the border has transparency.
        /// For this reason it is not recommended to use <see cref="CenterBorder"/> if transparency is involved.
        /// <br/><br/>
        /// This value does not exist in other XAML frameworks and only exists in Avalonia for backwards compatibility
        /// with legacy code. Before <see cref="BackgroundSizing"/> was added, Avalonia would always render using this
        /// value (Skia's default).
        /// </remarks>
        CenterBorder = 2,
    }
}
