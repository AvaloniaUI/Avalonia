using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Helper methods for parsing paint data.
    /// </summary>
    internal static class PaintParsingHelpers
    {
        public static uint ReadOffset24(ReadOnlySpan<byte> span)
        {
            // 24-bit offset (3 bytes, BIG-ENDIAN)
            return ((uint)span[0] << 16) | ((uint)span[1] << 8) | span[2];
        }

        public static double F2Dot14ToDouble(short value)
        {
            // F2DOT14: signed fixed-point number with 2 integer bits and 14 fractional bits
            return value / 16384.0;
        }

        public static double FixedToDouble(int value)
        {
            // Fixed 16.16 format
            return value / 65536.0;
        }

        /// <summary>
        /// Specifies the numeric format used to represent delta values in font tables.
        /// </summary>
        /// <remarks>Use this enumeration to indicate how a delta value should be interpreted or converted
        /// when processing font data. Each member corresponds to a specific numeric representation commonly found in
        /// OpenType and TrueType font tables.</remarks>
        public enum DeltaTargetType
        {
            /// <summary>FWORD - design units (int16) - no conversion needed</summary>
            FWORD,
            /// <summary>F2DOT14 - fixed-point with 2.14 format (divide by 16384)</summary>
            F2Dot14,
            /// <summary>Fixed - fixed-point with 16.16 format (divide by 65536)</summary>
            Fixed
        }

        public static double ConvertDelta(int deltaValue, DeltaTargetType targetType)
        {
            return targetType switch
            {
                DeltaTargetType.FWORD => deltaValue,
                DeltaTargetType.F2Dot14 => F2Dot14ToDouble((short)deltaValue),
                DeltaTargetType.Fixed => FixedToDouble(deltaValue),
                _ => deltaValue
            };
        }

        /// <summary>
        /// Reads the Affine2x3 (format 12) or VarAffine2x3 (format 13) a PaintTransform points to.
        /// <paramref name="span"/> starts at the paint: uint8 format, Offset24 paint, Offset24
        /// transform. A VarAffine2x3 is the six Fixed fields followed by its uint32 VarIndexBase.
        /// </summary>
        /// <returns><c>false</c> when the transform table does not fit in the data.</returns>
        public static bool TryParseAffine2x3(ReadOnlySpan<byte> span, bool isVariable, out Matrix matrix,
            out uint varIndexBase)
        {
            matrix = Matrix.Identity;
            varIndexBase = 0xFFFFFFFF;

            var size = isVariable ? 28 : 24;

            if (span.Length < 7)
            {
                return false;
            }

            var transformOffset = ReadOffset24(span.Slice(4));

            if ((long)transformOffset + size > span.Length)
            {
                return false;
            }

            var transformSpan = span.Slice((int)transformOffset, size);

            var xx = FixedToDouble(BinaryPrimitives.ReadInt32BigEndian(transformSpan));
            var yx = FixedToDouble(BinaryPrimitives.ReadInt32BigEndian(transformSpan.Slice(4)));
            var xy = FixedToDouble(BinaryPrimitives.ReadInt32BigEndian(transformSpan.Slice(8)));
            var yy = FixedToDouble(BinaryPrimitives.ReadInt32BigEndian(transformSpan.Slice(12)));
            var dx = FixedToDouble(BinaryPrimitives.ReadInt32BigEndian(transformSpan.Slice(16)));
            var dy = FixedToDouble(BinaryPrimitives.ReadInt32BigEndian(transformSpan.Slice(20)));

            if (isVariable)
            {
                varIndexBase = BinaryPrimitives.ReadUInt32BigEndian(transformSpan.Slice(24));
            }

            matrix = new Matrix(xx, yx, xy, yy, dx, dy);
            return true;
        }

        public static bool TryParseColorLine(
            ReadOnlySpan<byte> data,
            uint offset,
            in ColrContext context,
            bool isVarColorLine,
            out GradientStop[] stops,
            out GradientSpreadMethod extend)
        {
            stops = Array.Empty<GradientStop>();
            extend = GradientSpreadMethod.Pad;

            if (offset >= data.Length)
            {
                return false;
            }

            var span = data.Slice((int)offset);

            if (span.Length < 3) // extend (1) + numStops (2)
            {
                return false;
            }

            extend = span[0] switch
            {
                1 => GradientSpreadMethod.Repeat,
                2 => GradientSpreadMethod.Reflect,
                _ => GradientSpreadMethod.Pad
            };

            var numStops = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(1));

            // Validate numStops is reasonable
            // Gradients with more than 256 stops are likely corrupt data
            if (numStops == 0)
            {
                return false;
            }

            // Stops are kept as the COLR GradientStop record, not ImmutableGradientStop, because the
            // latter clamps offsets to [0, 1].
            // ColorStop is 6 bytes, VarColorStop is 10 bytes (each has varIndexBase)
            int stopSize = isVarColorLine ? 10 : 6;

            // Ensure we have enough data for all stops
            var requiredLength = 3 + (numStops * stopSize);
            if (span.Length < requiredLength)
            {
                return false;
            }

            var tempStops = new GradientStop[numStops];

            int stopOffset = 3;
            for (int i = 0; i < numStops; i++)
            {
                // ColorStop / VarColorStop share the first 6 bytes: F2DOT14 stopOffset (2),
                // uint16 paletteIndex (2), F2DOT14 alpha (2). VarColorStop appends uint32 varIndexBase.
                var stopPos = F2Dot14ToDouble(BinaryPrimitives.ReadInt16BigEndian(span.Slice(stopOffset)));
                var paletteIndex = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(stopOffset + 2));
                var alpha = F2Dot14ToDouble(BinaryPrimitives.ReadInt16BigEndian(span.Slice(stopOffset + 4)));

                if (isVarColorLine)
                {
                    // Apply this stop's variation deltas (both F2DOT14): offset = base+0, alpha = base+1.
                    var varIndexBase = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(stopOffset + 6));
                    stopPos += context.GetF2Dot14Delta(varIndexBase, 0);
                    alpha += context.GetF2Dot14Delta(varIndexBase, 1);
                }

                // Stop offsets keep their raw value: the color line extends past [0, 1], and the
                // resolver maps the stops onto [0, 1] by moving the gradient geometry. Alpha is a
                // coverage multiplier and is clamped.
                alpha = Math.Clamp(alpha, 0.0, 1.0);

                if (!context.CpalTable.TryGetColor(context.PaletteIndex, paletteIndex, out var color))
                {
                    color = Colors.Black;
                }

                color = Color.FromArgb((byte)(color.A * alpha), color.R, color.G, color.B);
                tempStops[i] = new GradientStop(stopPos, color);

                stopOffset += stopSize;
            }

            // Sort stops by offset (required for proper gradient rendering). The sort is stable: stops
            // sharing an offset form a hard color transition and keep their font order.
            tempStops = tempStops.OrderBy(s => s.Offset).ToArray();

            // Remove consecutive duplicate stops (same offset AND color)
            // NOTE: We preserve stops with the same offset but different colors (hard color transitions)
            var deduplicatedList = new List<GradientStop>(numStops);
            const double epsilon = 1e-6;

            for (int i = 0; i < tempStops.Length; i++)
            {
                var stop = tempStops[i];

                // Always add the first stop
                if (i == 0)
                {
                    deduplicatedList.Add(stop);
                    continue;
                }

                var previous = tempStops[i - 1];
                bool sameOffset = Math.Abs(stop.Offset - previous.Offset) < epsilon;
                bool sameColor = stop.Color.Equals(previous.Color);

                // Only skip if BOTH offset and color are the same (true duplicate)
                // Keep stops with same offset but different color (hard color transition)
                if (sameOffset && sameColor)
                {
                    // This is a true duplicate - skip it
                    continue;
                }

                deduplicatedList.Add(stop);
            }

            stops = deduplicatedList.ToArray();
            return true;
        }
    }
}
