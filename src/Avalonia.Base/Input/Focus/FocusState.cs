namespace Avalonia.Input
{
    /// <summary>
    /// Describes how an element obtained focus
    /// </summary>
    public enum FocusState
    {
        /// <summary>
        /// Element is not currently focused
        /// </summary>
        Unfocused,

        /// <summary>
        /// Element obtained focus through pointer interaction
        /// </summary>
        Pointer,

        /// <summary>
        /// Element obtained focus through keyboard interaction
        /// </summary>
        Keyboard,

        /// <summary>
        /// Element obtained focus by calling Focus() or other focus related API
        /// </summary>
        Programmatic
    }
}
