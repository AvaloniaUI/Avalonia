using System;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    /// <summary>
    /// Paints an area with a radial gradient.
    /// </summary>
    [NotClientImplementable]
    public interface IRadialGradientBrush : IGradientBrush
    {
        /// <summary>
        /// Gets the start point for the gradient.
        /// </summary>
        RelativePoint Center { get; }

        /// <summary>
        /// Gets the location of the two-dimensional focal point that defines the beginning of the
        /// gradient.
        /// </summary>
        RelativePoint GradientOrigin { get; }

        [Obsolete("Use RadiusX/RadiusY")] public double Radius { get; }

        /// <summary>
        /// Gets the horizontal radius of the outermost circle of the radial gradient.
        /// </summary>
        RelativeScalar RadiusX { get; }
        
        /// <summary>
        /// Gets the vertical radius of the outermost circle of the radial gradient.
        /// </summary>
        RelativeScalar RadiusY { get; }
    }
}
