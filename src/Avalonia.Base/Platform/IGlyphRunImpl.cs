using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    /// <summary>
    ///     An immutable platform representation of a <see cref="GlyphRun"/>.
    /// </summary>
    [Unstable]
    public interface IGlyphRunImpl : IDisposable
    {
        /// <summary>
        ///     Gets the <see cref="IGlyphTypeface"/> for the <see cref="IGlyphRunImpl"/>.
        /// </summary>
        IGlyphTypeface GlyphTypeface { get; }

        /// <summary>
        ///     Gets the em size used for rendering the <see cref="IGlyphRunImpl"/>.
        /// </summary>
        double FontRenderingEmSize { get; }

        /// <summary>
        ///     Gets the baseline origin of the glyph run./>.
        /// </summary>
        Point BaselineOrigin { get; }

        /// <summary>
        ///     Gets the conservative bounding box of the glyph run./>.
        /// </summary>
        Rect Bounds { get; }

        /// <summary>
        /// Gets the intersections of specified upper and lower limit.
        /// </summary>
        /// <param name="lowerLimit">Upper limit.</param>
        /// <param name="upperLimit">Lower limit.</param>
        /// <returns></returns>
        IReadOnlyList<float> GetIntersections(float lowerLimit, float upperLimit);
    }
}
