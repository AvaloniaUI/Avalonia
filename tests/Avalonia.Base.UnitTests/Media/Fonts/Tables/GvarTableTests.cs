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
    public class GvarTableTests
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

            Assert.False(GvarTable.TryLoad(typeface, expectedAxisCount: 0, expectedGlyphCount: typeface.GlyphCount, out var gvar));
            Assert.Null(gvar);
        }

        [Fact]
        public void TryLoad_Returns_True_For_Inter_Variable()
        {
            // Inter Variable carries gvar with axisCount=2 (matching fvar) and 2937 glyph
            // entries (matching maxp). The cross-table invariants from the spec.
            var typeface = LoadTypeface(InterVariableAsset);

            Assert.True(GvarTable.TryLoad(typeface, expectedAxisCount: 2, expectedGlyphCount: typeface.GlyphCount, out var gvar));
            Assert.NotNull(gvar);
            Assert.Equal(2, gvar!.AxisCount);
            Assert.Equal(typeface.GlyphCount, gvar.GlyphCount);
        }

        [Fact]
        public void TryLoad_Rejects_Mismatched_Axis_Count()
        {
            // If the caller's axis count doesn't match the table's, the table is treated
            // as unusable rather than risking misaligned reads.
            var typeface = LoadTypeface(InterVariableAsset);

            Assert.False(GvarTable.TryLoad(typeface, expectedAxisCount: 5, expectedGlyphCount: typeface.GlyphCount, out _));
        }

        [Fact]
        public void TryGetGlyphVariationData_Returns_False_For_Glyph_With_No_Variation()
        {
            // Glyph 0 (.notdef) in Inter Variable has no variation data — its outline is
            // constant across axis space. The table encodes this as a zero-length entry.
            var typeface = LoadTypeface(InterVariableAsset);
            Assert.True(GvarTable.TryLoad(typeface, 2, typeface.GlyphCount, out var gvar));

            Assert.False(gvar!.TryGetGlyphVariationData(0, out var data));
            Assert.True(data.IsEmpty);
        }

        [Fact]
        public void TryGetGlyphVariationData_Returns_True_For_Glyph_With_Variation()
        {
            // Glyph 1 (which Inter Variable assigns to 'space' or similar) has 36 bytes
            // of variation data. Any glyph with non-empty bytes is a valid signal that
            // the offset lookup works.
            var typeface = LoadTypeface(InterVariableAsset);
            Assert.True(GvarTable.TryLoad(typeface, 2, typeface.GlyphCount, out var gvar));

            Assert.True(gvar!.TryGetGlyphVariationData(1, out var data));
            Assert.False(data.IsEmpty);
            Assert.True(data.Length >= 4); // At least tvCount + dataOffset
        }

        [Fact]
        public void TryGetGlyphVariationData_Returns_False_For_Out_Of_Range_Glyph()
        {
            var typeface = LoadTypeface(InterVariableAsset);
            Assert.True(GvarTable.TryLoad(typeface, 2, typeface.GlyphCount, out var gvar));

            Assert.False(gvar!.TryGetGlyphVariationData(typeface.GlyphCount + 100, out _));
        }

        [Fact]
        public void SharedTuples_Match_Inter_Variable_Manifest()
        {
            // Inter Variable declares 5 shared tuples; the first three are well-known
            // peak combinations:
            //   (1.0, 0.0)   wght default / opsz max
            //   (1.0, 1.0)   wght max / opsz max
            //   (1.0, -1.0)  wght min / opsz max
            // Verified by hand against the binary fvar+gvar tables.
            var typeface = LoadTypeface(InterVariableAsset);
            Assert.True(GvarTable.TryLoad(typeface, 2, typeface.GlyphCount, out var gvar));

            Assert.Equal(5, gvar!.SharedTupleCount);

            Span<float> coords = stackalloc float[2];

            Assert.True(gvar.TryGetSharedTuple(0, coords));
            Assert.Equal(1.0f, coords[0]);
            Assert.Equal(0.0f, coords[1]);

            Assert.True(gvar.TryGetSharedTuple(1, coords));
            Assert.Equal(1.0f, coords[0]);
            Assert.Equal(1.0f, coords[1]);

            Assert.True(gvar.TryGetSharedTuple(2, coords));
            Assert.Equal(1.0f, coords[0]);
            Assert.Equal(-1.0f, coords[1]);
        }

        [Fact]
        public void TryGetSharedTuple_Returns_False_For_Out_Of_Range_Or_Short_Buffer()
        {
            var typeface = LoadTypeface(InterVariableAsset);
            Assert.True(GvarTable.TryLoad(typeface, 2, typeface.GlyphCount, out var gvar));

            Span<float> tooSmall = stackalloc float[1];
            Assert.False(gvar!.TryGetSharedTuple(0, tooSmall));

            Span<float> okSize = stackalloc float[2];
            Assert.False(gvar.TryGetSharedTuple(99, okSize));
        }
    }
}
