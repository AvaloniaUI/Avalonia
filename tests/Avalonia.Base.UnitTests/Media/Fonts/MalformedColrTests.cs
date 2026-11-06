using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    /// <summary>
    /// Hostile COLR v1 header fields and counts: GetGlyphDrawing must neither throw nor read past the
    /// table, and a count larger than the data must be clamped to the records that fit.
    /// </summary>
    public class MalformedColrTests
    {
        // COLR v1 header field positions.
        private const int BaseGlyphListOffsetField = 14;
        private const int ClipListOffsetField = 22;

        // ColrV1Builder writes the BaseGlyphList right after the 34-byte header.
        private const int BaseGlyphListStart = 34;

        [Fact]
        public void BaseGlyphList_Offset_Past_The_Table_Yields_No_Drawing()
        {
            var typeface = Build(BaseGlyphListOffsetField, 0xFFFFFF00);

            Assert.Null(Record.Exception(() => typeface.GetGlyphDrawing(3)));
            Assert.Null(typeface.GetGlyphDrawing(3));
        }

        [Fact]
        public void BaseGlyphList_Count_Larger_Than_The_Data_Is_Clamped()
        {
            // numBaseGlyphPaintRecords = 0x7FFFFFFF over a list holding one record: the binary search
            // must stay within the records that fit and still find glyph 3.
            var typeface = Build(BaseGlyphListStart, 0x7FFFFFFF);

            IGlyphDrawing? drawing = null;
            Assert.Null(Record.Exception(() => drawing = typeface.GetGlyphDrawing(3)));
            Assert.NotNull(drawing);
            Assert.Equal(new Rect(0, -100, 100, 100), drawing!.Bounds);
        }

        [Fact]
        public void ClipList_Offset_Past_The_Table_Is_Ignored()
        {
            var typeface = Build(ClipListOffsetField, 0xFFFFFF00);

            IGlyphDrawing? drawing = null;
            Assert.Null(Record.Exception(() => drawing = typeface.GetGlyphDrawing(3)));
            Assert.NotNull(drawing);
        }

        [Fact]
        public void ClipList_Count_Larger_Than_The_Data_Is_Clamped()
        {
            var colr = BuildColr();
            var clipListStart = (int)ReadUInt32(colr, ClipListOffsetField);

            // numClips follows the uint8 format.
            var typeface = Graft(colr).PatchUInt32("COLR", clipListStart + 1, 0x7FFFFFFF).TryCreateGlyphTypeface()!;

            IGlyphDrawing? drawing = null;
            Assert.Null(Record.Exception(() => drawing = typeface.GetGlyphDrawing(3)));
            Assert.NotNull(drawing);
            Assert.Equal(new Rect(0, -100, 100, 100), drawing!.Bounds);
        }

        private static GlyphTypeface Build(int field, uint value)
            => Graft(BuildColr()).PatchUInt32("COLR", field, value).TryCreateGlyphTypeface()!;

        private static SyntheticFont Graft(byte[] colr)
            => ColrTestFont.Graft(SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular), colr);

        private static byte[] BuildColr()
        {
            var outlineGlyph = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular)
                .TryCreateGlyphTypeface()!.CharacterToGlyphMap['A'];

            return new ColrV1Builder()
                .BaseGlyph(3, ColrPaint.Glyph(outlineGlyph, ColrPaint.Solid(0)))
                .Clip(3, 0, 0, 100, 100)
                .Build();
        }

        private static uint ReadUInt32(byte[] data, int offset)
            => (uint)(data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3]);
    }
}
