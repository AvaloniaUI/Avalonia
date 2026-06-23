namespace Avalonia.Controls
{
    /// <summary>
    /// Controls how the navigation bar interacts with the page content area.
    /// </summary>
    public enum BarLayoutBehavior
    {
        /// <summary>
        /// Default. The navigation bar takes up layout space; page content is laid out below it.
        /// </summary>
        Inset,

        /// <summary>
        /// The navigation bar floats above the page content. Content extends behind the bar.
        /// </summary>
        Overlay,
    }
}
