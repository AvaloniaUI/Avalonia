using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Fonts.Tables.Colr;
using Avalonia.UnitTests;
using Xunit;
using GradientStop = Avalonia.Media.Fonts.Tables.Colr.GradientStop;

namespace Avalonia.Skia.UnitTests.Media
{
    public class ColrTableTests
    {
        [Fact]
        public void Should_Load_COLR_Table_If_Present()
        {
            using (Start())
            {
                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Color Emoji");

                // Try to get glyph typeface - may or may not have COLR
                if (FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface))
                {
                    Assert.True(ColrTable.TryLoad(glyphTypeface, out var colrTable));

                    Assert.True(colrTable.Version <= 1);
                    Assert.True(colrTable.BaseGlyphCount >= 0);
                }
            }
        }

        [Fact]
        public void Should_Load_CPAL_Table_If_Present()
        {
            using (Start())
            {
                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Color Emoji");

                if (FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface))
                {
                    Assert.True(CpalTable.TryLoad(glyphTypeface, out var cpalTable));

                    Assert.True(cpalTable.Version <= 1);
                    Assert.True(cpalTable.PaletteCount > 0);
                    Assert.True(cpalTable.PaletteEntryCount > 0);
                }
            }
        }

        [Fact]
        public void Should_Return_Null_For_Missing_COLR_Table()
        {
            using (Start())
            {
                // Using a font that definitely doesn't have COLR
                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Mono");

                if (FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface))
                {
                    Assert.False(ColrTable.TryLoad(glyphTypeface, out _));
                }
            }
        }

        [Fact]
        public void Should_Get_Layers_For_Color_Glyph()
        {
            using (Start())
            {
                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Color Emoji");

                if (FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface))
                {
                    Assert.True(ColrTable.TryLoad(glyphTypeface, out var colrTable));
                    Assert.True(CpalTable.TryLoad(glyphTypeface, out var cpalTable));

                    // Try to get layers for a hypothetical color glyph
                    ushort testGlyphId = 0;

                    if (colrTable.TryGetBaseGlyphRecord(testGlyphId, out var baseRecord))
                    {
                        Assert.True(baseRecord.NumLayers > 0);

                        // Get each layer
                        for (int i = 0; i < baseRecord.NumLayers; i++)
                        {
                            if (colrTable.TryGetLayerRecord(baseRecord.FirstLayerIndex + i, out var layer))
                            {
                                // Get the color for this layer
                                if (cpalTable.TryGetColor(0, layer.PaletteIndex, out var color))
                                {
                                    Assert.NotEqual(default, color);
                                }
                            }
                        }
                    }
                }
            }
        }




        [Fact]
        public void Should_Verify_Transform_Coordinate_Conversion_Preserves_Final_Bounds()
        {
            using (Start())
            {
                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Color Emoji");

                if (!FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface))
                {
                    return;
                }

                Assert.True(ColrTable.TryLoad(glyphTypeface, out var colrTable));
                Assert.True(CpalTable.TryLoad(glyphTypeface, out var cpalTable));
                Assert.True(colrTable.HasV1Data);

                // Test with clock emoji (U+1F550)
                const int clockCodepoint = 0x1F550;
                var clockGlyphId = glyphTypeface.CharacterToGlyphMap[clockCodepoint];

                if (clockGlyphId == 0 || !colrTable.TryGetBaseGlyphV1Record(clockGlyphId, out var baseGlyphRecord))
                {
                    return;
                }

                if (!glyphTypeface.PlatformTypeface.TryGetTable(ColrTable.Tag, out var colrData))
                {
                    return;
                }

                var context = new ColrContext(glyphTypeface, colrTable, cpalTable, 0);
                var decycler = new PaintDecycler();

                var rootPaintOffset = colrTable.GetAbsolutePaintOffset(baseGlyphRecord.PaintOffset);

                if (!PaintParser.TryParse(colrData.Span, rootPaintOffset, in context, in decycler, out var rootPaint))
                {
                    return;
                }

                // Resolve the paint (this applies coordinate space conversion)
                var resolvedPaint = PaintResolver.ResolvePaint(rootPaint, in context);

                // Create a bounds tracking painter to measure actual rendered bounds
                var boundsPainter = new BoundsTrackingPainter(glyphTypeface);

                // Traverse the paint graph
                PaintTraverser.Traverse(resolvedPaint, boundsPainter);

                var finalBounds = boundsPainter.GetFinalBounds();

                // Verify bounds are reasonable (not collapsed or inverted)
                Assert.True(finalBounds.Width > 0, "Final bounds should have positive width");
                Assert.True(finalBounds.Height > 0, "Final bounds should have positive height");

                // The bounds should be roughly square for a clock face (allow some tolerance)
                var aspectRatio = finalBounds.Width / finalBounds.Height;
                Assert.True(aspectRatio > 0.5 && aspectRatio < 2.0,
                    $"Clock emoji aspect ratio should be roughly square, got {aspectRatio:F2}");

                // Verify that transforms didn't cause bounds to become excessively large or small
                // Typical emoji glyph bounds in font units are in the range of 0-2048
                Assert.True(finalBounds.Width < 10000, "Bounds width should not be excessively large");
                Assert.True(finalBounds.Height < 10000, "Bounds height should not be excessively large");
                Assert.True(finalBounds.Width > 10, "Bounds width should not be too small");
                Assert.True(finalBounds.Height > 10, "Bounds height should not be too small");

                System.Diagnostics.Debug.WriteLine($"Clock emoji 🕐 final bounds: {finalBounds}");
                System.Diagnostics.Debug.WriteLine($"  Width: {finalBounds.Width:F2}, Height: {finalBounds.Height:F2}");
                System.Diagnostics.Debug.WriteLine($"  Aspect ratio: {aspectRatio:F2}");
                System.Diagnostics.Debug.WriteLine($"  Total glyphs rendered: {boundsPainter.GlyphCount}");
            }
        }

        [Fact]
        public void Should_Verify_Bridge_At_Night_Emoji_Transform_Coordinate_Conversion_Preserves_Final_Bounds()
        {
            using (Start())
            {
                var typeface = new Typeface("resm:Avalonia.Skia.UnitTests.Assets?assembly=Avalonia.Skia.UnitTests#Noto Color Emoji");

                if (!FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface))
                {
                    return;
                }

                Assert.True(ColrTable.TryLoad(glyphTypeface, out var colrTable));
                Assert.True(CpalTable.TryLoad(glyphTypeface, out var cpalTable));
                Assert.True(colrTable.HasV1Data);

                // Test with bridge at night emoji (U+1F309)
                const int bridgeCodepoint = 0x1F309;
                var bridgeGlyphId = glyphTypeface.CharacterToGlyphMap[bridgeCodepoint];

                if (bridgeGlyphId == 0 || !colrTable.TryGetBaseGlyphV1Record(bridgeGlyphId, out var baseGlyphRecord))
                {
                    return;
                }

                if (!glyphTypeface.PlatformTypeface.TryGetTable(ColrTable.Tag, out var colrData))
                {
                    return;
                }

                var context = new ColrContext(glyphTypeface, colrTable, cpalTable, 0);
                var decycler = new PaintDecycler();

                var rootPaintOffset = colrTable.GetAbsolutePaintOffset(baseGlyphRecord.PaintOffset);

                if (!PaintParser.TryParse(colrData.Span, rootPaintOffset, in context, in decycler, out var rootPaint))
                {
                    return;
                }

                // Resolve the paint (this applies coordinate space conversion)
                var resolvedPaint = PaintResolver.ResolvePaint(rootPaint, in context);

                // Create a bounds tracking painter to measure actual rendered bounds
                var boundsPainter = new BoundsTrackingPainter(glyphTypeface);

                // Traverse the paint graph
                PaintTraverser.Traverse(resolvedPaint, boundsPainter);

                var finalBounds = boundsPainter.GetFinalBounds();

                // Verify bounds are reasonable (not collapsed or inverted)
                Assert.True(finalBounds.Width > 0, "Final bounds should have positive width");
                Assert.True(finalBounds.Height > 0, "Final bounds should have positive height");

                // The bounds should be roughly square for an emoji (allow some tolerance)
                var aspectRatio = finalBounds.Width / finalBounds.Height;
                Assert.True(aspectRatio > 0.5 && aspectRatio < 2.0,
                    $"Bridge at night emoji aspect ratio should be roughly square, got {aspectRatio:F2}");

                // Verify that transforms didn't cause bounds to become excessively large or small
                // Typical emoji glyph bounds in font units are in the range of 0-2048
                Assert.True(finalBounds.Width < 10000, "Bounds width should not be excessively large");
                Assert.True(finalBounds.Height < 10000, "Bounds height should not be excessively large");
                Assert.True(finalBounds.Width > 10, "Bounds width should not be too small");
                Assert.True(finalBounds.Height > 10, "Bounds height should not be too small");

                System.Diagnostics.Debug.WriteLine($"Bridge at night emoji 🌉 (U+{bridgeCodepoint:X4}, GlyphID: {bridgeGlyphId}) final bounds: {finalBounds}");
                System.Diagnostics.Debug.WriteLine($"  Width: {finalBounds.Width:F2}, Height: {finalBounds.Height:F2}");
                System.Diagnostics.Debug.WriteLine($"  Aspect ratio: {aspectRatio:F2}");
                System.Diagnostics.Debug.WriteLine($"  Total glyphs rendered: {boundsPainter.GlyphCount}");
            }
        }

        /// <summary>
        /// Custom painter that tracks the final bounds of all rendered content
        /// </summary>
        private class BoundsTrackingPainter : IColorPainter
        {
            private readonly GlyphTypeface _glyphTypeface;
            private readonly Stack<Matrix> _transforms = new Stack<Matrix>();
            private Rect _bounds = default;

            public int GlyphCount { get; private set; }

            public BoundsTrackingPainter(GlyphTypeface glyphTypeface)
            {
                _glyphTypeface = glyphTypeface;
                _transforms.Push(Matrix.Identity);
            }

            public Rect GetFinalBounds() => _bounds;

            public void PushTransform(Matrix transform)
            {
                var current = _transforms.Peek();
                _transforms.Push(current * transform);
            }

            public void PopTransform()
            {
                if (_transforms.Count > 1)
                {
                    _transforms.Pop();
                }
            }

            public void PushLayer(CompositeMode mode) { }
            public void PopLayer() { }
            public void PushClip(Rect clipBox) { }
            public void PopClip() { }
            public void FillSolid(Color color) { }
            public void FillLinearGradient(Point p0, Point p1, GradientStop[] stops, GradientSpreadMethod extend) { }
            public void FillRadialGradient(Point c0, double r0, Point c1, double r1, GradientStop[] stops, GradientSpreadMethod extend) { }
            public void FillConicGradient(Point center, double startAngle, double endAngle, GradientStop[] stops, GradientSpreadMethod extend) { }

            public void PushGlyphClip(ushort glyphIndex)
            {
                GlyphCount++;

                // Get glyph outline bounds
                var geometry = _glyphTypeface.GetGlyphOutline(glyphIndex);
                if (geometry != null)
                {
                    var glyphBounds = geometry.Bounds;

                    // Transform the bounds by current accumulated transform
                    var transform = _transforms.Peek();
                    var transformedBounds = glyphBounds.TransformToAABB(transform);

                    // Union with accumulated bounds
                    _bounds = _bounds.IsEmpty() ? transformedBounds : _bounds.Union(transformedBounds);
                }
            }
        }

        private static IDisposable Start()
        {
            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            return disposable;
        }
    }
}
