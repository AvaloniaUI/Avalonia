namespace Avalonia.Controls
{
    /// <summary>
    /// Specifies the state of the progress indicator displayed in the taskbar.
    /// </summary>
    public enum TaskbarProgressState
    {
        /// <summary>
        /// No progress is displayed.
        /// </summary>
        None = 0,

        /// <summary>
        /// Pulsing green indicator is displayed.
        /// </summary>
        Indeterminate = 1,

        /// <summary>
        /// Green progress indicator is displayed.
        /// </summary>
        Normal = 2,

        /// <summary>
        /// Red progress indicator is displayed.
        /// </summary>
        Error = 4,

        /// <summary>
        /// Yellow progress indicator is displayed.
        /// </summary>
        Paused = 8,
    }
}
