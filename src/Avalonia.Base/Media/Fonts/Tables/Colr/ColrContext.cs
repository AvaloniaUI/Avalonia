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

        /// <summary>The instance's normalized variation coordinates (empty at the default instance).</summary>
        public ReadOnlySpan<float> ActiveCoords => GlyphTypeface.ActiveVariationCoords;

        /// <summary>
        /// Resolves the region-scaled variation delta for a single varying field (in the field's raw
        /// units); 0 when the field does not vary at this instance.
        /// </summary>
        private float GetScaledDelta(uint varIndexBase, uint fieldOffset)
            => ColrTable.TryGetScaledDelta(varIndexBase, fieldOffset, ActiveCoords, out var delta) ? delta : 0f;

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
        /// Maps sorted color stops onto [0, 1], the range gradient brushes interpolate over.
        /// </summary>
        /// <remarks>
        /// COLR stop offsets may lie anywhere; brushes only honour [0, 1]. The stops are rescaled so
        /// the first lands at 0 and the last at 1, and <paramref name="first"/> / <paramref name="last"/>
        /// report the raw offsets that now map to 0 and 1. The caller must move the gradient geometry
        /// to those positions on the color line (as HarfBuzz and Skia do), or the colors land in the
        /// wrong place. Returns a new array unless the stops already span exactly [0, 1].
        /// </remarks>
        public GradientStop[] NormalizeColorStops(GradientStop[] stops, out double first, out double last)
        {
            first = 0.0;
            last = 1.0;

            if (stops.Length == 0)
                return stops;

            var firstStop = stops[0];
            var lastStop = stops[stops.Length - 1];
            var colorStopRange = lastStop.Offset - firstStop.Offset;

            // All stops at one position: extend the last color over one unit of the color line so the
            // range is not degenerate.
            if (Math.Abs(colorStopRange) < 1e-6)
            {
                var newStops = new GradientStop[stops.Length + 1];
                Array.Copy(stops, newStops, stops.Length);
                newStops[stops.Length] = new GradientStop(firstStop.Offset + 1.0, lastStop.Color);
                stops = newStops;
                colorStopRange = 1.0;
            }

            first = firstStop.Offset;
            last = first + colorStopRange;

            if (Math.Abs(first) <= 1e-6 && Math.Abs(colorStopRange - 1.0) <= 1e-6)
            {
                first = 0.0;
                last = 1.0;
                return stops;
            }

            var scale = 1.0 / colorStopRange;
            var startOffset = first;

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
