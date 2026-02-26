namespace Avalonia.Controls
{
    /// <summary>
    /// Specifies the type of navigation that occurred.
    /// </summary>
    public enum NavigationType
    {
        /// <summary>
        /// A new page was pushed onto the navigation stack.
        /// </summary>
        Push,
        /// <summary>
        /// The top page was popped from the navigation stack.
        /// </summary>
        Pop,
        /// <summary>
        /// All pages above the root were popped.
        /// </summary>
        PopToRoot,
        /// <summary>
        /// A page was inserted into the navigation stack.
        /// </summary>
        Insert,
        /// <summary>
        /// A page was removed from the navigation stack.
        /// </summary>
        Remove,
        /// <summary>
        /// The current top page was replaced with a new one.
        /// </summary>
        Replace
    }
}
