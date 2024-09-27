using Avalonia.Input;

namespace Avalonia.Diagnostics
{
    internal class HotKeyConfiguration
    {
        /// <summary>
        /// Freezes refreshing the Value Frames inspector for the selected Control
        /// </summary>
        public KeyGesture ValueFramesFreeze { get; init; } = new(Key.S, KeyModifiers.Alt);

        /// <summary>
        /// Resumes refreshing the Value Frames inspector for the selected Control
        /// </summary>
        public KeyGesture ValueFramesUnfreeze { get; init; } = new(Key.D, KeyModifiers.Alt);

        /// <summary>
        /// Inspects the hovered Control in the Logical or Visual Tree Page
        /// </summary>
        public KeyGesture InspectHoveredControl { get; init; } = new(Key.None, KeyModifiers.Shift | KeyModifiers.Control);

        /// <summary>
        /// Toggles the freezing of Popups which prevents visible Popups from closing so they can be inspected
        /// </summary>
        public KeyGesture TogglePopupFreeze { get; init; } = new(Key.F, KeyModifiers.Alt | KeyModifiers.Control);

        /// <summary>
        /// Saves a Screenshot of the Selected Control in the Logical or Visual Tree Page
        /// </summary>
        public KeyGesture ScreenshotSelectedControl { get; init; } = new(Key.F8);
    }
}
