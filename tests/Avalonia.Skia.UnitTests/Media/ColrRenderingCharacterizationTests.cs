using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    /// <summary>
    /// Tests for COLR v1 rendering correctness that require a real geometry backend (Skia): the
    /// painter only emits a fill when <c>GetGlyphOutline</c> returns geometry, so these run under a
    /// Skia render interface and capture the brush handed to <c>DrawGeometry</c> via a recording
    /// <see cref="DrawingContext"/>.
    /// </summary>
    public class ColrRenderingCharacterizationTests
    {
        // Skia.UnitTests embeds the RenderTests fonts, so Inter-Regular is available here too.
        private const string InterAsset =
            "resm:Avalonia.Skia.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Skia.UnitTests";

        [Fact]
        public void Two_Circle_Radial_Gradient_Maps_To_Focal_Point_And_Outer_Circle()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface())))
            {
                var outlineGlyph = SyntheticFont.FromAsset(InterAsset).TryCreateGlyphTypeface()!
                    .CharacterToGlyphMap['A'];

                // A genuine two-point radial: focal circle (0,0) r0=0, outer circle (100,200) r1=300.
                var colr = BuildColrV1GlyphRadialGradient(
                    baseGlyph: 3, outlineGlyph, c0X: 0, c0Y: 0, r0: 0, c1X: 100, c1Y: 200, r1: 300);

                var recorder = DrawColrGlyph(colr);

                var brush = Assert.IsType<ImmutableRadialGradientBrush>(Assert.Single(recorder.Brushes));

                // The painter maps the two COLR circles onto Avalonia's focal-point radial model:
                // circle 1 (c1/r1) becomes Center + outer radius, and circle 0's centre (c0) becomes
                // the focal point (GradientOrigin). The focal radius r0 has no equivalent and is
                // approximated as 0 — exact here because r0 == 0.
                Assert.NotEqual(brush.Center, brush.GradientOrigin);
                Assert.Equal(new Point(100, 200), brush.Center.Point);     // c1
                Assert.Equal(new Point(0, 0), brush.GradientOrigin.Point); // c0
                Assert.Equal(300, brush.RadiusX.Scalar);                   // r1
                Assert.Equal(300, brush.RadiusY.Scalar);
            }
        }

        [Fact]
        public void Sweep_Gradient_Angle_Is_Decoded_To_Degrees()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface())))
            {
                var outlineGlyph = SyntheticFont.FromAsset(InterAsset).TryCreateGlyphTypeface()!
                    .CharacterToGlyphMap['A'];

                // F2DOT14 sweep angles are 180° per 1.0 of value: 0.25 → 45°, 0.75 → 135°.
                var colr = BuildColrV1GlyphSweepGradient(
                    baseGlyph: 3, outlineGlyph, centerX: 0, centerY: 0,
                    startAngleF2Dot14: 4096 /* 0.25 */, endAngleF2Dot14: 12288 /* 0.75 */);

                var recorder = DrawColrGlyph(colr);

                var brush = Assert.IsType<ImmutableConicGradientBrush>(Assert.Single(recorder.Brushes));

                // IConicGradientBrush.Angle is in degrees. After the OpenType +180° shift, the
                // counter-clockwise→clockwise flip, and the start<end swap, 0.25/0.75 resolve to a
                // 45° start angle — NOT the ~-244° a radians-scaled-by-180 decode would produce.
                Assert.Equal(45.0, brush.Angle, 3);
            }
        }

        [Fact]
        public void Glyph_Drawing_Is_Memoised_Per_Glyph()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface())))
            {
                var outlineGlyph = SyntheticFont.FromAsset(InterAsset).TryCreateGlyphTypeface()!
                    .CharacterToGlyphMap['A'];
                var colr = BuildColrV1GlyphRadialGradient(
                    baseGlyph: 3, outlineGlyph, c0X: 0, c0Y: 0, r0: 0, c1X: 0, c1Y: 0, r1: 100);

                var typeface = ColrTestFont.Graft(SyntheticFont.FromAsset(InterAsset), colr).TryCreateGlyphTypeface();
                Assert.NotNull(typeface);

                var first = typeface!.GetGlyphDrawing(3);
                var second = typeface.GetGlyphDrawing(3);

                // The drawing parses the whole paint graph up front, so it is cached per glyph (per
                // typeface instance) rather than rebuilt on every call.
                Assert.NotNull(first);
                Assert.Same(first, second);
            }
        }

        [Fact]
        public void Palette_Selection_Resolves_Solid_Fills_From_The_Selected_Palette()
        {
            using (UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface())))
            {
                var outlineGlyph = SyntheticFont.FromAsset(InterAsset).TryCreateGlyphTypeface()!
                    .CharacterToGlyphMap['A'];

                // Two palettes, one entry each: palette 0 red, palette 1 blue. The paint references
                // palette entry 0, so the selected palette decides the fill colour.
                var typeface = ColrTestFont
                    .Graft(SyntheticFont.FromAsset(InterAsset),
                        BuildColrV1GlyphSolid(baseGlyph: 3, outlineGlyph),
                        ColrTestFont.Cpal(new[] { Colors.Red }, new[] { Colors.Blue }))
                    .TryCreateGlyphTypeface();
                Assert.NotNull(typeface);

                var drawing = typeface!.GetGlyphDrawing(3, new GlyphDrawingOptions { PaletteIndex = 1 });
                Assert.NotNull(drawing);

                var recorder = new RecordingDrawingContext();
                drawing!.Draw(recorder, new Point(0, 0));

                var brush = Assert.IsType<ImmutableSolidColorBrush>(Assert.Single(recorder.Brushes));
                Assert.Equal(Colors.Blue, brush.Color);
            }
        }

        private static RecordingDrawingContext DrawColrGlyph(byte[] colr)
        {
            var typeface = ColrTestFont.Graft(SyntheticFont.FromAsset(InterAsset), colr).TryCreateGlyphTypeface();
            Assert.NotNull(typeface);

            var drawing = typeface!.GetGlyphDrawing(3);
            Assert.NotNull(drawing);

            var recorder = new RecordingDrawingContext();
            drawing!.Draw(recorder, new Point(0, 0));
            return recorder;
        }

        /// <summary>
        /// COLR v1: base glyph → PaintGlyph(outlineGlyph) → PaintSolid referencing palette entry 0
        /// at full alpha. Sub-paint offsets are constant because the layout is sequential.
        /// </summary>
        private static byte[] BuildColrV1GlyphSolid(ushort baseGlyph, ushort outlineGlyph)
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

            // PaintGlyph (format 10), 6 bytes; PaintSolid follows, so sub-offset = 6.
            colr.PatchUInt32(recordPaintOffsetPos, (uint)(colr.Position - baseListStart));
            colr.UInt8(10);
            colr.UInt24(6);
            colr.UInt16(outlineGlyph);

            // PaintSolid (format 2): paletteIndex 0, alpha 1.0.
            colr.UInt8(2);
            colr.UInt16(0);
            colr.F2Dot14(1.0);

            return colr.ToArray();
        }

        /// <summary>
        /// COLR v1: base glyph → PaintGlyph(outlineGlyph) → PaintRadialGradient with a 2-stop color
        /// line. Sub-paint offsets are constant because the layout is sequential.
        /// </summary>
        private static byte[] BuildColrV1GlyphRadialGradient(
            ushort baseGlyph, ushort outlineGlyph,
            short c0X, short c0Y, ushort r0, short c1X, short c1Y, ushort r1)
        {
            var colr = new BigEndianBuffer();

            // COLR header (v1).
            colr.UInt16(1);
            colr.UInt16(0);
            colr.UInt32(0);
            colr.UInt32(0);
            colr.UInt16(0);
            var baseListOffsetPos = colr.ReserveOffset32(); // baseGlyphV1ListOffset @14
            colr.UInt32(0);   // layerV1ListOffset @18
            colr.UInt32(0);   // clipListOffset @22
            colr.UInt32(0);   // varIndexMapOffset @26
            colr.UInt32(0);   // itemVariationStoreOffset @30

            // BaseGlyphV1List → PaintGlyph.
            var baseListStart = colr.Position;
            colr.PatchUInt32(baseListOffsetPos, (uint)baseListStart);
            colr.UInt32(1);
            colr.UInt16(baseGlyph);
            var recordPaintOffsetPos = colr.ReserveOffset32();

            // PaintGlyph (format 10), 6 bytes; PaintRadialGradient follows, so sub-offset = 6.
            colr.PatchUInt32(recordPaintOffsetPos, (uint)(colr.Position - baseListStart));
            colr.UInt8(10);
            colr.UInt24(6);
            colr.UInt16(outlineGlyph);

            // PaintRadialGradient (format 6), 16 bytes; the ColorLine follows, so its offset = 16.
            colr.UInt8(6);
            colr.UInt24(16);          // colorLineOffset, relative to this paint
            colr.Int16(c0X).Int16(c0Y).UInt16(r0);
            colr.Int16(c1X).Int16(c1Y).UInt16(r1);

            // ColorLine: extend + numStops + 2 ColorStops (offset, paletteIndex, alpha).
            colr.UInt8(0);            // extend = Pad
            colr.UInt16(2);           // numStops
            colr.F2Dot14(0.0).UInt16(0).F2Dot14(1.0);
            colr.F2Dot14(1.0).UInt16(0).F2Dot14(1.0);

            return colr.ToArray();
        }

        /// <summary>
        /// COLR v1: base glyph → PaintGlyph(outlineGlyph) → PaintSweepGradient with a 2-stop colour
        /// line. The colour line follows the 12-byte sweep paint, so its offset is constant.
        /// </summary>
        private static byte[] BuildColrV1GlyphSweepGradient(
            ushort baseGlyph, ushort outlineGlyph,
            short centerX, short centerY, short startAngleF2Dot14, short endAngleF2Dot14)
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

            // PaintGlyph (format 10), 6 bytes; PaintSweepGradient follows, so sub-offset = 6.
            colr.PatchUInt32(recordPaintOffsetPos, (uint)(colr.Position - baseListStart));
            colr.UInt8(10);
            colr.UInt24(6);
            colr.UInt16(outlineGlyph);

            // PaintSweepGradient (format 8), 12 bytes; the ColorLine follows, so its offset = 12.
            colr.UInt8(8);
            colr.UInt24(12);          // colorLineOffset, relative to this paint
            colr.Int16(centerX).Int16(centerY);
            colr.Int16(startAngleF2Dot14).Int16(endAngleF2Dot14);

            // ColorLine: extend + numStops + 2 ColorStops (offset, paletteIndex, alpha).
            colr.UInt8(0);            // extend = Pad
            colr.UInt16(2);           // numStops
            colr.F2Dot14(0.0).UInt16(0).F2Dot14(1.0);
            colr.F2Dot14(1.0).UInt16(0).F2Dot14(1.0);

            return colr.ToArray();
        }

        /// <summary>
        /// A <see cref="DrawingContext"/> that records the brushes passed to <c>DrawGeometry</c> and
        /// the opacities pushed; everything else is a no-op. Modeled on the test suite's existing
        /// <c>MockDrawingContext</c>.
        /// </summary>
        private sealed class RecordingDrawingContext : DrawingContext
        {
            public List<IBrush?> Brushes { get; } = new();
            public List<double> Opacities { get; } = new();

            protected override void DrawGeometryCore(IBrush? brush, IPen? pen, IGeometryImpl geometry)
                => Brushes.Add(brush);

            protected override void PushOpacityCore(double opacity) => Opacities.Add(opacity);

            // Remaining abstract members — no-ops.
            protected override void DrawEllipseCore(IBrush? brush, IPen? pen, Rect rect) { }
            protected override void DrawLineCore(IPen pen, Point p1, Point p2) { }
            protected override void DrawRectangleCore(IBrush? brush, IPen? pen, RoundedRect rrect, BoxShadows boxShadows = default) { }
            protected override void PushClipCore(Rect rect) { }
            protected override void PushClipCore(RoundedRect rect) { }
            protected override void PushGeometryClipCore(Geometry clip) { }
            protected override void PushOpacityMaskCore(IBrush mask, Rect bounds) { }
            protected override void PushTransformCore(Matrix matrix) { }
            protected override void PushRenderOptionsCore(RenderOptions renderOptions) { }
            protected override void PushTextOptionsCore(TextOptions textOptions) { }
            protected override void PushEffectCore(IEffect effect, Rect bounds) { }
            protected override void PopClipCore() { }
            protected override void PopGeometryClipCore() { }
            protected override void PopOpacityCore() { }
            protected override void PopOpacityMaskCore() { }
            protected override void PopTransformCore() { }
            protected override void PopRenderOptionsCore() { }
            protected override void PopTextOptionsCore() { }
            protected override void PopEffectCore() { }
            protected override void DisposeCore() { }
            internal override void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect) { }
            public override void Custom(ICustomDrawOperation custom) { }
            public override void DrawGlyphRun(IBrush? foreground, GlyphRun glyphRun) { }
        }
    }
}
