using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts.Tables.Colr;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    /// <summary>
    /// Regression tests for the COLR v1 decycler. A cyclic / over-deep paint graph must be contained:
    /// the parse boundary catches the <c>DecyclerException</c> so it never escapes the public
    /// <see cref="GlyphTypeface.GetGlyphDrawing(ushort)"/>, and the <c>using var guard = decycler.Enter(...)</c>
    /// pattern keeps Enter/Exit balanced so a failed sub-paint can't poison a sibling.
    /// </summary>
    /// <remarks>
    /// Each test grafts a hand-built COLR v1 + CPAL table onto a real font (Inter) via
    /// <see cref="SyntheticFont"/>, then drives the (formerly) crashing path through the public API
    /// and asserts it no longer throws.
    /// </remarks>
    public class ColrDecyclerCharacterizationTests
    {
        [Fact]
        public void SelfReferential_ColrGlyph_Does_Not_Throw_From_GetGlyphDrawing()
        {
            // Glyph 3's paint is PaintColrGlyph(3): parsing it re-enters glyph 3 → cycle. The boundary
            // catch turns the DecyclerException into "no color drawing" instead of letting it escape.
            var typeface = BuildColrTypeface((3, 3));

            Assert.Null(Record.Exception(() => typeface.GetGlyphDrawing(3)));
        }

        [Fact]
        public void Mutual_ColrGlyph_Cycle_Does_Not_Throw_From_GetGlyphDrawing()
        {
            // Glyph 3 → PaintColrGlyph(4) and glyph 4 → PaintColrGlyph(3): A↔B cycle, contained.
            var typeface = BuildColrTypeface((3, 4), (4, 3));

            Assert.Null(Record.Exception(() => typeface.GetGlyphDrawing(3)));
        }

        [Fact]
        public void Deep_Acyclic_ColrGlyph_Chain_Exceeds_Depth_Limit_Without_Throwing()
        {
            // An acyclic chain g(10)→g(11)→…→g(74)→g(75) deeper than PaintDecycler.MaxTraversalDepth (64).
            // The depth limit still fires, but the exception is now caught at the boundary.
            const int chainLength = PaintDecycler.MaxTraversalDepth + 1; // 65 records: glyphs 10..74
            var records = new (ushort Glyph, ushort RefGlyph)[chainLength];
            for (var i = 0; i < chainLength; i++)
            {
                records[i] = ((ushort)(10 + i), (ushort)(11 + i));
            }

            var typeface = BuildColrTypeface(records);

            Assert.Null(Record.Exception(() => typeface.GetGlyphDrawing(10)));
        }

        [Fact]
        public void NonCyclic_ColrGlyph_Does_Not_Throw()
        {
            // Positive control: glyph 3 → PaintColrGlyph(99) where glyph 99 has no base record.
            // The reference resolves to "no paint" and GetGlyphDrawing returns a (drawing-nothing)
            // result without throwing — proving the harness's COLR is genuinely parsed and that the
            // non-cyclic path is graceful.
            var typeface = BuildColrTypeface((3, 99));

            var exception = Record.Exception(() => typeface.GetGlyphDrawing(3));

            Assert.Null(exception);
        }

        [Fact]
        public void Malformed_Sibling_Layer_Does_Not_Poison_The_Decycler()
        {
            // A PaintColrLayers with two layers that both reference color glyph 4 (which has no base
            // record). Each reference resolves to "no paint"; with the balanced using-guard the first
            // layer's failed lookup no longer leaves glyph 4 in the visited set, so the second layer is
            // not misdetected as a cycle — both skip gracefully and nothing throws.
            var typeface = BuildColrLayersTypeface(baseGlyph: 3, layerRefGlyph: 4);

            Assert.Null(Record.Exception(() => typeface.GetGlyphDrawing(3)));
        }

        [Fact]
        public void SelfReferential_Transform_Paint_Does_Not_Throw_From_GetGlyphDrawing()
        {
            // Glyph 3's paint is a PaintScaleUniform (format 20) whose 24-bit sub-paint offset is 0,
            // so it points at itself. This is a paint→paint cycle that never crosses a glyph
            // boundary: the old guard (entered only at PaintGlyph / PaintColrGlyph) missed it and
            // the parser recursed into an uncatchable StackOverflow. The offset-keyed decycler now
            // trips on the repeated paint offset and the boundary catch contains it.
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular)
                .Replace("COLR", BuildColrV1WithSelfReferentialPaint(glyph: 3))
                .Replace("CPAL", BuildMinimalCpal());

            var typeface = font.TryCreateGlyphTypeface();
            Assert.NotNull(typeface);

            Assert.Null(Record.Exception(() => typeface!.GetGlyphDrawing(3)));
        }

        [Fact]
        public void Long_Acyclic_Transform_Chain_Is_Bounded_Without_Throwing()
        {
            // A chain of PaintScaleUniform paints, each pointing to the next, deeper than the
            // decycler's depth limit. No cycle is involved — purely acyclic nesting — yet the old
            // code (which never entered the decycler on transform edges) would recurse the whole
            // chain. The per-paint depth guard now bounds it and the boundary catch contains it.
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular)
                .Replace("COLR", BuildColrV1WithTransformChain(glyph: 3, chainLength: PaintDecycler.MaxTraversalDepth + 5))
                .Replace("CPAL", BuildMinimalCpal());

            var typeface = font.TryCreateGlyphTypeface();
            Assert.NotNull(typeface);

            Assert.Null(Record.Exception(() => typeface!.GetGlyphDrawing(3)));
        }

        // ── helpers ──

        /// <summary>
        /// Builds a COLR v1 table whose <paramref name="glyph"/> paints a single PaintScaleUniform
        /// (format 20) with a sub-paint offset of 0 — a self-referential paint→paint edge.
        /// </summary>
        private static byte[] BuildColrV1WithSelfReferentialPaint(ushort glyph)
        {
            var colr = new BigEndianBuffer();
            WriteColrV1Header(colr, out var baseListOffsetPos);

            var listStart = colr.Position;
            colr.PatchUInt32(baseListOffsetPos, (uint)listStart);
            colr.UInt32(1);          // numBaseGlyphV1Records
            colr.UInt16(glyph);
            var paintOffsetPos = colr.ReserveOffset32();

            // PaintScaleUniform (format 20): format(1) + subPaintOffset24(3) + scale F2Dot14(2).
            colr.PatchUInt32(paintOffsetPos, (uint)(colr.Position - listStart));
            colr.UInt8(20);
            colr.UInt24(0);          // sub-paint offset 0 → this paint references itself
            colr.F2Dot14(1.0);       // scale

            return colr.ToArray();
        }

        /// <summary>
        /// Builds a COLR v1 table whose <paramref name="glyph"/> paints a chain of
        /// <paramref name="chainLength"/> PaintScaleUniform (format 20) paints, each referencing the
        /// next; the final one references a 1-byte stub (format 0, an unknown/leaf paint).
        /// </summary>
        private static byte[] BuildColrV1WithTransformChain(ushort glyph, int chainLength)
        {
            var colr = new BigEndianBuffer();
            WriteColrV1Header(colr, out var baseListOffsetPos);

            var listStart = colr.Position;
            colr.PatchUInt32(baseListOffsetPos, (uint)listStart);
            colr.UInt32(1);
            colr.UInt16(glyph);
            var paintOffsetPos = colr.ReserveOffset32();

            // Each ScaleUniform is 6 bytes; lay them contiguously so paint i's sub-paint offset is 6.
            colr.PatchUInt32(paintOffsetPos, (uint)(colr.Position - listStart));
            for (var i = 0; i < chainLength; i++)
            {
                colr.UInt8(20);
                colr.UInt24(6);      // next paint is the immediately following 6-byte ScaleUniform
                colr.F2Dot14(1.0);
            }

            // Leaf: format 0 is not a recursive paint, so the chain terminates here.
            colr.UInt8(0);

            return colr.ToArray();
        }

        /// <summary>Writes the 34-byte COLR v1 header, reserving only baseGlyphV1ListOffset (@14).</summary>
        private static void WriteColrV1Header(BigEndianBuffer colr, out int baseListOffsetPos)
        {
            colr.UInt16(1);   // version
            colr.UInt16(0);   // numBaseGlyphRecords (v0)
            colr.UInt32(0);   // baseGlyphRecordsOffset (v0)
            colr.UInt32(0);   // layerRecordsOffset (v0)
            colr.UInt16(0);   // numLayerRecords (v0)
            baseListOffsetPos = colr.ReserveOffset32(); // baseGlyphV1ListOffset @14
            colr.UInt32(0);   // layerV1ListOffset @18
            colr.UInt32(0);   // clipListOffset @22
            colr.UInt32(0);   // varIndexMapOffset @26
            colr.UInt32(0);   // itemVariationStoreOffset @30
        }

        private static GlyphTypeface BuildColrTypeface(params (ushort Glyph, ushort RefGlyph)[] records)
        {
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular)
                .Replace("COLR", BuildColrV1WithColrGlyphPaints(records))
                .Replace("CPAL", BuildMinimalCpal());

            var typeface = font.TryCreateGlyphTypeface();

            // Sanity: the grafted COLR/CPAL are well-formed enough that the font loads. If this ever
            // fails, the throw assertions below would be meaningless (GetGlyphDrawing short-circuits
            // to null when _colrTable / _cpalTable are absent).
            Assert.NotNull(typeface);
            return typeface!;
        }

        /// <summary>
        /// Builds a minimal COLR v1 table whose BaseGlyphV1List maps each given glyph to a
        /// <c>PaintColrGlyph</c> (format 11) that references another glyph.
        /// </summary>
        private static byte[] BuildColrV1WithColrGlyphPaints((ushort Glyph, ushort RefGlyph)[] records)
        {
            // BaseGlyphV1Records must be sorted by glyph ID (the reader binary-searches them).
            var sorted = records.OrderBy(r => r.Glyph).ToArray();

            var colr = new BigEndianBuffer();

            // COLR header (v1) — 34 bytes; only baseGlyphV1ListOffset is non-zero.
            colr.UInt16(1);   // version
            colr.UInt16(0);   // numBaseGlyphRecords (v0)
            colr.UInt32(0);   // baseGlyphRecordsOffset (v0)
            colr.UInt32(0);   // layerRecordsOffset (v0)
            colr.UInt16(0);   // numLayerRecords (v0)
            var baseListOffsetPos = colr.ReserveOffset32(); // baseGlyphV1ListOffset @14
            colr.UInt32(0);   // layerV1ListOffset @18
            colr.UInt32(0);   // clipListOffset @22
            colr.UInt32(0);   // varIndexMapOffset @26
            colr.UInt32(0);   // itemVariationStoreOffset @30

            var listStart = colr.Position;
            colr.PatchUInt32(baseListOffsetPos, (uint)listStart);

            // BaseGlyphV1List: uint32 numRecords, then sorted (uint16 glyphID, Offset32 paintOffset).
            colr.UInt32((uint)sorted.Length);
            var paintOffsetPositions = new int[sorted.Length];
            for (var i = 0; i < sorted.Length; i++)
            {
                colr.UInt16(sorted[i].Glyph);
                paintOffsetPositions[i] = colr.ReserveOffset32(); // paintOffset, relative to listStart
            }

            // Paints: PaintColrGlyph = uint8 format(11) + uint16 glyphID.
            for (var i = 0; i < sorted.Length; i++)
            {
                colr.PatchUInt32(paintOffsetPositions[i], (uint)(colr.Position - listStart));
                colr.UInt8(11);
                colr.UInt16(sorted[i].RefGlyph);
            }

            return colr.ToArray();
        }

        private static GlyphTypeface BuildColrLayersTypeface(ushort baseGlyph, ushort layerRefGlyph)
        {
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular)
                .Replace("COLR", BuildColrV1WithTwoLayersReferencing(baseGlyph, layerRefGlyph))
                .Replace("CPAL", BuildMinimalCpal());

            var typeface = font.TryCreateGlyphTypeface();
            Assert.NotNull(typeface);
            return typeface!;
        }

        /// <summary>
        /// Builds a COLR v1 table where <paramref name="baseGlyph"/>'s paint is a
        /// <c>PaintColrLayers</c> with two layers, both <c>PaintColrGlyph(<paramref name="refGlyph"/>)</c>.
        /// <paramref name="refGlyph"/> deliberately has no base record, so each layer's lookup fails.
        /// </summary>
        private static byte[] BuildColrV1WithTwoLayersReferencing(ushort baseGlyph, ushort refGlyph)
        {
            var colr = new BigEndianBuffer();

            // COLR header (v1) — baseGlyphV1ListOffset @14 and layerV1ListOffset @18 are back-patched.
            colr.UInt16(1);   // version
            colr.UInt16(0);   // numBaseGlyphRecords (v0)
            colr.UInt32(0);   // baseGlyphRecordsOffset (v0)
            colr.UInt32(0);   // layerRecordsOffset (v0)
            colr.UInt16(0);   // numLayerRecords (v0)
            var baseListOffsetPos = colr.ReserveOffset32();  // baseGlyphV1ListOffset @14
            var layerListOffsetPos = colr.ReserveOffset32(); // layerV1ListOffset @18
            colr.UInt32(0);   // clipListOffset @22
            colr.UInt32(0);   // varIndexMapOffset @26
            colr.UInt32(0);   // itemVariationStoreOffset @30

            // BaseGlyphV1List: one record for baseGlyph → the PaintColrLayers paint.
            var baseListStart = colr.Position;
            colr.PatchUInt32(baseListOffsetPos, (uint)baseListStart);
            colr.UInt32(1);                 // numBaseGlyphV1Records
            colr.UInt16(baseGlyph);
            var recordPaintOffsetPos = colr.ReserveOffset32(); // paintOffset, relative to baseListStart

            // PaintColrLayers (format 1): uint8 format, uint8 numLayers, uint32 firstLayerIndex.
            colr.PatchUInt32(recordPaintOffsetPos, (uint)(colr.Position - baseListStart));
            colr.UInt8(1);
            colr.UInt8(2);    // numLayers
            colr.UInt32(0);   // firstLayerIndex

            // LayerV1List: uint32 numLayers, then Offset32[] (relative to the LayerV1List start).
            var layerListStart = colr.Position;
            colr.PatchUInt32(layerListOffsetPos, (uint)layerListStart);
            colr.UInt32(2);
            var layer0OffsetPos = colr.ReserveOffset32();
            var layer1OffsetPos = colr.ReserveOffset32();

            // Two PaintColrGlyph (format 11) layers, both referencing refGlyph.
            colr.PatchUInt32(layer0OffsetPos, (uint)(colr.Position - layerListStart));
            colr.UInt8(11);
            colr.UInt16(refGlyph);

            colr.PatchUInt32(layer1OffsetPos, (uint)(colr.Position - layerListStart));
            colr.UInt8(11);
            colr.UInt16(refGlyph);

            return colr.ToArray();
        }

        /// <summary>Builds a minimal valid CPAL v0 table (one 1-entry palette).</summary>
        private static byte[] BuildMinimalCpal()
        {
            var cpal = new BigEndianBuffer();

            cpal.UInt16(0);   // version
            cpal.UInt16(1);   // numPaletteEntries
            cpal.UInt16(1);   // numPalettes
            cpal.UInt16(1);   // numColorRecords
            var colorRecordsOffsetPos = cpal.ReserveOffset32(); // colorRecordsArrayOffset @8

            cpal.UInt16(0);   // colorRecordIndices[0] (palette 0's first color index)

            // The color records array starts here (offset 14); patch the header to point at it.
            cpal.PatchUInt32(colorRecordsOffsetPos, (uint)cpal.Position);
            cpal.UInt8(0).UInt8(0).UInt8(0).UInt8(0xFF); // one BGRA color record (opaque black)

            return cpal.ToArray();
        }
    }
}
