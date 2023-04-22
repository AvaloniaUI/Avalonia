using System;
using Avalonia.Media;
using Avalonia.Metadata;

#nullable enable
namespace Avalonia.Controls.Platform
{
    [Unstable]
    [NotClientImplementable]
    public interface IInsetsManager
    {
        /// <summary>
        /// Gets or sets whether the system bars are visible.
        /// </summary>
        bool? IsSystemBarVisible { get; set; }

        /// <summary>
        /// Gets or sets whether the window draws edge to edge. behind any visibile system bars.
        /// </summary>
        bool DisplayEdgeToEdge { get; set; }

        /// <summary>
        /// Gets the current safe area padding.
        /// </summary>
        Thickness SafeAreaPadding { get; }

        /// <summary>
        /// Gets or sets the color of the platform's system bars
        /// </summary>
        Color? SystemBarColor { get; set; }

        /// <summary>
        /// Occurs when safe area for the current window changes.
        /// </summary>
        event EventHandler<SafeAreaChangedArgs>? SafeAreaChanged;
    }
    
    public class SafeAreaChangedArgs : EventArgs
    {
        public SafeAreaChangedArgs(Thickness safeArePadding)
        {
            SafeAreaPadding = safeArePadding;
        }

        /// <inheritdoc cref="IInsetsManager.SafeAreaPadding"/>
        public Thickness SafeAreaPadding { get; }
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
