using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    /// <summary>
    /// COLR v1 glyph bounds, computed without a render backend. A ClipList yields bounds directly;
    /// the no-ClipList fallback unions the painted outlines' control-point extents, read via the
    /// outline tables' bounds path (the <c>glyf</c> header / a <c>BoundsGeometryContext</c> interpret
    /// pass) rather than by building geometry — so neither path needs a geometry backend.
    /// </summary>
    public class ColrRenderingCharacterizationTests
    {
        [Fact]
        public void Clipped_V1_Glyph_Derives_Bounds_From_The_Clip_Box()
        {
            var plain = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).TryCreateGlyphTypeface();
            Assert.NotNull(plain);
            var outlineGlyph = plain!.CharacterToGlyphMap['A'];

            // base 3 → PaintGlyph(outline) → PaintSolid, with a ClipList.
            var clipped = ColrTestFont
                .Graft(InterRegular(), BuildColrV1GlyphSolid(baseGlyph: 3, outlineGlyph, clip: (0, 0, 1000, 1000)))
                .TryCreateGlyphTypeface()!
                .GetGlyphDrawing(3);

            Assert.NotNull(clipped);

            // The clip box yields non-empty bounds without building any geometry — which also proves
            // the paint resolved (bounds are only computed when the paint parses).
            Assert.NotEqual(default, clipped!.Bounds);
        }

        [Fact]
        public void Clipless_V1_Glyph_Derives_Bounds_From_The_Painted_Outline()
        {
            var plain = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).TryCreateGlyphTypeface();
            Assert.NotNull(plain);
            var outlineGlyph = plain!.CharacterToGlyphMap['A'];

            // The referenced outline's control-point box (the glyf header for this static font),
            // flipped from font space (Y-up) to drawing space (Y-down) — what the fallback derives,
            // with no render backend.
            var header = new GlyphBounds[1];
            Assert.True(plain.TryGetGlyphBounds(new[] { outlineGlyph }, header));
            var box = header[0];
            var expected = new Rect(box.XMin, box.YMin, box.Width, box.Height)
                .TransformToAABB(Matrix.CreateScale(1, -1));

            // The identical glyph WITHOUT a ClipList falls back to the painted outline's extent — read
            // from the outline table's bounds path, no geometry built and no backend required.
            var clipless = ColrTestFont
                .Graft(InterRegular(), BuildColrV1GlyphSolid(baseGlyph: 3, outlineGlyph, clip: null))
                .TryCreateGlyphTypeface()!
                .GetGlyphDrawing(3);

            Assert.NotNull(clipless);
            Assert.NotEqual(default, clipless!.Bounds);
            Assert.Equal(expected, clipless.Bounds);
        }

        [Fact]
        public void Palette_Selection_Memoises_Per_Palette_And_Normalizes_Undefined_Indices()
        {
            var plain = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).TryCreateGlyphTypeface();
            Assert.NotNull(plain);
            var outlineGlyph = plain!.CharacterToGlyphMap['A'];

            // Two palettes, one entry each: palette 0 red, palette 1 blue.
            var typeface = ColrTestFont
                .Graft(InterRegular(),
                    BuildColrV1GlyphSolid(baseGlyph: 3, outlineGlyph, clip: (0, 0, 1000, 1000)),
                    ColrTestFont.Cpal(new[] { Colors.Red }, new[] { Colors.Blue }))
                .TryCreateGlyphTypeface();
            Assert.NotNull(typeface);

            var defaultPalette = typeface!.GetGlyphDrawing(3);
            var palette0 = typeface.GetGlyphDrawing(3, new GlyphDrawingOptions { PaletteIndex = 0 });
            var palette1 = typeface.GetGlyphDrawing(3, new GlyphDrawingOptions { PaletteIndex = 1 });
            var undefined = typeface.GetGlyphDrawing(3, new GlyphDrawingOptions { PaletteIndex = 99 });

            Assert.NotNull(defaultPalette);
            Assert.NotNull(palette1);

            // Explicit palette 0 and null options share the default palette's cached drawing; a
            // different palette gets its own memoised drawing; an index the font does not define
            // falls back to the default palette's entry instead of minting a junk one.
            Assert.Same(defaultPalette, palette0);
            Assert.NotSame(defaultPalette, palette1);
            Assert.Same(palette1, typeface.GetGlyphDrawing(3, new GlyphDrawingOptions { PaletteIndex = 1 }));
            Assert.Same(defaultPalette, undefined);
        }

        private static SyntheticFont InterRegular() => SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular);

        /// <summary>
        /// Builds a COLR v1 table: base glyph → PaintGlyph(outlineGlyph) → PaintSolid, with an
        /// optional single-glyph ClipList. Offsets are constant because the layout is sequential.
        /// </summary>
        private static byte[] BuildColrV1GlyphSolid(
            ushort baseGlyph,
            ushort outlineGlyph,
            (short XMin, short YMin, short XMax, short YMax)? clip)
        {
            var colr = new BigEndianBuffer();

            // COLR header (v1).
            colr.UInt16(1);   // version
            colr.UInt16(0);   // numBaseGlyphRecords
            colr.UInt32(0);   // baseGlyphRecordsOffset
            colr.UInt32(0);   // layerRecordsOffset
            colr.UInt16(0);   // numLayerRecords
            var baseListOffsetPos = colr.ReserveOffset32(); // baseGlyphV1ListOffset @14
            colr.UInt32(0);   // layerV1ListOffset @18
            var clipListOffsetPos = colr.ReserveOffset32(); // clipListOffset @22
            colr.UInt32(0);   // varIndexMapOffset @26
            colr.UInt32(0);   // itemVariationStoreOffset @30

            // BaseGlyphV1List: one record → the PaintGlyph paint.
            var baseListStart = colr.Position;
            colr.PatchUInt32(baseListOffsetPos, (uint)baseListStart);
            colr.UInt32(1);
            colr.UInt16(baseGlyph);
            var recordPaintOffsetPos = colr.ReserveOffset32(); // relative to baseListStart

            // PaintGlyph (format 10): format(1) + Offset24 paintOffset + uint16 glyphID = 6 bytes.
            // The sub-paint (PaintSolid) immediately follows, so the Offset24 is a constant 6.
            colr.PatchUInt32(recordPaintOffsetPos, (uint)(colr.Position - baseListStart));
            colr.UInt8(10);
            colr.UInt24(6);            // sub-paint offset, relative to this PaintGlyph
            colr.UInt16(outlineGlyph);

            // PaintSolid (format 2): format(1) + uint16 paletteIndex + F2Dot14 alpha = 5 bytes.
            colr.UInt8(2);
            colr.UInt16(0);            // paletteIndex
            colr.F2Dot14(1.0);         // alpha

            if (clip is { } box)
            {
                // ClipList format 1: uint8 format + uint32 numClips + ClipRecord[1].
                // ClipRecord: uint16 start + uint16 end + Offset24 clipBoxOffset (relative to ClipList).
                // The ClipBox immediately follows the single 7-byte record, so its offset is 5 + 7 = 12.
                var clipListStart = colr.Position;
                colr.PatchUInt32(clipListOffsetPos, (uint)clipListStart);
                colr.UInt8(1);
                colr.UInt32(1);
                colr.UInt16(baseGlyph);
                colr.UInt16(baseGlyph);
                colr.UInt24(12);       // clip-box offset, relative to ClipList start

                // ClipBox format 1: uint8 format + 4 FWORDs.
                colr.UInt8(1);
                colr.Int16(box.XMin).Int16(box.YMin).Int16(box.XMax).Int16(box.YMax);
            }

            return colr.ToArray();
        }
    }
}
