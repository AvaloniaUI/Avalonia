using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Avalonia.Media.Fonts.Tables.Glyf
{
    /// <summary>
    /// Represents the descriptor for a glyph in a font, providing access to its outline and bounding metrics.
    /// </summary>
    /// <remarks>A glyph descriptor exposes contour and bounding box information for a glyph, as well as
    /// access to its simple or composite outline data. Use the properties to determine the glyph type and retrieve the
    /// corresponding outline representation.</remarks>
    internal class GlyphDescriptor
    {
        private readonly ReadOnlyMemory<byte> _glyphData;

        public GlyphDescriptor(ReadOnlyMemory<byte> data)
        {
            var span = data.Span;
            
            NumberOfContours = BinaryPrimitives.ReadInt16BigEndian(span.Slice(0, 2));
            
            var xMin = BinaryPrimitives.ReadInt16BigEndian(span.Slice(2, 2));
            var yMin = BinaryPrimitives.ReadInt16BigEndian(span.Slice(4, 2));
            var xMax = BinaryPrimitives.ReadInt16BigEndian(span.Slice(6, 2));
            var yMax = BinaryPrimitives.ReadInt16BigEndian(span.Slice(8, 2));

            // Store as Rect - note: coordinates are in font design units
            ConservativeBounds = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);

            _glyphData = data.Slice(10);
        }

        /// <summary>
        /// Gets the number of contours in the glyph.
        /// </summary>
        /// <remarks>
        /// If the value is greater than or equal to zero, the glyph is a simple glyph.
        /// If the value is negative (typically -1), the glyph is a composite glyph.
        /// </remarks>
        public short NumberOfContours { get; }

        /// <summary>
        /// Gets the conservative bounding box for the glyph in font design units.
        /// </summary>
        /// <remarks>
        /// This represents the minimum bounding rectangle that contains all points in the glyph outline.
        /// The coordinates are in the font's coordinate system (design units), not scaled to any particular size.
        /// For proper rendering, these coordinates should be transformed by the font matrix and scaled
        /// by the font rendering size.
        /// </remarks>
        public Rect ConservativeBounds { get; }

        /// <summary>
        /// Gets a value indicating whether this glyph is a simple glyph (as opposed to a composite glyph).
        /// </summary>
        public bool IsSimpleGlyph => NumberOfContours >= 0;

        /// <summary>
        /// Gets the simple glyph outline data.
        /// </summary>
        /// <remarks>
        /// This property should only be accessed if <see cref="IsSimpleGlyph"/> is true.
        /// The returned struct holds references to rented arrays and must be disposed.
        /// </remarks>
        public SimpleGlyph SimpleGlyph => SimpleGlyph.Create(_glyphData.Span, NumberOfContours);

        /// <summary>
        /// Gets the composite glyph outline data.
        /// </summary>
        /// <remarks>
        /// This property should only be accessed if <see cref="IsSimpleGlyph"/> is false.
        /// The returned struct holds references to rented arrays and must be disposed.
        /// </remarks>
        public CompositeGlyph CompositeGlyph => CompositeGlyph.Create(_glyphData.Span);
    }
}
