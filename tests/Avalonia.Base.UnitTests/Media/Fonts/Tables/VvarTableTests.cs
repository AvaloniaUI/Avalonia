using System;
using Avalonia.Base.UnitTests.Media.Fonts.Tables;
using Avalonia.Media;
using Avalonia.Media.Fonts.Tables.Variation;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    public class VvarTableTests
    {
        private const string InterRegularAsset =
            "resm:Avalonia.Base.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Base.UnitTests";

        private const string InterVariableAsset =
            "resm:Avalonia.Base.UnitTests.Assets.InterVariable.ttf?assembly=Avalonia.Base.UnitTests";

        // AdobeBlank2VF is the only test font that ships VVAR. It's intentionally
        // blank (2 glyphs, empty outlines), which is perfect for variation-table
        // testing where we care about advances/bearings, not outlines.
        private const string AdobeBlankAsset =
            "resm:Avalonia.Base.UnitTests.Assets.AdobeBlank2VF.ttf?assembly=Avalonia.Base.UnitTests";

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

            Assert.False(VvarTable.TryLoad(typeface, expectedAxisCount: 0, out var vvar));
            Assert.Null(vvar);
        }

        [Fact]
        public void TryLoad_Returns_False_For_Horizontal_Only_Variable_Font()
        {
            // Inter Variable doesn't ship VVAR — it's designed for horizontal text. We
            // should silently report no VVAR rather than crash or guess.
            var typeface = LoadTypeface(InterVariableAsset);

            Assert.False(VvarTable.TryLoad(typeface, expectedAxisCount: 2, out var vvar));
            Assert.Null(vvar);
        }

        [Fact]
        public void TryLoad_Returns_True_For_AdobeBlank()
        {
            // AdobeBlank2VF has 2 axes, 2 glyphs, and ships VVAR (verified via the gvar
            // / VVAR binary inspection that grounds this PR).
            var typeface = LoadTypeface(AdobeBlankAsset);

            Assert.True(VvarTable.TryLoad(typeface, expectedAxisCount: 2, out var vvar));
            Assert.NotNull(vvar);
            Assert.NotNull(vvar!.Store);
            Assert.Equal(2, vvar.Store.AxisCount);
        }

        [Fact]
        public void TryLoad_Rejects_Mismatched_Axis_Count()
        {
            var typeface = LoadTypeface(AdobeBlankAsset);

            Assert.False(VvarTable.TryLoad(typeface, expectedAxisCount: 5, out _));
        }

        [Fact]
        public void TryGetAdvanceHeightDelta_Returns_Zero_At_Default_Variation()
        {
            // Every region's scaler is 0 at the default-instance point.
            var typeface = LoadTypeface(AdobeBlankAsset);
            Assert.True(VvarTable.TryLoad(typeface, 2, out var vvar));

            Span<float> defaultCoords = stackalloc float[2] { 0f, 0f };

            Assert.True(vvar!.TryGetAdvanceHeightDelta(0, defaultCoords, out var delta));
            Assert.Equal(0f, delta);
        }

        [Fact]
        public void TryGetTopSideBearingDelta_Returns_Zero_When_TSB_Mapping_Absent()
        {
            // AdobeBlank2VF doesn't ship a TSB mapping (only advance), so the helper
            // returns true with delta=0 — same idempotent contract as HVAR's LSB.
            var typeface = LoadTypeface(AdobeBlankAsset);
            Assert.True(VvarTable.TryLoad(typeface, 2, out var vvar));

            Span<float> coords = stackalloc float[2] { 0f, 1f };

            Assert.True(vvar!.TryGetTopSideBearingDelta(0, coords, out var delta));
            Assert.Equal(0f, delta);
        }

        [Fact]
        public void TryGetAdvanceHeightDelta_Reads_Valid_Float_At_Axis_Extreme()
        {
            // We don't assert on the magnitude of the delta because AdobeBlank2VF's
            // designer may or may not have populated non-zero values; the test only
            // pins that the lookup succeeds (no out-of-range index, no malformed
            // packing) and that the result is a finite number.
            var typeface = LoadTypeface(AdobeBlankAsset);
            Assert.True(VvarTable.TryLoad(typeface, 2, out var vvar));

            Span<float> coords = stackalloc float[2] { 1f, 1f };

            Assert.True(vvar!.TryGetAdvanceHeightDelta(0, coords, out var delta));
            Assert.False(float.IsNaN(delta));
            Assert.False(float.IsInfinity(delta));
        }
    }
}
