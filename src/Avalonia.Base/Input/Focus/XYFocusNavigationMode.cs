namespace Avalonia.Input
{
    /// <summary>
    /// Specifies the 2D directional navigation behavior when using the keyboard arrow keys
    /// </summary>
    /// <remarks>In WinUI, this is XYFocusKeyboardNavigationMode, shortened name here</remarks>
    public enum XYFocusNavigationMode
    {
        /// <summary>
        /// Behavior is inherited from ancestors. If all ancestors have value set to Auto,
        /// the default is Disabled
        /// </summary>
        Auto,

        /// <summary>
        /// Arrow keys can be used for navigation
        /// </summary>
        Enabled,

        /// <summary>
        /// Arrow keys cannot be used for navigation
        /// </summary>
        Disabled
    }
}
