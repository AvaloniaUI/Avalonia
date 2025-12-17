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
            IGlyphTypeface glyphTypeface,
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

        public IGlyphTypeface GlyphTypeface { get; }
        public ReadOnlyMemory<byte> ColrData { get; }
        public ColrTable ColrTable { get; }
        public CpalTable CpalTable { get; }
        public int PaletteIndex { get; }

        /// <summary>
        /// Applies alpha delta to a color.
        /// </summary>
        public Color ApplyAlphaDelta(Color color, uint varIndexBase, int deltaIndex)
        {
            if (!ColrTable.TryGetVariationDeltaSet(varIndexBase, out var deltaSet))
            {
                return color;
            }

            if (deltaIndex >= deltaSet.Count)
            {
                return color;
            }

            // Alpha deltas are F2DOT14 format
            var alphaDelta = deltaSet.GetF2Dot14Delta(deltaIndex);

            var newAlpha = Math.Clamp(color.A / 255.0 + alphaDelta, 0.0, 1.0);

            return Color.FromArgb((byte)(newAlpha * 255), color.R, color.G, color.B);
        }

        /// <summary>
        /// Applies affine transformation deltas to a matrix.
        /// </summary>
        public Matrix ApplyAffineDeltas(Matrix matrix, uint varIndexBase)
        {
            if (!ColrTable.TryGetVariationDeltaSet(varIndexBase, out var deltaSet))
            {
                return matrix;
            }

            // Affine2x3 matrix component deltas are Fixed format (16.16)
            // Note: Depending on the spec, these might need to be treated differently
            // For now, using the raw deltas as they might already be in the correct format
            var m11 = matrix.M11 + (deltaSet.Count > 0 ? deltaSet.GetFixedDelta(0) : 0.0);
            var m12 = matrix.M12 + (deltaSet.Count > 1 ? deltaSet.GetFixedDelta(1) : 0.0);
            var m21 = matrix.M21 + (deltaSet.Count > 2 ? deltaSet.GetFixedDelta(2) : 0.0);
            var m22 = matrix.M22 + (deltaSet.Count > 3 ? deltaSet.GetFixedDelta(3) : 0.0);
            var m31 = matrix.M31 + (deltaSet.Count > 4 ? deltaSet.GetFixedDelta(4) : 0.0);
            var m32 = matrix.M32 + (deltaSet.Count > 5 ? deltaSet.GetFixedDelta(5) : 0.0);

            return new Matrix(m11, m12, m21, m22, m31, m32);
        }

        /// <summary>
        /// Resolves color stops with variation deltas applied and normalized to 0-1 range.
        /// Based on fontations ColorStops::resolve and gradient normalization logic.
        /// </summary>
        public GradientStop[] ResolveColorStops(
            GradientStopVar[] stops,
            uint? varIndexBase)
        {
            if (stops.Length == 0)
                return Array.Empty<GradientStop>();

            // No variation deltas to apply, just normalize
            if (!varIndexBase.HasValue)
            {
                return NormalizeColorStops(stops);
            }

            // Check if we should apply variations
            DeltaSet deltaSet = default;
            var shouldApplyVariations = ColrTable.TryGetVariationDeltaSet(varIndexBase.Value, out deltaSet);

            GradientStop[]? resolvedStops = null;

            if (shouldApplyVariations)
            {
                resolvedStops = new GradientStop[stops.Length];

                for (int i = 0; i < stops.Length; i++)
                {
                    var stop = stops[i];
                    var offset = stop.Offset;
                    var color = stop.Color;

                    if (deltaSet.Count >= 2)
                    {
                        // Stop offset and alpha deltas are F2DOT14 format
                        offset += deltaSet.GetF2Dot14Delta(0);

                        // Apply alpha delta
                        var alphaDelta = deltaSet.GetF2Dot14Delta(1);
                        var newAlpha = Math.Clamp(color.A / 255.0 + alphaDelta, 0.0, 1.0);
                        color = Color.FromArgb((byte)(newAlpha * 255), color.R, color.G, color.B);
                    }

                    resolvedStops[i] = new GradientStop(offset, color);
                }
            }
            else
            {
                resolvedStops = stops;
            }

            return NormalizeColorStops(resolvedStops);
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
