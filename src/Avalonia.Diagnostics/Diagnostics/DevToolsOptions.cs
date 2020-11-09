using Avalonia.Input;

namespace Avalonia.Diagnostics
{
    /// <summary>
    /// DevTool instance settings
    /// </summary>
    public class DevToolsOptions
    {
        /// <summary>
        /// The key gesture to open DevTools.
        /// </summary>
        public KeyGesture Gesture { get; set; } = new KeyGesture(Key.F12);

        /// <summary>
        /// Indicates whether DevTools should stay on top of the inspected control if possible.
        /// </summary>
        public bool OnTop { get; set; } = true;

        /// <summary>
        /// The size of DevTools window. Default value is 1024x512.
        /// </summary>
        public Size Size { get; set; } = new Size(1024, 512);

        /// <summary>
        /// Indicates whether to display margins and padding, default value is true.
        /// </summary>
        public bool ShouldVisualizeMarginPadding { get; set; } = true;

        /// <summary>
        /// Indicates whether to display dirty rect, default value is false.
        /// </summary>
        public bool ShouldVisualizeDirtyRects { get; set; } = false;

        /// <summary>
        /// Indicates whether to display fps overly, default value is false.
        /// </summary>
        public bool ShowFpsOverlay { get; set; } = false;

        /// <summary>
        /// Indicates whether to display console, default value is false.
        /// </summary>
        public bool ShowConsole { get; set; } = false;

        /// <summary>
        /// Indicates whether to display Layout Visualizer, default value is true.
        /// </summary>
        public bool ShowLayoutVisualizer { get; set; } = true;
    }
}
