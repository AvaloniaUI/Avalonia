using System;

namespace Avalonia.Media
{
    /// <summary>
    /// A glyph's control-point bounding box in font design units, as stored in the
    /// <c>glyf</c> header. Used by the batch bounds path
    /// (<see cref="GlyphTypeface.TryGetGlyphBounds"/>) where only the ink extent is
    /// needed and advances are already known by the caller.
    /// </summary>
    internal readonly record struct GlyphBounds(short XMin, short YMin, short XMax, short YMax)
    {
        /// <summary>
        /// Width of the bounding box (<see cref="XMax"/> − <see cref="XMin"/>), clamped to a
        /// non-negative value. A malformed header with <see cref="XMax"/> &lt; <see cref="XMin"/>
        /// yields <c>0</c> rather than wrapping when narrowed to an unsigned extent. The maximum
        /// possible extent for <see cref="short"/> coordinates is 65535, so the result always
        /// fits in a <see cref="ushort"/>.
        /// </summary>
        public int Width => Math.Max(0, XMax - XMin);

        /// <summary>
        /// Height of the bounding box (<see cref="YMax"/> − <see cref="YMin"/>), clamped to a
        /// non-negative value. A malformed header with <see cref="YMax"/> &lt; <see cref="YMin"/>
        /// yields <c>0</c> rather than wrapping when narrowed to an unsigned extent.
        /// </summary>
        public int Height => Math.Max(0, YMax - YMin);
    }
}
