using System;
using System.Collections.Generic;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    /// <summary>
    ///     Actual implementation of a glyph run that stores platform dependent resources.
    /// </summary>
    [Unstable]
    public interface IGlyphRunImpl : IDisposable 
    {

        /// <summary>
        ///     Gets the conservative bounding box of the glyph run./>.
        /// </summary>
        Size Size { get; }

        /// <summary>
        ///     Gets the baseline origin of the glyph run./>.
        /// </summary>
        Point BaselineOrigin { get; }

        /// <summary>
        /// Gets the intersections of specified upper and lower limit.
        /// </summary>
        /// <param name="lowerLimit">Upper limit.</param>
        /// <param name="upperLimit">Lower limit.</param>
        /// <returns></returns>
        IReadOnlyList<float> GetIntersections(float lowerLimit, float upperLimit);
    }
}
