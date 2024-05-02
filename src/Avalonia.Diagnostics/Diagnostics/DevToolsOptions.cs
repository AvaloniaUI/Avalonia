using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;

namespace Avalonia.Diagnostics
{
    /// <summary>
    /// Describes options used to customize DevTools.
    /// </summary>
    public class DevToolsOptions
    {
        /// <summary>
        /// Gets or sets the key gesture used to open DevTools.
        /// </summary>
        public KeyGesture Gesture { get; set; } = new KeyGesture(Key.F12);

        /// <summary>
        /// Gets or sets a value indicating whether DevTools should be displayed as a child window
        /// of the window being inspected. The default value is true.
        /// </summary>
        /// <remarks>This setting is ignored if DevTools is attached to <see cref="Application"/></remarks>
        public bool ShowAsChildWindow { get; set; } = true;

        /// <summary>
        /// Gets or sets the initial size of the DevTools window. The default value is 1280x720.
        /// </summary>
        public Size Size { get; set; } = new Size(1280, 720);

        /// <summary>
        /// Get or set the startup screen index where the DevTools window will be displayed.
        /// </summary>
        public int? StartupScreenIndex { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether DevTools should be displayed implemented interfaces on Control details. The default value is true.
        /// </summary>
        public bool ShowImplementedInterfaces { get; set; } = true;
        
        /// <summary>
        /// Allow to customize SreenshotHandler
        /// </summary>
        /// <remarks>Default handler is <see cref="Screenshots.FilePickerHandler"/></remarks>
        public IScreenshotHandler ScreenshotHandler { get; set; }
          = Conventions.DefaultScreenshotHandler;

        /// <summary>
        /// Gets or sets whether DevTools theme.
        /// </summary>
        public ThemeVariant? ThemeVariant { get; set; }

        /// <summary>
        /// Get or set Focus Highlighter <see cref="Brush"/>
        /// </summary>
        public IBrush? FocusHighlighterBrush { get; set; }

        /// <summary>
        /// Set the <see cref="DevToolsViewKind">kind</see> of diagnostic view that show at launch of DevTools
        /// </summary>
        public DevToolsViewKind LaunchView { get; init; }
    }
}
