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
    }
}
