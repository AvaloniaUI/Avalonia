using System;
using System.Drawing;
using System.Linq;
using Avalonia.Media.Immutable;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Context for parsing Paint tables and resolving ResolvedPaint, providing access to required tables and data.
    /// </summary>
    internal readonly struct ColrContext
    {
        public ColrContext(
            GlyphTypeface glyphTypeface,
            ColrTable colrTable,
            CpalTable cpalTable,
            int paletteIndex)
        {
            GlyphTypeface = glyphTypeface;
            ColrData = colrTable.ColrData;
            ColrTable = colrTable;
            CpalTable = cpalTable;
            PaletteIndex = paletteIndex;
        }

        public GlyphTypeface GlyphTypeface { get; }
        public ReadOnlyMemory<byte> ColrData { get; }
        public ColrTable ColrTable { get; }
        public CpalTable CpalTable { get; }
        public int PaletteIndex { get; }

        /// <summary>
        /// Resolves the region-scaled variation delta for a single varying field (in the field's raw
        /// units); 0 when the field does not vary at this instance.
        /// </summary>
        private float GetScaledDelta(uint varIndexBase, uint fieldOffset)
        {
            ReadOnlySpan<float> coords = GlyphTypeface.ActiveVariationCoords;
            return ColrTable.TryGetScaledDelta(varIndexBase, fieldOffset, coords, out var delta) ? delta : 0f;
        }

        /// <summary>The delta for an FWORD field (design units) at <paramref name="fieldOffset"/>.</summary>
        public double GetFWordDelta(uint varIndexBase, uint fieldOffset)
            => GetScaledDelta(varIndexBase, fieldOffset);

        /// <summary>The delta for an F2DOT14 field (scale / angle / alpha) at <paramref name="fieldOffset"/>.</summary>
        public double GetF2Dot14Delta(uint varIndexBase, uint fieldOffset)
            => GetScaledDelta(varIndexBase, fieldOffset) / 16384.0;

        /// <summary>The delta for a Fixed (16.16) field (affine components) at <paramref name="fieldOffset"/>.</summary>
        public double GetFixedDelta(uint varIndexBase, uint fieldOffset)
            => GetScaledDelta(varIndexBase, fieldOffset) / 65536.0;

        /// <summary>
        /// Applies the alpha delta for a variable solid / colour field to a colour. Alpha is an
        /// F2DOT14 field at <paramref name="fieldOffset"/> from <paramref name="varIndexBase"/>.
        /// </summary>
        public Color ApplyAlphaDelta(Color color, uint varIndexBase, uint fieldOffset)
        {
            var alphaDelta = GetF2Dot14Delta(varIndexBase, fieldOffset);

            if (alphaDelta == 0.0)
            {
                return color;
            }

            var newAlpha = Math.Clamp(color.A / 255.0 + alphaDelta, 0.0, 1.0);

            return Color.FromArgb((byte)(newAlpha * 255), color.R, color.G, color.B);
        }

        /// <summary>
        /// Applies the Affine2x3 component deltas (Fixed / 16.16) to a matrix. Each component is its
        /// own delta-set index: xx, yx, xy, yy, dx, dy → <c>VarIndexBase + 0..5</c> →
        /// M11, M12, M21, M22, M31, M32.
        /// </summary>
        public Matrix ApplyAffineDeltas(Matrix matrix, uint varIndexBase)
        {
            var m11 = matrix.M11 + GetFixedDelta(varIndexBase, 0);
            var m12 = matrix.M12 + GetFixedDelta(varIndexBase, 1);
            var m21 = matrix.M21 + GetFixedDelta(varIndexBase, 2);
            var m22 = matrix.M22 + GetFixedDelta(varIndexBase, 3);
            var m31 = matrix.M31 + GetFixedDelta(varIndexBase, 4);
            var m32 = matrix.M32 + GetFixedDelta(varIndexBase, 5);

            return new Matrix(m11, m12, m21, m22, m31, m32);
        }

        /// <summary>
        /// Copies a variable colour line's stops and normalizes them to the 0-1 range.
        /// </summary>
        /// <remarks>
        /// Per-stop colour-line variation is not applied here: the colour-line parser does not capture
        /// each VarColorStop's <c>varIndexBase</c> and folds alpha into the stop colour at parse time,
        /// so stop offset / alpha deltas can't be resolved correctly. Geometric and solid-alpha
        /// variation is handled via the paint records' own <c>VarIndexBase</c>.
        /// </remarks>
        public GradientStop[] ResolveColorStops(GradientStopVar[] stops)
        {
            if (stops.Length == 0)
            {
                return Array.Empty<GradientStop>();
            }

            var copy = new GradientStop[stops.Length];

            for (int i = 0; i < stops.Length; i++)
            {
                copy[i] = new GradientStop(stops[i].Offset, stops[i].Color);
            }

            return NormalizeColorStops(copy);
        }

        /// <summary>
        /// Normalizes color stops to 0-1 range and handles edge cases.
        /// Modifies the array in-place to avoid allocations when possible.
        /// </summary>
        public GradientStop[] NormalizeColorStops(GradientStop[] stops)
        {
            if (stops.Length == 0)
                return stops;

            // Sort by offset (ImmutableGradientStop is immutable, so we need to sort the array)
            Array.Sort(stops, (a, b) => a.Offset.CompareTo(b.Offset));

            // Get first and last stops for normalization
            var firstStop = stops[0];
            var lastStop = stops[stops.Length - 1];
            var colorStopRange = lastStop.Offset - firstStop.Offset;

            // If all stops are at the same position
            if (Math.Abs(colorStopRange) < 1e-6)
            {
                // For Pad mode with zero range, add an extra stop
                if (colorStopRange == 0.0)
                {
                    var newStops = new GradientStop[stops.Length + 1];
                    Array.Copy(stops, newStops, stops.Length);
                    newStops[stops.Length] = new GradientStop(lastStop.Offset + 1.0, lastStop.Color);
                    stops = newStops;
                    colorStopRange = 1.0;
                    firstStop = stops[0];
                }
                else
                {
                    return stops;
                }
            }

            // Check if normalization is needed
            var needsNormalization = Math.Abs(colorStopRange - 1.0) > 1e-6 || Math.Abs(firstStop.Offset) > 1e-6;

            if (!needsNormalization)
                return stops;

            // Normalize stops to 0-1 range
            var scale = 1.0 / colorStopRange;
            var startOffset = firstStop.Offset;

            // Create new array with normalized values
            var normalizedStops = new GradientStop[stops.Length];
            for (int i = 0; i < stops.Length; i++)
            {
                var stop = stops[i];
                var normalizedOffset = (stop.Offset - startOffset) * scale;
                normalizedStops[i] = new GradientStop(normalizedOffset, stop.Color);
            }

            return normalizedStops;
        }
    }
}
