using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Avalonia.Media.Fonts.Tables.Glyf
{
    /// <summary>
    /// Represents a simple glyph outline as defined in the TrueType font format, including contour endpoints,
    /// instructions, flags, and coordinate data. Provides access to the parsed glyph data for rendering or analysis.
    /// </summary>
    /// <remarks>A simple glyph consists of one or more contours, each defined by a sequence of points. This
    /// struct exposes the raw data necessary to interpret the glyph's shape, including the endpoints of each contour,
    /// the TrueType instructions for hinting, per-point flags, and the X and Y coordinates of each point. The struct is
    /// intended for low-level font processing and is not thread-safe. This struct holds references to rented arrays
    /// that must be returned via Dispose.</remarks>
    internal readonly ref struct SimpleGlyph
    {
        // Rented buffers for flags
        private readonly GlyphFlag[]? _rentedFlags;
        // Rented buffers for  y-coordinates
        private readonly short[]? _rentedXCoords;
        // Rented buffers for x-coordinates
        private readonly short[]? _rentedYCoords;

        /// <summary>
        /// Gets the indices of the last point in each contour within the glyph outline.
        /// </summary>
        /// <remarks>The indices are zero-based and correspond to positions in the glyph's point array.
        /// The number of elements indicates the number of contours in the glyph.</remarks>
        public ReadOnlySpan<ushort> EndPtsOfContours { get; }

        /// <summary>
        /// Gets the instruction data.
        /// </summary>
        public ReadOnlySpan<byte> Instructions { get; }

        /// <summary>
        /// Gets the collection of flags associated with the glyph.
        /// </summary>
        public ReadOnlySpan<GlyphFlag> Flags { get; }

        /// <summary>
        /// Gets the X coordinates.
        /// </summary>
        public ReadOnlySpan<short> XCoordinates { get; }

        /// <summary>
        /// Gets the Y coordinates.
        /// </summary>
        public ReadOnlySpan<short> YCoordinates { get; }

        /// <summary>
        /// Initializes a new instance of the SimpleGlyph class using the specified contour endpoints, instructions,
        /// flags, and coordinate data.
        /// </summary>
        /// <remarks>The rented arrays, if supplied, are used to optimize memory usage and may be returned
        /// to a pool after use. Callers should not access or modify these arrays after passing them to the
        /// constructor.</remarks>
        /// <param name="endPtsOfContours">A read-only span containing the indices of the last point in each contour. The values define the structure
        /// of the glyph's outline.</param>
        /// <param name="instructions">A read-only span containing the TrueType instructions associated with the glyph. These instructions control
        /// glyph rendering and hinting.</param>
        /// <param name="flags">A read-only span of flags describing the attributes of each glyph point, such as whether a point is on-curve
        /// or off-curve.</param>
        /// <param name="xCoordinates">A read-only span containing the X coordinates for each glyph point, in font units.</param>
        /// <param name="yCoordinates">A read-only span containing the Y coordinates for each glyph point, in font units.</param>
        /// <param name="rentedFlags">An optional array of GlyphFlag values used for temporary storage. If provided, the array may be reused
        /// internally to reduce allocations.</param>
        /// <param name="rentedXCoords">An optional array of short values used for temporary storage of X coordinates. If provided, the array may be
        /// reused internally to reduce allocations.</param>
        /// <param name="rentedYCoords">An optional array of short values used for temporary storage of Y coordinates. If provided, the array may be
        /// reused internally to reduce allocations.</param>
        private SimpleGlyph(
            ReadOnlySpan<ushort> endPtsOfContours,
            ReadOnlySpan<byte> instructions,
            ReadOnlySpan<GlyphFlag> flags,
            ReadOnlySpan<short> xCoordinates,
            ReadOnlySpan<short> yCoordinates,
            GlyphFlag[]? rentedFlags,
            short[]? rentedXCoords,
            short[]? rentedYCoords)
        {
            EndPtsOfContours = endPtsOfContours;
            Instructions = instructions;
            Flags = flags;
            XCoordinates = xCoordinates;
            YCoordinates = yCoordinates;
            _rentedFlags = rentedFlags;
            _rentedXCoords = rentedXCoords;
            _rentedYCoords = rentedYCoords;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="SimpleGlyph"/> structure by parsing glyph data from the specified
        /// byte span.
        /// </summary>
        /// <remarks>The returned <see cref="SimpleGlyph"/> uses buffers rented from array pools for
        /// performance. Callers are responsible for disposing or returning these buffers if required by the <see
        /// cref="SimpleGlyph"/> implementation. The method does not validate the integrity of the glyph data beyond the
        /// contour count; malformed data may result in exceptions or undefined behavior.</remarks>
        /// <param name="data">A read-only span of bytes containing the raw glyph data to parse. The data must be formatted according to
        /// the TrueType simple glyph specification.</param>
        /// <param name="numberOfContours">The number of contours in the glyph. Must be greater than zero; otherwise, a default value is returned.</param>
        /// <returns>A <see cref="SimpleGlyph"/> instance representing the parsed glyph data. Returns the default value if
        /// <paramref name="numberOfContours"/> is less than or equal to zero.</returns>
        public static SimpleGlyph Create(ReadOnlySpan<byte> data, int numberOfContours)
        {
            if (numberOfContours <= 0)
            {
                return default;
            }

            // Endpoints of contours
            var endPtsOfContours = new ushort[numberOfContours];
            var endPtsBytes = data.Slice(0, numberOfContours * 2);

            for (int i = 0; i < numberOfContours; i++)
            {
                endPtsOfContours[i] = BinaryPrimitives.ReadUInt16BigEndian(endPtsBytes.Slice(i * 2, 2));
            }

            // Instructions
            int instructionsOffset = numberOfContours * 2;
            ushort instructionsLength = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(instructionsOffset, 2));
            var instructions = data.Slice(instructionsOffset + 2, instructionsLength);

            // Number of points
            int numPoints = endPtsOfContours[numberOfContours - 1] + 1;

            // Rent buffers
            int flagsOffset = instructionsOffset + 2 + instructionsLength;
            var flagsBuffer = ArrayPool<GlyphFlag>.Shared.Rent(numPoints);
            var xCoordsBuffer = ArrayPool<short>.Shared.Rent(numPoints);
            var yCoordsBuffer = ArrayPool<short>.Shared.Rent(numPoints);

            try
            {
                // Decode flags
                int flagIndex = 0;
                int offset = flagsOffset;

                while (flagIndex < numPoints)
                {
                    var flag = (GlyphFlag)data[offset++];
                    flagsBuffer[flagIndex++] = flag;

                    // Repeat flag
                    if ((flag & GlyphFlag.Repeat) != 0)
                    {
                        // Read repeat count
                        byte repeatCount = data[offset++];

                        // Repeat the flag
                        for (int i = 0; i < repeatCount && flagIndex < numPoints; i++)
                        {
                            flagsBuffer[flagIndex++] = flag;
                        }
                    }
                }

                // Decode X coordinates
                short x = 0;

                for (int i = 0; i < numPoints; i++)
                {
                    var flag = flagsBuffer[i];

                    // Short vector
                    if ((flag & GlyphFlag.XShortVector) != 0)
                    {
                        byte dx = data[offset++];

                        if ((flag & GlyphFlag.XIsSameOrPositiveXShortVector) != 0)
                        {
                            x += (short)dx;
                        }
                        else
                        {
                            x -= (short)dx;
                        }
                    }
                    else
                    {
                        // Not a short vector
                        if ((flag & GlyphFlag.XIsSameOrPositiveXShortVector) == 0)
                        {
                            short dx = BinaryPrimitives.ReadInt16BigEndian(data.Slice(offset, 2));
                            offset += 2;
                            x += dx;
                        }
                    }

                    xCoordsBuffer[i] = x;
                }

                // Decode Y coordinates
                short y = 0;

                for (int i = 0; i < numPoints; i++)
                {
                    var flag = flagsBuffer[i];

                    // Short vector
                    if ((flag & GlyphFlag.YShortVector) != 0)
                    {
                        byte dy = data[offset++];
                        if ((flag & GlyphFlag.YIsSameOrPositiveYShortVector) != 0)
                        {
                            y += (short)dy;
                        }
                        else
                        {
                            y -= (short)dy;
                        }
                    }
                    else
                    {
                        // Not a short vector
                        if ((flag & GlyphFlag.YIsSameOrPositiveYShortVector) == 0)
                        {
                            short dy = BinaryPrimitives.ReadInt16BigEndian(data.Slice(offset, 2));
                            offset += 2;
                            y += dy;
                        }
                    }

                    yCoordsBuffer[i] = y;
                }

                return new SimpleGlyph(
                    endPtsOfContours,
                    instructions,
                    flagsBuffer.AsSpan(0, numPoints),
                    xCoordsBuffer.AsSpan(0, numPoints),
                    yCoordsBuffer.AsSpan(0, numPoints),
                    flagsBuffer,
                    xCoordsBuffer,
                    yCoordsBuffer
                );
            }
            catch
            {
                // On exception, return buffers immediately
                ArrayPool<GlyphFlag>.Shared.Return(flagsBuffer);
                ArrayPool<short>.Shared.Return(xCoordsBuffer);
                ArrayPool<short>.Shared.Return(yCoordsBuffer);
                throw;
            }
        }

        /// <summary>
        /// Returns the rented buffers to the ArrayPool.
        /// </summary>
        /// <remarks>This method should be called when the SimpleGlyph is no longer needed
        /// to ensure the rented buffers are returned to the pool.</remarks>
        public void Dispose()
        {
            if (_rentedFlags != null)
            {
                ArrayPool<GlyphFlag>.Shared.Return(_rentedFlags);
            }

            if (_rentedXCoords != null)
            {
                ArrayPool<short>.Shared.Return(_rentedXCoords);
            }

            if (_rentedYCoords != null)
            {
                ArrayPool<short>.Shared.Return(_rentedYCoords);
            }
        }
    }
}
