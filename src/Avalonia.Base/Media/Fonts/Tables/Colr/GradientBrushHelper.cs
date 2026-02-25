using System;
using Avalonia.Media.Immutable;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    /// <summary>
    /// Helper class for creating gradient brushes with proper normalization.
    /// Based on fontations gradient normalization logic from traversal.rs.
    /// </summary>
    internal static class GradientBrushHelper
    {
        /// <summary>
        /// Creates a linear gradient brush with normalization.
        /// Converts from P0, P1, P2 representation to a simple two-point gradient.
        /// </summary>
        public static IBrush CreateLinearGradient(
            Point p0,
            Point p1,
            Point p2,
            Immutable.ImmutableGradientStop[] stops,
            GradientSpreadMethod extend)
        {
            // If no stops or single stop, return solid color
            if (stops.Length == 0)
                return new ImmutableSolidColorBrush(Colors.Transparent);

            if (stops.Length == 1)
                return new ImmutableSolidColorBrush(stops[0].Color);

            // If p0p1 or p0p2 are degenerate, use first color
            var p0ToP1 = p1 - p0;
            var p0ToP2 = p2 - p0;

            if (IsDegenerate(p0ToP1) || IsDegenerate(p0ToP2) ||
                Math.Abs(CrossProduct(p0ToP1, p0ToP2)) < 1e-6)
            {
                return new ImmutableSolidColorBrush(stops[0].Color);
            }

            // Compute P3 as orthogonal projection of p0->p1 onto perpendicular to p0->p2
            var perpToP2 = new Vector(p0ToP2.Y, -p0ToP2.X);
            var p3 = p0 + ProjectOnto(p0ToP1, perpToP2);

            // Stops are already ImmutableGradientStop[], pass directly (arrays implement IReadOnlyList)
            return new ImmutableLinearGradientBrush(
                gradientStops: stops,
                startPoint: new RelativePoint(p0, RelativeUnit.Absolute),
                endPoint: new RelativePoint(p3, RelativeUnit.Absolute),
                spreadMethod: extend
            );
        }

        /// <summary>
        /// Creates a radial gradient brush with normalization.
        /// </summary>
        public static IBrush CreateRadialGradient(
            Point c0,
            double r0,
            Point c1,
            double r1,
            Immutable.ImmutableGradientStop[] stops,
            GradientSpreadMethod extend)
        {
            if (stops.Length == 0)
                return new ImmutableSolidColorBrush(Colors.Transparent);

            if (stops.Length == 1)
                return new ImmutableSolidColorBrush(stops[0].Color);

            // Note: Negative radii can occur after normalization
            // The client should handle truncation at the 0 position

            // Stops are already ImmutableGradientStop[], pass directly (arrays implement IReadOnlyList)
            return new ImmutableRadialGradientBrush(
                stops,
                center: new RelativePoint(c0, RelativeUnit.Absolute),
                gradientOrigin: new RelativePoint(c1, RelativeUnit.Absolute),
                radiusX: new RelativeScalar(r0, RelativeUnit.Absolute),
                radiusY: new RelativeScalar(r1, RelativeUnit.Absolute),
                spreadMethod: extend
            );
        }

        /// <summary>
        /// Creates a conic (sweep) gradient brush with angle normalization.
        /// Angles are converted from counter-clockwise to clockwise for the shader.
        /// </summary>
        public static IBrush CreateConicGradient(
            Point center,
            double startAngle,
            double endAngle,
            Immutable.ImmutableGradientStop[] stops,
            GradientSpreadMethod extend)
        {
            if (stops.Length == 0)
                return new ImmutableSolidColorBrush(Colors.Transparent);

            if (stops.Length == 1)
                return new ImmutableSolidColorBrush(stops[0].Color);

            // OpenType 1.9.1 adds a shift to ease 0-360 degree specification
            var startAngleDeg = startAngle * 180.0 + 180.0;
            var endAngleDeg = endAngle * 180.0 + 180.0;

            // Convert from counter-clockwise to clockwise
            startAngleDeg = 360.0 - startAngleDeg;
            endAngleDeg = 360.0 - endAngleDeg;

            var finalStops = stops;

            // Swap if needed to ensure start < end
            if (startAngleDeg > endAngleDeg)
            {
                (startAngleDeg, endAngleDeg) = (endAngleDeg, startAngleDeg);

                // Reverse stops - only allocate if we need to reverse
                finalStops = ReverseStops(stops);
            }

            // If start == end and not Pad mode, nothing should be drawn
            if (Math.Abs(startAngleDeg - endAngleDeg) < 1e-6 && extend != GradientSpreadMethod.Pad)
            {
                return new ImmutableSolidColorBrush(Colors.Transparent);
            }

            return new ImmutableConicGradientBrush(
                finalStops,
                center: new RelativePoint(center, RelativeUnit.Absolute),
                angle: startAngleDeg,
                spreadMethod: extend
            );
        }

        /// <summary>
        /// Reverses gradient stops without LINQ to minimize allocations.
        /// ImmutableGradientStop constructor is (Color, double offset).
        /// </summary>
        private static Immutable.ImmutableGradientStop[] ReverseStops(Immutable.ImmutableGradientStop[] stops)
        {
            var length = stops.Length;
            var reversed = new Immutable.ImmutableGradientStop[length];
            
            // Reverse in-place without LINQ
            for (int i = 0; i < length; i++)
            {
                var originalStop = stops[length - 1 - i];
                // ImmutableGradientStop constructor: (Color color, double offset)
                reversed[i] = new Immutable.ImmutableGradientStop(1.0 - originalStop.Offset, originalStop.Color);
            }
            
            return reversed;
        }

        private static bool IsDegenerate(Vector v)
        {
            return Math.Abs(v.X) < 1e-6 && Math.Abs(v.Y) < 1e-6;
        }

        private static double CrossProduct(Vector a, Vector b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        private static double DotProduct(Vector a, Vector b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        private static Vector ProjectOnto(Vector vector, Vector onto)
        {
            var length = Math.Sqrt(onto.X * onto.X + onto.Y * onto.Y);
            if (length < 1e-6)
                return new Vector(0, 0);

            var normalized = onto / length;
            var scale = DotProduct(vector, onto) / length;
            return normalized * scale;
        }
    }
}
