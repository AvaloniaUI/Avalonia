using System;
using System.Collections.Generic;
using Avalonia.Base.UnitTests.Media.Fonts.Tables;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.Fonts.Tables.Variation;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    public class HvarTableTests
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

            Assert.False(HvarTable.TryLoad(typeface, expectedAxisCount: 0, out var hvar));
            Assert.Null(hvar);
        }

        [Fact]
        public void TryLoad_Returns_True_For_Inter_Variable()
        {
            var typeface = LoadTypeface(InterVariableAsset);

            Assert.True(HvarTable.TryLoad(typeface, expectedAxisCount: 2, out var hvar));
            Assert.NotNull(hvar);
            Assert.NotNull(hvar!.Store);
            Assert.Equal(2, hvar.Store.AxisCount);
        }

        [Fact]
        public void TryLoad_Rejects_Mismatched_Axis_Count()
        {
            // ItemVariationStore must agree with the caller's axis count; the offsets
            // depend on it.
            var typeface = LoadTypeface(InterVariableAsset);

            Assert.False(HvarTable.TryLoad(typeface, expectedAxisCount: 5, out _));
        }

        [Fact]
        public void TryGetAdvanceDelta_Returns_Zero_At_Default_Variation()
        {
            // At the default-instance point (all coords = 0) every region's scaler
            // contains a zero contribution unless peak == 0 — for HVAR's regions the
            // peaks are non-zero (each region's peak is at an axis extremum), so the
            // scaler should be 0 and the cumulative delta should be 0.
            var typeface = LoadTypeface(InterVariableAsset);
            Assert.True(HvarTable.TryLoad(typeface, 2, out var hvar));

            var glyph = typeface.CharacterToGlyphMap['A'];
            Span<float> defaultCoords = stackalloc float[2] { 0f, 0f };

            Assert.True(hvar!.TryGetAdvanceDelta(glyph, defaultCoords, out var delta));
            Assert.Equal(0f, delta);
        }

        [Fact]
        public void TryGetAdvanceDelta_Returns_NonZero_At_Bold_Weight()
        {
            // wght=1 (normalized max) → the bolder 'A' should have a positive advance
            // delta because the glyph is physically wider.
            var typeface = LoadTypeface(InterVariableAsset);
            Assert.True(HvarTable.TryLoad(typeface, 2, out var hvar));

            var glyph = typeface.CharacterToGlyphMap['A'];
            // axis order: opsz, wght. At wght=1.0 (max), advance should grow.
            Span<float> coords = stackalloc float[2] { 0f, 1f };

            Assert.True(hvar!.TryGetAdvanceDelta(glyph, coords, out var delta));
            Assert.True(delta > 0f, $"Expected advance delta > 0 at wght=1.0, got {delta}");
        }

        [Fact]
        public void TryGetLeftSideBearingDelta_Returns_Zero_When_LSB_Mapping_Absent()
        {
            // Inter Variable doesn't ship an LSB mapping in HVAR (advance-only), so
            // every call returns true with delta=0.
            var typeface = LoadTypeface(InterVariableAsset);
            Assert.True(HvarTable.TryLoad(typeface, 2, out var hvar));

            var glyph = typeface.CharacterToGlyphMap['A'];
            Span<float> coords = stackalloc float[2] { 0f, 1f };

            Assert.True(hvar!.TryGetLeftSideBearingDelta(glyph, coords, out var delta));
            Assert.Equal(0f, delta);
        }
    }
}
