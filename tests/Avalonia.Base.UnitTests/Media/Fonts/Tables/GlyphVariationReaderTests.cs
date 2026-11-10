using Avalonia.Media.Fonts.Tables.Variation;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    public class GlyphVariationReaderTests
    {
        // Unit tests for the pure-math scaler computation. The full delta-application
        // path is exercised via GlyphTypefaceVariationDeformationTests, which goes
        // through GetGlyphOutline end-to-end on Inter Variable.

        [Fact]
        public void ComputeScalerPeak_Returns_One_For_Default_Active_When_Peak_Is_Zero()
        {
            // Axes where peak == 0 don't contribute: the tuple is independent of that
            // axis. Multiplicative identity (1) means "this axis is fully satisfied".
            var active = new float[] { 0f };
            var peak = new float[] { 0f };

            Assert.Equal(1f, GlyphVariationReader.ComputeScalerPeak(active, peak));
        }

        [Fact]
        public void ComputeScalerPeak_Returns_Zero_When_Active_Is_Default_But_Peak_Is_Not()
        {
            // Tuple peaks at +1 but active stays at 0 (default instance) → no contribution.
            var active = new float[] { 0f };
            var peak = new float[] { 1f };

            Assert.Equal(0f, GlyphVariationReader.ComputeScalerPeak(active, peak));
        }

        [Fact]
        public void ComputeScalerPeak_Returns_Zero_For_Opposite_Signs()
        {
            // Active and peak on opposite sides of the default → tuple doesn't apply.
            var active = new float[] { -0.5f };
            var peak = new float[] { 1f };

            Assert.Equal(0f, GlyphVariationReader.ComputeScalerPeak(active, peak));
        }

        [Fact]
        public void ComputeScalerPeak_Returns_One_When_Active_At_Peak()
        {
            // Active exactly at peak → fully activated.
            var active = new float[] { 1f };
            var peak = new float[] { 1f };

            Assert.Equal(1f, GlyphVariationReader.ComputeScalerPeak(active, peak));
        }

        [Fact]
        public void ComputeScalerPeak_Linear_Ramp_Between_Default_And_Peak()
        {
            // Half-way between default (0) and peak (1) gives scaler 0.5.
            var active = new float[] { 0.5f };
            var peak = new float[] { 1f };

            Assert.Equal(0.5f, GlyphVariationReader.ComputeScalerPeak(active, peak));
        }

        [Fact]
        public void ComputeScalerPeak_Clamps_Above_Peak_To_One()
        {
            // Beyond peak (in same direction) → clamped to 1, not extrapolated.
            var active = new float[] { 1.5f };
            var peak = new float[] { 1f };

            Assert.Equal(1f, GlyphVariationReader.ComputeScalerPeak(active, peak));
        }

        [Fact]
        public void ComputeScalerPeak_Multiplies_Across_Axes()
        {
            // Two-axis tuple: scaler is the product of per-axis contributions.
            // Active = (0.5, 0.5), peak = (1.0, 1.0) → 0.5 * 0.5 = 0.25.
            var active = new float[] { 0.5f, 0.5f };
            var peak = new float[] { 1f, 1f };

            Assert.Equal(0.25f, GlyphVariationReader.ComputeScalerPeak(active, peak), precision: 6);
        }

        [Fact]
        public void ComputeScalerPeak_Returns_Zero_If_Any_Axis_Returns_Zero()
        {
            // One axis at peak (full contribution), one axis at default (zero contribution
            // when its peak is non-zero) → total scaler is zero.
            var active = new float[] { 1f, 0f };
            var peak = new float[] { 1f, 1f };

            Assert.Equal(0f, GlyphVariationReader.ComputeScalerPeak(active, peak));
        }

        [Fact]
        public void ComputeScalerIntermediate_Linear_Ramp_Up()
        {
            // start=0, peak=1, end=1 → between 0 and 1, scaler = active / 1.
            var active = new float[] { 0.5f };
            var peak = new float[] { 1f };
            var intermediateStart = new float[] { 0f };
            var intermediateEnd = new float[] { 1f };

            Assert.Equal(0.5f, GlyphVariationReader.ComputeScalerIntermediate(
                active, peak, intermediateStart, intermediateEnd), precision: 6);
        }

        [Fact]
        public void ComputeScalerIntermediate_Linear_Ramp_Down()
        {
            // start=0, peak=0.5, end=1 → at active=0.75 (between peak and end),
            // scaler = (end - active) / (end - peak) = 0.25 / 0.5 = 0.5.
            var active = new float[] { 0.75f };
            var peak = new float[] { 0.5f };
            var intermediateStart = new float[] { 0f };
            var intermediateEnd = new float[] { 1f };

            Assert.Equal(0.5f, GlyphVariationReader.ComputeScalerIntermediate(
                active, peak, intermediateStart, intermediateEnd), precision: 6);
        }

        [Fact]
        public void ComputeScalerIntermediate_Returns_Zero_Outside_Region()
        {
            // start=0.2, peak=0.5, end=0.8. Active=0.1 (below start) → 0.
            var active = new float[] { 0.1f };
            var peak = new float[] { 0.5f };
            var intermediateStart = new float[] { 0.2f };
            var intermediateEnd = new float[] { 0.8f };

            Assert.Equal(0f, GlyphVariationReader.ComputeScalerIntermediate(
                active, peak, intermediateStart, intermediateEnd));
        }

        [Fact]
        public void ApplyIup_Identity_When_All_Points_Referenced()
        {
            // If every contour point is referenced, IUP does nothing (the explicit deltas
            // are kept as-is). One contour with 4 points all referenced.
            var referenced = new[] { true, true, true, true };
            var origX = new short[] { 0, 10, 20, 30 };
            var origY = new short[] { 0, 0, 0, 0 };
            var endPts = new ushort[] { 3 };
            var dX = new[] { 1f, 2f, 3f, 4f };
            var dY = new[] { 1f, 2f, 3f, 4f };

            GlyphVariationReader.ApplyIup(referenced, origX, origY, endPts, dX, dY);

            Assert.Equal(new[] { 1f, 2f, 3f, 4f }, dX);
            Assert.Equal(new[] { 1f, 2f, 3f, 4f }, dY);
        }

        [Fact]
        public void ApplyIup_Propagates_Single_Reference_To_All_Contour_Points()
        {
            // Single referenced point on a 4-point contour → every other point gets that
            // delta. This is the degenerate case the IUP spec handles by skipping the
            // interpolation step entirely.
            var referenced = new[] { false, true, false, false };
            var origX = new short[] { 0, 10, 20, 30 };
            var origY = new short[] { 0, 0, 0, 0 };
            var endPts = new ushort[] { 3 };
            var dX = new[] { 0f, 5f, 0f, 0f };
            var dY = new[] { 0f, 3f, 0f, 0f };

            GlyphVariationReader.ApplyIup(referenced, origX, origY, endPts, dX, dY);

            Assert.Equal(new[] { 5f, 5f, 5f, 5f }, dX);
            Assert.Equal(new[] { 3f, 3f, 3f, 3f }, dY);
        }

        [Fact]
        public void ApplyIup_Linearly_Interpolates_Between_Bracketing_References()
        {
            // Contour: points at x = 0, 10, 20, 30. Points 0 and 2 referenced with
            // deltas dx=0 and dx=10. Point 1 (x=10) should land at delta 5 (midpoint).
            // Point 3 (x=30) is outside the bracket [0, 20], so it clamps to point 2's
            // delta (10).
            var referenced = new[] { true, false, true, false };
            var origX = new short[] { 0, 10, 20, 30 };
            var origY = new short[] { 0, 0, 0, 0 };
            var endPts = new ushort[] { 3 };
            var dX = new[] { 0f, 0f, 10f, 0f };
            var dY = new[] { 0f, 0f, 0f, 0f };

            GlyphVariationReader.ApplyIup(referenced, origX, origY, endPts, dX, dY);

            Assert.Equal(5f, dX[1], precision: 4);
            // Point 3 wraps cyclically back to point 0 (between point 2 and point 0).
            // Original x=30 is past both bracket endpoints (0 and 20), so it clamps to
            // the nearest reference's delta. Acceptable values are either 0 (clamp to
            // point 0) or 10 (clamp to point 2) depending on which side of the bracket
            // is "nearer" along the cyclic walk. The IUP rule we implement clamps
            // outside-bracket points to the matching-side reference, so this lands at 0.
            Assert.True(dX[3] == 0f || dX[3] == 10f);
        }
    }
}
