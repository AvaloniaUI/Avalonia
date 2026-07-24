using System;
using Avalonia.Media;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    public class LocaTableTests
    {
        private const string InterFontUri =
            "resm:Avalonia.Base.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Base.UnitTests";

        private static GlyphTypeface LoadInter()
        {
            var assetLoader = new StandardAssetLoader();
            using var stream = assetLoader.Open(new Uri(InterFontUri));
            return new GlyphTypeface(new CustomPlatformTypeface(stream));
        }

        private static LocaTable LoadLoca(GlyphTypeface typeface)
        {
            Assert.True(HeadTable.TryLoad(typeface, out var head));
            var maxp = MaxpTable.Load(typeface);
            var loca = LocaTable.Load(typeface, head!, maxp);
            Assert.NotNull(loca);
            return loca!;
        }

        [Fact]
        public void Load_Returns_Table_With_Glyphs_For_Inter()
        {
            var loca = LoadLoca(LoadInter());

            Assert.True(loca.GlyphCount > 0);
        }

        [Fact]
        public void GlyphCount_Matches_Maxp()
        {
            var typeface = LoadInter();
            var maxp = MaxpTable.Load(typeface);

            var loca = LoadLoca(typeface);

            Assert.Equal(maxp.NumGlyphs, loca.GlyphCount);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        public void TryGetOffsets_Returns_False_For_Out_Of_Range(int glyphIndex)
        {
            var loca = LoadLoca(LoadInter());

            Assert.False(loca.TryGetOffsets(glyphIndex, out _, out _));
        }

        [Fact]
        public void TryGetOffsets_Returns_False_At_GlyphCount()
        {
            var loca = LoadLoca(LoadInter());

            // GlyphCount is one past the last valid glyph index.
            Assert.False(loca.TryGetOffsets(loca.GlyphCount, out _, out _));
        }

        [Fact]
        public void TryGetOffsets_Yields_Ascending_Ranges_For_Every_Glyph()
        {
            var loca = LoadLoca(LoadInter());

            for (var i = 0; i < loca.GlyphCount; i++)
            {
                Assert.True(loca.TryGetOffsets(i, out var start, out var end));

                // A glyph's data range must be non-negative in length; equal start/end
                // marks an empty glyph.
                Assert.True(end >= start, $"Glyph {i}: end {end} < start {start}");
            }
        }

        [Fact]
        public void Empty_Glyph_Has_Equal_Offsets()
        {
            var typeface = LoadInter();
            var map = typeface.CharacterToGlyphMap;

            Assert.True(map.ContainsGlyph(' '));
            var spaceGlyph = map[' '];

            var loca = LoadLoca(typeface);

            Assert.True(loca.TryGetOffsets(spaceGlyph, out var start, out var end));

            // The space glyph carries no outline, so loca gives it a zero-length range.
            Assert.Equal(start, end);
        }

        [Fact]
        public void Letter_Glyph_Has_NonEmpty_Range()
        {
            var typeface = LoadInter();
            var map = typeface.CharacterToGlyphMap;

            Assert.True(map.ContainsGlyph('A'));
            var letterGlyph = map['A'];

            var loca = LoadLoca(typeface);

            Assert.True(loca.TryGetOffsets(letterGlyph, out var start, out var end));
            Assert.True(end > start, "'A' should have a non-empty glyf range.");
        }
    }
}
