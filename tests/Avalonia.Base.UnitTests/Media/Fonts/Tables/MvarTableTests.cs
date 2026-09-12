using System;
using Avalonia.Base.UnitTests.Media.Fonts.Tables;
using Avalonia.Media;
using Avalonia.Media.Fonts.Tables.Variation;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    public class MvarTableTests
    {
        private const string InterRegularAsset =
            "resm:Avalonia.Base.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Base.UnitTests";

        private const string InterVariableAsset =
            "resm:Avalonia.Base.UnitTests.Assets.InterVariable.ttf?assembly=Avalonia.Base.UnitTests";

        private static GlyphTypeface LoadTypeface(string assetUri)
        {
            var assetLoader = new StandardAssetLoader();
            using var stream = assetLoader.Open(new Uri(assetUri));
            return new GlyphTypeface(new CustomPlatformTypeface(stream));
        }

        [Fact]
        public void TryLoad_Returns_False_For_Static_Font()
        {
            var typeface = LoadTypeface(InterRegularAsset);

            Assert.False(MvarTable.TryLoad(typeface, expectedAxisCount: 0, out var mvar));
            Assert.Null(mvar);
        }

        [Fact]
        public void TryLoad_Returns_True_For_Inter_Variable()
        {
            var typeface = LoadTypeface(InterVariableAsset);

            Assert.True(MvarTable.TryLoad(typeface, expectedAxisCount: 2, out var mvar));
            Assert.NotNull(mvar);
            Assert.NotNull(mvar!.Store);
            Assert.Equal(2, mvar.Store.AxisCount);
        }

        [Fact]
        public void TryLoad_Rejects_Mismatched_Axis_Count()
        {
            var typeface = LoadTypeface(InterVariableAsset);

            Assert.False(MvarTable.TryLoad(typeface, expectedAxisCount: 5, out _));
        }

        [Fact]
        public void Inter_Variable_Declares_Underline_And_Strikeout_Records()
        {
            // Verified directly against Inter Variable's MVAR binary: 5 records
            // (stro, strs, undo, unds, xhgt). Ascent/descent/line gap are NOT in MVAR
            // for Inter — they're held constant across weights.
            var typeface = LoadTypeface(InterVariableAsset);
            Assert.True(MvarTable.TryLoad(typeface, 2, out var mvar));

            Assert.Equal(5, mvar!.RecordCount);

            Span<float> bold = stackalloc float[2] { 0f, 1f };

            // Underline / strikeout deltas should be non-zero at wght=1.0 (Inter's
            // strokes thicken with weight, so the underline / strikeout sizes do too).
            Assert.True(mvar.TryGetMetricDelta(MvarTags.UnderlineSize, bold, out var unds));
            Assert.NotEqual(0f, unds);

            Assert.True(mvar.TryGetMetricDelta(MvarTags.StrikeoutSize, bold, out var strs));
            Assert.NotEqual(0f, strs);
        }

        [Fact]
        public void Inter_Variable_Does_Not_Declare_Ascent_Record()
        {
            // hasc, hdsc, hlgp are absent in Inter Variable's MVAR — TryGetMetricDelta
            // should return false rather than guess a delta.
            var typeface = LoadTypeface(InterVariableAsset);
            Assert.True(MvarTable.TryLoad(typeface, 2, out var mvar));

            Span<float> coords = stackalloc float[2] { 0f, 1f };

            Assert.False(mvar!.TryGetMetricDelta(MvarTags.HorizontalAscender, coords, out var d));
            Assert.Equal(0f, d);

            Assert.False(mvar.TryGetMetricDelta(MvarTags.HorizontalDescender, coords, out d));
            Assert.False(mvar.TryGetMetricDelta(MvarTags.HorizontalLineGap, coords, out d));
        }

        [Fact]
        public void TryGetMetricDelta_Returns_Zero_At_Default_Variation()
        {
            // At the default-instance point every region's scaler is 0 (peaks are at
            // axis extrema), so every delta sum is 0.
            var typeface = LoadTypeface(InterVariableAsset);
            Assert.True(MvarTable.TryLoad(typeface, 2, out var mvar));

            Span<float> defaultCoords = stackalloc float[2] { 0f, 0f };

            Assert.True(mvar!.TryGetMetricDelta(MvarTags.UnderlineSize, defaultCoords, out var delta));
            Assert.Equal(0f, delta);
        }
    }
}
