using System;
using System.Buffers.Binary;
using System.Collections.Generic;

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

        public static Matrix ParseAffine2x3(ReadOnlySpan<byte> span)
        {
            // Format 12 layout: [format][paintOffset][transformOffset]
            if (span.Length < 7)
            {
                return Matrix.Identity;
            }

            var transformOffset = PaintParsingHelpers.ReadOffset24(span.Slice(4));

            if (transformOffset > (uint)span.Length || span.Length - (int)transformOffset < 24)
            {
                return Matrix.Identity;
            }

            var transformSpan = span.Slice((int)transformOffset, 24);

            var xx = FixedToDouble(BinaryPrimitives.ReadInt32BigEndian(transformSpan));
            var yx = FixedToDouble(BinaryPrimitives.ReadInt32BigEndian(transformSpan.Slice(4)));
            var xy = FixedToDouble(BinaryPrimitives.ReadInt32BigEndian(transformSpan.Slice(8)));
            var yy = FixedToDouble(BinaryPrimitives.ReadInt32BigEndian(transformSpan.Slice(12)));
            var dx = FixedToDouble(BinaryPrimitives.ReadInt32BigEndian(transformSpan.Slice(16)));
            var dy = FixedToDouble(BinaryPrimitives.ReadInt32BigEndian(transformSpan.Slice(20)));

            return new Matrix(xx, yx, xy, yy, dx, dy);
        }

        public static bool TryParseColorLine(
            ReadOnlySpan<byte> data,
            uint offset,
            in ColrContext context,
            bool isVarColorLine,
            out Immutable.ImmutableGradientStop[] stops,
            out GradientSpreadMethod extend)
        {
            stops = Array.Empty<Immutable.ImmutableGradientStop>();
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

            // ColorStop is 6 bytes, VarColorStop is 10 bytes (each has varIndexBase)
            int stopSize = isVarColorLine ? 10 : 6;

            // Ensure we have enough data for all stops
            var requiredLength = 3 + (numStops * stopSize);
            if (span.Length < requiredLength)
            {
                return false;
            }

            var tempStops = new Immutable.ImmutableGradientStop[numStops];

            int stopOffset = 3;
            for (int i = 0; i < numStops; i++)
            {
                // Both ColorStop and VarColorStop have the same first 6 bytes:
                // F2DOT14 stopOffset (2), uint16 paletteIndex (2), F2DOT14 alpha (2)
                // VarColorStop adds: uint32 varIndexBase (4) - which we ignore for now

                var stopPos = F2Dot14ToDouble(BinaryPrimitives.ReadInt16BigEndian(span.Slice(stopOffset)));

                // Clamp stopPos to valid [0, 1] range
                // According to OpenType spec, stops should be in [0,1] but font data may have issues
                stopPos = Math.Clamp(stopPos, 0.0, 1.0);

                var paletteIndex = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(stopOffset + 2));
                var alpha = F2Dot14ToDouble(BinaryPrimitives.ReadInt16BigEndian(span.Slice(stopOffset + 4)));

                // Clamp alpha to valid [0, 1] range
                alpha = Math.Clamp(alpha, 0.0, 1.0);

                if (!context.CpalTable.TryGetColor(context.PaletteIndex, paletteIndex, out var color))
                {
                    color = Colors.Black;
                }

                color = Color.FromArgb((byte)(color.A * alpha), color.R, color.G, color.B);
                tempStops[i] = new Immutable.ImmutableGradientStop(stopPos, color);

                stopOffset += stopSize;
            }

            // Sort stops by offset (required for proper gradient rendering)
            Array.Sort(tempStops, (a, b) => a.Offset.CompareTo(b.Offset));

            // Remove consecutive duplicate stops (same offset AND color)
            // NOTE: We preserve stops with the same offset but different colors (hard color transitions)
            var deduplicatedList = new List<Immutable.ImmutableGradientStop>(numStops);
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
