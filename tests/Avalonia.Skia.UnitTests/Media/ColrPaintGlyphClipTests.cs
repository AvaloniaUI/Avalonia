using System.Collections.Generic;
using System.Linq;
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
    /// PaintGlyph clips its child paint to the glyph outline: the outline is a clip in the transform
    /// current at the PaintGlyph, and every fill inside fills the clip region. These tests record
    /// the DrawingContext calls a COLR v1 drawing makes.
    /// </summary>
    public class ColrPaintGlyphClipTests
    {
        private const string InterAsset =
            "resm:Avalonia.Skia.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Skia.UnitTests";

        [Fact]
        public void PaintGlyph_Over_A_Transform_Clips_In_The_Outer_Space_And_Fills_The_Clip()
        {
            using (Start())
            {
                var a = OutlineGlyph('A');

                // PaintGlyph(A) -> PaintTransform(scale 2) -> PaintSolid: the outline clip is pushed
                // before the scale, and the fill covers the clip instead of drawing a scaled 'A'.
                var ops = Draw(ColrPaint.Glyph(a, ColrPaint.Transform(2, 0, 0, 2, 0, 0, ColrPaint.Solid(0))));

                Assert.Equal(new[] { "PushTransform", "PushGeometryClip", "PushTransform", "DrawRectangle #FFFF0000",
                    "PopTransform", "PopGeometryClip", "PopTransform" }, ops);
            }
        }

        [Fact]
        public void PaintGlyph_Over_Layers_Fills_Every_Layer()
        {
            using (Start())
            {
                var a = OutlineGlyph('A');

                var ops = Draw(ColrPaint.Glyph(a, ColrPaint.Layers(ColrPaint.Solid(0), ColrPaint.Solid(1))));

                Assert.Equal(new[] { "PushTransform", "DrawGeometry #FFFF0000", "DrawGeometry #FF0000FF", "PopTransform" },
                    ops);
            }
        }

        [Fact]
        public void Nested_PaintGlyph_Intersects_The_Clips()
        {
            using (Start())
            {
                var a = OutlineGlyph('A');
                var o = OutlineGlyph('O');

                // The outer outline stays a clip while the inner outline is filled inside it.
                var ops = Draw(ColrPaint.Glyph(a, ColrPaint.Glyph(o, ColrPaint.Solid(0))));

                Assert.Equal(new[] { "PushTransform", "PushGeometryClip", "DrawGeometry #FFFF0000", "PopGeometryClip",
                    "PopTransform" }, ops);
            }
        }

        [Fact]
        public void PaintGlyph_Over_A_Composite_Keeps_The_Clip()
        {
            using (Start())
            {
                var a = OutlineGlyph('A');

                // Mode 3 is SrcOver. Both the backdrop and the source are filled inside the clip.
                var ops = Draw(ColrPaint.Glyph(a, ColrPaint.Composite(3, ColrPaint.Solid(1), ColrPaint.Solid(0))));

                Assert.Equal(new[] { "PushTransform", "PushGeometryClip", "PushOpacity", "DrawRectangle #FFFF0000",
                    "DrawRectangle #FF0000FF", "PopOpacity", "PopGeometryClip", "PopTransform" }, ops);
            }
        }

        [Fact]
        public void Root_Clip_Box_Clips_The_Glyph()
        {
            using (Start())
            {
                var a = OutlineGlyph('A');

                var ops = Draw(ColrPaint.Glyph(a, ColrPaint.Solid(0)), clip: true);

                Assert.Equal(new[] { "PushTransform", "PushClip", "DrawGeometry #FFFF0000", "PopClip", "PopTransform" },
                    ops);
            }
        }

        private static ushort OutlineGlyph(char c)
            => SyntheticFont.FromAsset(InterAsset).TryCreateGlyphTypeface()!.CharacterToGlyphMap[c];

        private static List<string> Draw(ColrPaint paint, bool clip = false)
        {
            var builder = new ColrV1Builder().BaseGlyph(3, paint);
            if (clip)
            {
                builder.Clip(3, 0, 0, 500, 500);
            }

            var colr = builder.Build();
            var typeface = ColrTestFont
                .Graft(SyntheticFont.FromAsset(InterAsset), colr, ColrTestFont.Cpal(new[] { Colors.Red, Colors.Blue }))
                .TryCreateGlyphTypeface()!;

            var drawing = typeface.GetGlyphDrawing(3);
            Assert.NotNull(drawing);

            var recorder = new OperationRecorder();
            drawing!.Draw(recorder, new Point(0, 0));
            return recorder.Operations;
        }

        private static System.IDisposable Start()
            => UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(renderInterface: new PlatformRenderInterface()));

        /// <summary>Records the name of every drawing call, with the colour of solid brushes.</summary>
        private sealed class OperationRecorder : DrawingContext
        {
            public List<string> Operations { get; } = new();

            private static string Describe(IBrush? brush)
                => brush is ISolidColorBrush solid ? $" #{solid.Color.ToUInt32():X8}" : "";

            protected override void DrawGeometryCore(IBrush? brush, IPen? pen, IGeometryImpl geometry)
                => Operations.Add("DrawGeometry" + Describe(brush));

            protected override void DrawRectangleCore(IBrush? brush, IPen? pen, RoundedRect rrect,
                BoxShadows boxShadows = default)
                => Operations.Add("DrawRectangle" + Describe(brush));

            protected override void PushClipCore(Rect rect) => Operations.Add("PushClip");
            protected override void PushClipCore(RoundedRect rect) => Operations.Add("PushClip");
            protected override void PushGeometryClipCore(Geometry clip) => Operations.Add("PushGeometryClip");
            protected override void PushOpacityCore(double opacity) => Operations.Add("PushOpacity");
            protected override void PushTransformCore(Matrix matrix) => Operations.Add("PushTransform");
            protected override void PopClipCore() => Operations.Add("PopClip");
            protected override void PopGeometryClipCore() => Operations.Add("PopGeometryClip");
            protected override void PopOpacityCore() => Operations.Add("PopOpacity");
            protected override void PopTransformCore() => Operations.Add("PopTransform");

            protected override void DrawEllipseCore(IBrush? brush, IPen? pen, Rect rect) { }
            protected override void DrawLineCore(IPen pen, Point p1, Point p2) { }
            protected override void PushOpacityMaskCore(IBrush mask, Rect bounds) { }
            protected override void PushRenderOptionsCore(RenderOptions renderOptions) { }
            protected override void PushTextOptionsCore(TextOptions textOptions) { }
            protected override void PushEffectCore(IEffect effect, Rect bounds) { }
            protected override void PopOpacityMaskCore() { }
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
