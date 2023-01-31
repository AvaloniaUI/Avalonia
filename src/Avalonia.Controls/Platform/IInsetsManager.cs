using System;
using Avalonia.Interactivity;

namespace Avalonia.Controls.Platform
{
    [Avalonia.Metadata.Unstable]
    public interface IInsetsManager
    {
        /// <summary>
        /// Gets or sets the theme for the system bars, if supported.
        /// </summary>
        SystemBarTheme? SystemBarTheme { get; set; }

        /// <summary>
        /// Gets or sets whether the system bars are visible.
        /// </summary>
        bool? IsSystemBarVisible { get; set; }

        /// <summary>
        /// Occurs when safe area for the current window changes.
        /// </summary>

        event EventHandler<SafeAreaChangedArgs> SafeAreaChanged;


        /// <summary>
        /// Gets or sets whether the window draws edge to edge. behind any visibile system bars.
        /// </summary>
        bool DisplayEdgeToEdge { get; set; }

        /// <summary>
        /// Gets the current safe area padding.
        /// </summary>
        /// <returns></returns>
        Thickness GetSafeAreaPadding();

        public class SafeAreaChangedArgs : RoutedEventArgs
        {
            public SafeAreaChangedArgs(Thickness safeArePadding)
            {
                SafeAreaPadding = safeArePadding;
            }

            public Thickness SafeAreaPadding { get; }
        }
    }

    public enum SystemBarTheme
    {
        /// <summary>
        /// Light system bar theme, with light background and a dark foreground
        /// </summary>
        Light,

        /// <summary>
        /// Bark system bar theme, with dark background and a light foreground
        /// </summary>
        Dark
    }
}
