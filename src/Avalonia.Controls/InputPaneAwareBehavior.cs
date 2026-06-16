namespace Avalonia.Controls
{
    /// <summary>
    /// Specifies the behavior <see cref="InputPaneAwareView"/> takes when input pane state changes.
    /// </summary>
    public enum InputPaneAwareBehavior
    {
        /// <summary>
        /// The view doesn't resize or move the content when input pane state changes.
        /// </summary>
        None = 0,

        /// <summary>
        /// The view moves the content vertically when input pane states changes. The content is not resized.
        /// </summary>
        Pan,

        /// <summary>
        /// The view resizes the content when input pane state changes.
        /// </summary>
        Resize
    }
}
