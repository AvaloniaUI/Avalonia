namespace Avalonia.Controls
{
    /// <summary>
    /// Determines the startup location of the window.
    /// </summary>
    public enum WindowStartupLocation
    {
        /// <summary>
        /// The startup location is defined by the Position property.
        /// </summary>
        Manual,

        /// <summary>
        /// The startup location is the center of the screen.
        /// </summary>
        CenterScreen,

        /// <summary>
        /// The startup location is the center of the owner window. If the owner window is not specified, the startup location will be <see cref="Manual"/>.
        /// </summary>
        CenterOwner,

        /// <summary>
        /// The startup location is the upper left corner of the screen.
        /// </summary>
        UpperLeftScreen,

        /// <summary>
        /// The startup location is the upper right corner of the screen.
        /// </summary>
        UpperRightScreen,

        /// <summary>
        /// The startup location is the lower right corner of the screen.
        /// </summary>
        LowerRightScreen,

        /// <summary>
        /// The startup location is the lower left corner of the screen.
        /// </summary>
        LowerLeftScreen,
    }
}
