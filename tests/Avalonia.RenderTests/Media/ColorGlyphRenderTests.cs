using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.RenderTests
{
    /// <summary>
    /// Pixel-level render tests for colour glyphs: COLR v0 layer lists and COLR v1 paint graphs,
    /// drawn through <see cref="GlyphTypeface.GetGlyphDrawing(ushort)"/> /
    /// <see cref="IGlyphDrawing.Draw"/> against the full Skia pipeline. The COLR / CPAL tables are
    /// hand-built and grafted onto Inter (which supplies the layer outlines), so each test controls
    /// exactly one paint construct; the characterization tests assert the brushes the painter
    /// produces, these assert the pixels.
    /// </summary>
    public class ColorGlyphRenderTests : TestBase
    {
        private const string InterRegularAsset =
            "resm:Avalonia.Skia.RenderTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Skia.RenderTests";

        // The COLR base glyph id the drawing is requested for. Any in-range id works — it only
        // needs a COLR record; the painted outlines are the layer glyphs resolved via cmap.
        private const ushort BaseGlyph = 3;

        public ColorGlyphRenderTests()
            : base(@"Media\ColorGlyph")
        {
        }

        [Fact]
        public async Task Should_Render_Colr_V0_Layered_Glyph()
        {
            // Two stacked v0 layers: a red 'O' below a blue 'I'. Where they overlap the blue layer
            // must win, proving bottom-to-top layer order and per-layer CPAL colour resolution.
            var plain = CreatePlainTypeface();
            var colr = BuildColrV0(BaseGlyph,
                (plain.CharacterToGlyphMap['O'], 0),
                (plain.CharacterToGlyphMap['I'], 1));

            var typeface = CreateColorTypeface(colr, ColrTestFont.Cpal(new[] { Colors.Red, Colors.Blue }));

            await RenderToFile(BuildTarget(GetDrawing(typeface), typeface));
            CompareImages();
        }

        [Fact]
        public async Task Should_Render_Colr_V0_Glyph_With_Second_Palette()
        {
            // The same layered glyph drawn with GlyphDrawingOptions.PaletteIndex = 1: every layer
            // colour must come from the second palette (green / orange), end to end in pixels.
            var plain = CreatePlainTypeface();
            var colr = BuildColrV0(BaseGlyph,
                (plain.CharacterToGlyphMap['O'], 0),
                (plain.CharacterToGlyphMap['I'], 1));

            var typeface = CreateColorTypeface(colr, ColrTestFont.Cpal(
                new[] { Colors.Red, Colors.Blue },
                new[] { Colors.Green, Colors.Orange }));

            var drawing = typeface.GetGlyphDrawing(BaseGlyph, new GlyphDrawingOptions { PaletteIndex = 1 });
            Assert.NotNull(drawing);

            await RenderToFile(BuildTarget(drawing!, typeface));
            CompareImages();
        }

        [Fact]
        public async Task Should_Render_Colr_V1_Solid_Glyph()
        {
            // The smallest v1 paint graph: PaintGlyph('A') → PaintSolid(palette entry 0) — a red 'A'.
            var plain = CreatePlainTypeface();
            var colr = BuildColrV1Solid(BaseGlyph, plain.CharacterToGlyphMap['A'], paletteEntry: 0);

            var typeface = CreateColorTypeface(colr, ColrTestFont.Cpal(new[] { Colors.Red, Colors.Blue }));

            await RenderToFile(BuildTarget(GetDrawing(typeface), typeface));
            CompareImages();
        }

        [Fact]
        public async Task Should_Render_Colr_V1_Linear_Gradient_Glyph()
        {
            // PaintGlyph('A') → PaintLinearGradient, red → blue left-to-right across the glyph's
            // ink. p2 is perpendicular to p0→p1 (the spec's no-skew configuration), so the
            // effective gradient is exactly p0→p1.
            var plain = CreatePlainTypeface();
            var ink = InkBounds(plain, 'A');

            var colr = BuildColrV1LinearGradient(BaseGlyph, plain.CharacterToGlyphMap['A'],
                p0X: (short)ink.X, p0Y: 0,
                p1X: (short)ink.Right, p1Y: 0,
                p2X: (short)ink.X, p2Y: 1000);

            var typeface = CreateColorTypeface(colr, ColrTestFont.Cpal(new[] { Colors.Red, Colors.Blue }));

            await RenderToFile(BuildTarget(GetDrawing(typeface), typeface));
            CompareImages();
        }

        [Fact]
        public async Task Should_Render_Colr_V1_Radial_Gradient_Glyph()
        {
            // PaintGlyph('A') → PaintRadialGradient: a focal radial (r0 = 0) centred on the glyph's
            // ink box, red core fading to blue at a radius covering the whole glyph.
            var plain = CreatePlainTypeface();
            var ink = InkBounds(plain, 'A');
            var center = ink.Center;
            var radius = (ushort)(Math.Max(ink.Width, ink.Height) / 2);

            var colr = BuildColrV1RadialGradient(BaseGlyph, plain.CharacterToGlyphMap['A'],
                c0X: (short)center.X, c0Y: (short)center.Y, r0: 0,
                c1X: (short)center.X, c1Y: (short)center.Y, r1: radius);

            var typeface = CreateColorTypeface(colr, ColrTestFont.Cpal(new[] { Colors.Red, Colors.Blue }));

            await RenderToFile(BuildTarget(GetDrawing(typeface), typeface));
            CompareImages();
        }

        [Fact]
        public async Task Should_Render_Colr_V1_Sweep_Gradient_Glyph()
        {
            // PaintGlyph('A') → PaintSweepGradient centred on the glyph's ink box, sweeping red →
            // blue over half a turn (F2DOT14 angles 0.0 → 1.0 are 0° → 180° before the OpenType
            // +180° shift); the Pad extend fills the remaining arc with the end colours.
            var plain = CreatePlainTypeface();
            var ink = InkBounds(plain, 'A');
            var center = ink.Center;

            var colr = BuildColrV1SweepGradient(BaseGlyph, plain.CharacterToGlyphMap['A'],
                centerX: (short)center.X, centerY: (short)center.Y,
                startAngleF2Dot14: 0, endAngleF2Dot14: 16384);

            var typeface = CreateColorTypeface(colr, ColrTestFont.Cpal(new[] { Colors.Red, Colors.Blue }));

            await RenderToFile(BuildTarget(GetDrawing(typeface), typeface));
            CompareImages();
        }

        private static GlyphTypeface CreatePlainTypeface()
        {
            var typeface = SyntheticFont.FromAsset(InterRegularAsset).TryCreateGlyphTypeface();
            Assert.NotNull(typeface);
            return typeface!;
        }

        private static GlyphTypeface CreateColorTypeface(byte[] colr, byte[] cpal)
        {
            var typeface = ColrTestFont
                .Graft(SyntheticFont.FromAsset(InterRegularAsset), colr, cpal)
                .TryCreateGlyphTypeface();
            Assert.NotNull(typeface);
            return typeface!;
        }

        private static IGlyphDrawing GetDrawing(GlyphTypeface typeface)
        {
            var drawing = typeface.GetGlyphDrawing(BaseGlyph);
            Assert.NotNull(drawing);
            return drawing!;
        }

        // The layer glyph's ink box in font design units (Y-up), used to aim gradient geometry at
        // the glyph without hard-coding the font's UPM.
        private static Rect InkBounds(GlyphTypeface typeface, char ch)
        {
            var outline = typeface.GetGlyphOutline(typeface.CharacterToGlyphMap[ch]);
            Assert.NotNull(outline);
            return outline!.Bounds;
        }

        private static Border BuildTarget(IGlyphDrawing drawing, GlyphTypeface glyphTypeface)
            => new Border
            {
                Width = 240,
                Height = 240,
                Background = Brushes.White,
                Child = new ColorGlyphControl(drawing, glyphTypeface),
            };

        /// <summary>
        /// Renders one colour-glyph drawing. The drawing applies the font-space (Y-up) →
        /// drawing-space (Y-down) flip itself, so the control only pushes the em scale and places
        /// the baseline origin — mirroring how a text renderer would integrate
        /// <see cref="IGlyphDrawing"/>.
        /// </summary>
        private sealed class ColorGlyphControl : Control
        {
            private const double EmSize = 200;
            private const double Margin = 20;

            private readonly IGlyphDrawing _drawing;
            private readonly double _scale;

            public ColorGlyphControl(IGlyphDrawing drawing, GlyphTypeface glyphTypeface)
            {
                _drawing = drawing;
                _scale = EmSize / glyphTypeface.Metrics.DesignEmHeight;
            }

            public override void Render(DrawingContext context)
            {
                using (context.PushTransform(Matrix.CreateScale(_scale, _scale)
                                             * Matrix.CreateTranslation(Margin, EmSize + Margin)))
                {
                    _drawing.Draw(context, default);
                }
            }
        }

        /// <summary>
        /// COLR v0: one base glyph record covering all <paramref name="layers"/> (bottom to top),
        /// each layer a (cmap glyph, CPAL palette entry) pair. Offsets are constant because the
        /// layout is sequential.
        /// </summary>
        private static byte[] BuildColrV0(ushort baseGlyph, params (ushort GlyphId, ushort PaletteEntry)[] layers)
        {
            var colr = new BigEndianBuffer();

            colr.UInt16(0);                          // version
            colr.UInt16(1);                          // numBaseGlyphRecords
            var basePos = colr.ReserveOffset32();    // baseGlyphRecordsOffset
            var layerPos = colr.ReserveOffset32();   // layerRecordsOffset
            colr.UInt16((ushort)layers.Length);      // numLayerRecords

            // BaseGlyphRecord: glyphID, firstLayerIndex, numLayers.
            colr.PatchUInt32(basePos, (uint)colr.Position);
            colr.UInt16(baseGlyph).UInt16(0).UInt16((ushort)layers.Length);

            // LayerRecord[]: glyphID, paletteIndex.
            colr.PatchUInt32(layerPos, (uint)colr.Position);
            foreach (var (glyphId, paletteEntry) in layers)
            {
                colr.UInt16(glyphId).UInt16(paletteEntry);
            }

            return colr.ToArray();
        }

        // Writes the COLR v1 header and the BaseGlyphV1List up to (and including) the PaintGlyph
        // that wraps the actual fill paint, which the caller emits immediately after. The
        // PaintGlyph's sub-paint offset is 6 (its own size) because the layout is sequential.
        private static BigEndianBuffer BeginColrV1PaintGlyph(ushort baseGlyph, ushort outlineGlyph)
        {
            var colr = new BigEndianBuffer();

            // COLR header (v1).
            colr.UInt16(1);
            colr.UInt16(0);
            colr.UInt32(0);
            colr.UInt32(0);
            colr.UInt16(0);
            var baseListOffsetPos = colr.ReserveOffset32(); // baseGlyphV1ListOffset
            colr.UInt32(0);   // layerV1ListOffset
            colr.UInt32(0);   // clipListOffset
            colr.UInt32(0);   // varIndexMapOffset
            colr.UInt32(0);   // itemVariationStoreOffset

            // BaseGlyphV1List → PaintGlyph.
            var baseListStart = colr.Position;
            colr.PatchUInt32(baseListOffsetPos, (uint)baseListStart);
            colr.UInt32(1);
            colr.UInt16(baseGlyph);
            var recordPaintOffsetPos = colr.ReserveOffset32();

            // PaintGlyph (format 10), 6 bytes; the fill paint follows, so its offset is 6.
            colr.PatchUInt32(recordPaintOffsetPos, (uint)(colr.Position - baseListStart));
            colr.UInt8(10);
            colr.UInt24(6);
            colr.UInt16(outlineGlyph);

            return colr;
        }

        // A two-stop colour line: extend = Pad, stop 0 → palette entry 0, stop 1 → palette entry 1.
        private static void WriteRedToBlueColorLine(BigEndianBuffer colr)
        {
            colr.UInt8(0);            // extend = Pad
            colr.UInt16(2);           // numStops
            colr.F2Dot14(0.0).UInt16(0).F2Dot14(1.0);
            colr.F2Dot14(1.0).UInt16(1).F2Dot14(1.0);
        }

        /// <summary>COLR v1: PaintGlyph → PaintSolid (format 2) at full alpha.</summary>
        private static byte[] BuildColrV1Solid(ushort baseGlyph, ushort outlineGlyph, ushort paletteEntry)
        {
            var colr = BeginColrV1PaintGlyph(baseGlyph, outlineGlyph);

            colr.UInt8(2);
            colr.UInt16(paletteEntry);
            colr.F2Dot14(1.0);

            return colr.ToArray();
        }

        /// <summary>COLR v1: PaintGlyph → PaintLinearGradient (format 4), 16 bytes + colour line.</summary>
        private static byte[] BuildColrV1LinearGradient(ushort baseGlyph, ushort outlineGlyph,
            short p0X, short p0Y, short p1X, short p1Y, short p2X, short p2Y)
        {
            var colr = BeginColrV1PaintGlyph(baseGlyph, outlineGlyph);

            colr.UInt8(4);
            colr.UInt24(16);          // colorLineOffset, relative to this paint
            colr.Int16(p0X).Int16(p0Y).Int16(p1X).Int16(p1Y).Int16(p2X).Int16(p2Y);

            WriteRedToBlueColorLine(colr);

            return colr.ToArray();
        }

        /// <summary>COLR v1: PaintGlyph → PaintRadialGradient (format 6), 16 bytes + colour line.</summary>
        private static byte[] BuildColrV1RadialGradient(ushort baseGlyph, ushort outlineGlyph,
            short c0X, short c0Y, ushort r0, short c1X, short c1Y, ushort r1)
        {
            var colr = BeginColrV1PaintGlyph(baseGlyph, outlineGlyph);

            colr.UInt8(6);
            colr.UInt24(16);          // colorLineOffset, relative to this paint
            colr.Int16(c0X).Int16(c0Y).UInt16(r0);
            colr.Int16(c1X).Int16(c1Y).UInt16(r1);

            WriteRedToBlueColorLine(colr);

            return colr.ToArray();
        }

        /// <summary>COLR v1: PaintGlyph → PaintSweepGradient (format 8), 12 bytes + colour line.</summary>
        private static byte[] BuildColrV1SweepGradient(ushort baseGlyph, ushort outlineGlyph,
            short centerX, short centerY, short startAngleF2Dot14, short endAngleF2Dot14)
        {
            var colr = BeginColrV1PaintGlyph(baseGlyph, outlineGlyph);

            colr.UInt8(8);
            colr.UInt24(12);          // colorLineOffset, relative to this paint
            colr.Int16(centerX).Int16(centerY);
            colr.Int16(startAngleF2Dot14).Int16(endAngleF2Dot14);

            WriteRedToBlueColorLine(colr);

            return colr.ToArray();
        }
    }
}
