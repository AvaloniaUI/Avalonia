using System.Collections.Generic;

namespace Avalonia.Media
{
    public enum AcrylicBackgroundSource
    {
        None,
        HostBackDrop,
        BackDrop
    }

    public interface IAcrylicBrush : IBrush
    {
        AcrylicBackgroundSource BackgroundSource { get; set; }

        Color TintColor { get; set; }

        double TintOpacity { get; set; }

        double TintLuminosityOpacity { get; set; }

        Color FallbackColor { get; set; }
    }

    /// <summary>
    /// A brush that draws with a gradient.
    /// </summary>
    public interface IGradientBrush : IBrush
    {
        /// <summary>
        /// Gets the brush's gradient stops.
        /// </summary>
        IReadOnlyList<IGradientStop> GradientStops { get; }

        /// <summary>
        /// Gets the brush's spread method that defines how to draw a gradient that doesn't fill
        /// the bounds of the destination control.
        /// </summary>
        GradientSpreadMethod SpreadMethod { get; }
    }
}
