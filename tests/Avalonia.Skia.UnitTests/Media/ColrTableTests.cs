using System;
using System.Collections.Generic;
using System.Linq;
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

                // Test with clock emoji 🕐 (U+1F550)
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
                PaintTraverser.Traverse(resolvedPaint, boundsPainter, Matrix.Identity);

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

                // Test with bridge at night emoji 🌉 (U+1F309)
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
                PaintTraverser.Traverse(resolvedPaint, boundsPainter, Matrix.Identity);

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

        [Fact]
        public void Should_Analyze_Bridge_At_Night_Emoji_Paint_Graph_And_Verify_Transform_Accumulation()
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

                // Parse cmap to find glyph ID for U+1F309 (🌉 Bridge at Night)
                const int bridgeCodepoint = 0x1F309;
                var bridgeGlyphId = glyphTypeface.CharacterToGlyphMap[bridgeCodepoint];
                bool foundGlyph = bridgeGlyphId != 0;

                if (!foundGlyph)
                {
                    return;
                }

                // Verify this glyph has a COLR v1 record
                if (!colrTable.TryGetBaseGlyphV1Record(bridgeGlyphId, out var baseGlyphRecord))
                {
                    return;
                }

                // Get the COLR data for parsing
                if (!glyphTypeface.PlatformTypeface.TryGetTable(ColrTable.Tag, out var colrData))
                {
                    return;
                }

                // Create context for paint parsing
                var context = new ColrContext(
                    glyphTypeface,
                    colrTable,
                    cpalTable,
                    0);

                var decycler = new PaintDecycler();

                // Parse the root paint
                var rootPaintOffset = colrTable.GetAbsolutePaintOffset(baseGlyphRecord.PaintOffset);

                if (!PaintParser.TryParse(colrData.Span, rootPaintOffset, in context, in decycler, out var rootPaint))
                {
                    Assert.Fail("Failed to parse root paint for bridge at night emoji");
                    return;
                }

                // Resolve the paint (apply deltas, normalize)
                var resolvedPaint = PaintResolver.ResolvePaint(rootPaint, in context);

                // Create a tracking painter to analyze the paint graph
                var trackingPainter = new TransformTrackingPainter();

                // Traverse the paint graph
                PaintTraverser.Traverse(resolvedPaint, trackingPainter, Matrix.Identity);

                // Analyze the results
                Assert.NotEmpty(trackingPainter.TransformStack);

                // Output diagnostic information
                System.Diagnostics.Debug.WriteLine($"Bridge at night emoji 🌉 (U+{bridgeCodepoint:X4}, GlyphID: {bridgeGlyphId}) paint graph analysis:");
                System.Diagnostics.Debug.WriteLine($"  Total transforms encountered: {trackingPainter.AllTransforms.Count}");
                System.Diagnostics.Debug.WriteLine($"  Maximum transform stack depth: {trackingPainter.MaxStackDepth}");
                System.Diagnostics.Debug.WriteLine($"  Total glyphs encountered: {trackingPainter.GlyphCount}");
                System.Diagnostics.Debug.WriteLine($"  Total fills encountered: {trackingPainter.FillCount}");
                System.Diagnostics.Debug.WriteLine($"  Total clips encountered: {trackingPainter.ClipCount}");
                System.Diagnostics.Debug.WriteLine($"  Total layers encountered: {trackingPainter.LayerCount}");

                if (trackingPainter.AllTransforms.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("\n  Transform stack at each level:");
                    for (int i = 0; i < trackingPainter.TransformStack.Count && i < 10; i++)
                    {
                        var m = trackingPainter.TransformStack[i];
                        System.Diagnostics.Debug.WriteLine(
                            $"    Level {i}: [{m.M11:F4}, {m.M12:F4}, {m.M21:F4}, {m.M22:F4}, {m.M31:F4}, {m.M32:F4}]");
                    }

                    // Verify transforms were properly accumulated if any exist
                    for (int i = 1; i < trackingPainter.TransformStack.Count; i++)
                    {
                        var current = trackingPainter.TransformStack[i];
                        var previous = trackingPainter.TransformStack[i - 1];

                        // Current transform should be different from previous (transforms accumulated)
                        // Unless the transform was identity
                        var isIdentity = current.M11 == 1 && current.M12 == 0 &&
                                       current.M21 == 0 && current.M22 == 1 &&
                                       current.M31 == 0 && current.M32 == 0;

                        if (!isIdentity && i > 0)
                        {
                            // Verify that transform changed
                            bool transformChanged =
                                Math.Abs(current.M11 - previous.M11) > 0.0001 ||
                                Math.Abs(current.M12 - previous.M12) > 0.0001 ||
                                Math.Abs(current.M21 - previous.M21) > 0.0001 ||
                                Math.Abs(current.M22 - previous.M22) > 0.0001 ||
                                Math.Abs(current.M31 - previous.M31) > 0.0001 ||
                                Math.Abs(current.M32 - previous.M32) > 0.0001;

                            Assert.True(transformChanged,
                                $"Transform at level {i} should differ from level {i - 1} when accumulating non-identity transforms");
                        }
                    }
                }
            }
        }

        [Fact]
        public void Should_Debug_Bridge_At_Night_Emoji_Paint_Operations_In_Detail()
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

                // Test with bridge at night emoji 🌉 (U+1F309)
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

                // Resolve the paint
                var resolvedPaint = PaintResolver.ResolvePaint(rootPaint, in context);

                // Create a detailed diagnostic painter
                var diagnosticPainter = new DetailedDiagnosticPainter(glyphTypeface);

                // Traverse the paint graph
                PaintTraverser.Traverse(resolvedPaint, diagnosticPainter, Matrix.Identity);

                // Output all the collected diagnostic information
                System.Diagnostics.Debug.WriteLine($"\n=== Bridge at Night Emoji 🌉 (U+{bridgeCodepoint:X4}, GlyphID: {bridgeGlyphId}) Detailed Paint Operations ===\n");

                foreach (var op in diagnosticPainter.Operations)
                {
                    System.Diagnostics.Debug.WriteLine(op);
                }

                System.Diagnostics.Debug.WriteLine($"\n=== Summary ===");
                System.Diagnostics.Debug.WriteLine($"Total operations: {diagnosticPainter.Operations.Count}");
                System.Diagnostics.Debug.WriteLine($"Glyphs rendered: {diagnosticPainter.Operations.Count(o => o.Contains("Glyph"))}");
                System.Diagnostics.Debug.WriteLine($"Transforms: {diagnosticPainter.Operations.Count(o => o.Contains("PushTransform"))}");
                System.Diagnostics.Debug.WriteLine($"Fills: {diagnosticPainter.Operations.Count(o => o.Contains("Fill"))}");
                System.Diagnostics.Debug.WriteLine($"Clips: {diagnosticPainter.Operations.Count(o => o.Contains("Clip"))}");
                System.Diagnostics.Debug.WriteLine($"Layers: {diagnosticPainter.Operations.Count(o => o.Contains("Layer"))}");

                // Verify we got some operations
                Assert.NotEmpty(diagnosticPainter.Operations);
            }
        }

        /// <summary>
        /// Detailed diagnostic painter that logs every operation with full details
        /// </summary>
        private class DetailedDiagnosticPainter : IColorPainter
        {
            private readonly GlyphTypeface _glyphTypeface;
            private readonly Stack<Matrix> _transforms = new Stack<Matrix>();
            private int _operationIndex = 0;
            public List<string> Operations { get; } = new List<string>();

            public DetailedDiagnosticPainter(GlyphTypeface glyphTypeface)
            {
                _glyphTypeface = glyphTypeface;
                _transforms.Push(Matrix.Identity);
            }

            private string FormatMatrix(Matrix m)
            {
                return $"[{m.M11:F4}, {m.M12:F4}, {m.M21:F4}, {m.M22:F4}, {m.M31:F4}, {m.M32:F4}]";
            }

            private string FormatPoint(Point p)
            {
                return $"({p.X:F2}, {p.Y:F2})";
            }

            private string FormatRect(Rect r)
            {
                return $"({r.X:F2}, {r.Y:F2}, {r.Width:F2}, {r.Height:F2})";
            }

            public void PushTransform(Matrix transform)
            {
                var current = _transforms.Peek();
                var accumulated = current * transform;
                _transforms.Push(accumulated);

                Operations.Add($"[{_operationIndex++}] PushTransform: {FormatMatrix(transform)}");
                Operations.Add($"     Accumulated: {FormatMatrix(accumulated)}");
            }

            public void PopTransform()
            {
                if (_transforms.Count > 1)
                {
                    _transforms.Pop();
                    Operations.Add($"[{_operationIndex++}] PopTransform");
                }
            }

            public void PushLayer(CompositeMode mode)
            {
                Operations.Add($"[{_operationIndex++}] PushLayer: Mode={mode}");
            }

            public void PopLayer()
            {
                Operations.Add($"[{_operationIndex++}] PopLayer");
            }

            public void PushClip(Rect clipBox)
            {
                Operations.Add($"[{_operationIndex++}] PushClip: {FormatRect(clipBox)}");
            }

            public void PopClip()
            {
                Operations.Add($"[{_operationIndex++}] PopClip");
            }

            public void FillSolid(Color color)
            {
                Operations.Add($"[{_operationIndex++}] FillSolid: Color=#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}");
            }

            public void FillLinearGradient(Point p0, Point p1, GradientStop[] stops, GradientSpreadMethod extend)
            {
                Operations.Add($"[{_operationIndex++}] FillLinearGradient: P0={FormatPoint(p0)}, P1={FormatPoint(p1)}, Stops={stops.Length}, Extend={extend}");
            }

            public void FillRadialGradient(Point c0, double r0, Point c1, double r1, GradientStop[] stops, GradientSpreadMethod extend)
            {
                Operations.Add($"[{_operationIndex++}] FillRadialGradient: C0={FormatPoint(c0)}, R0={r0:F2}, C1={FormatPoint(c1)}, R1={r1:F2}, Stops={stops.Length}, Extend={extend}");
            }

            public void FillConicGradient(Point center, double startAngle, double endAngle, GradientStop[] stops, GradientSpreadMethod extend)
            {
                Operations.Add($"[{_operationIndex++}] FillConicGradient: Center={FormatPoint(center)}, Start={startAngle:F2}°, End={endAngle:F2}°, Stops={stops.Length}, Extend={extend}");
            }

            public void Glyph(ushort glyphId)
            {
                var geometry = _glyphTypeface.GetGlyphOutline(glyphId, Matrix.Identity);
                var bounds = geometry?.Bounds ?? default;
                var transform = _transforms.Peek();
                var transformedBounds = bounds.TransformToAABB(transform);

                Operations.Add($"[{_operationIndex++}] Glyph: GlyphID={glyphId}");
                Operations.Add($"     Original Bounds: {FormatRect(bounds)}");
                Operations.Add($"     Current Transform: {FormatMatrix(transform)}");
                Operations.Add($"     Transformed Bounds: {FormatRect(transformedBounds)}");
            }

            public void ColrGlyph(ushort glyphId)
            {
                Operations.Add($"[{_operationIndex++}] ColrGlyph: GlyphID={glyphId}");
            }
        }

        /// <summary>
        /// Custom painter that tracks transform accumulation during paint graph traversal
        /// </summary>
        private class TransformTrackingPainter : IColorPainter
        {
            public List<Matrix> TransformStack { get; } = new List<Matrix>();
            public List<Matrix> AllTransforms { get; } = new List<Matrix>();
            public int MaxStackDepth { get; private set; }
            public int GlyphCount { get; private set; }
            public int FillCount { get; private set; }
            public int ClipCount { get; private set; }
            public int LayerCount { get; private set; }

            private readonly Stack<Matrix> _activeTransforms = new Stack<Matrix>();

            public TransformTrackingPainter()
            {
                _activeTransforms.Push(Matrix.Identity);
                TransformStack.Add(Matrix.Identity);
            }

            public void PushTransform(Matrix transform)
            {
                var current = _activeTransforms.Peek();
                var accumulated = current * transform;
                _activeTransforms.Push(accumulated);
                TransformStack.Add(accumulated);
                AllTransforms.Add(transform);

                MaxStackDepth = Math.Max(MaxStackDepth, _activeTransforms.Count);
            }

            public void PopTransform()
            {
                if (_activeTransforms.Count > 1)
                {
                    _activeTransforms.Pop();
                }
            }

            public void PushLayer(CompositeMode mode)
            {
                LayerCount++;
            }

            public void PopLayer()
            {
            }

            public void PushClip(Rect clipBox)
            {
                ClipCount++;
            }

            public void PopClip()
            {
            }

            public void FillSolid(Color color)
            {
                FillCount++;
            }

            public void FillLinearGradient(Point p0, Point p1, GradientStop[] stops, GradientSpreadMethod extend)
            {
                FillCount++;
            }

            public void FillRadialGradient(Point c0, double r0, Point c1, double r1, GradientStop[] stops, GradientSpreadMethod extend)
            {
                FillCount++;
            }

            public void FillConicGradient(Point center, double startAngle, double endAngle, GradientStop[] stops, GradientSpreadMethod extend)
            {
                FillCount++;
            }

            public void Glyph(ushort glyphId)
            {
                GlyphCount++;
            }

            public void ColrGlyph(ushort glyphId)
            {
                GlyphCount++;
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

            public void Glyph(ushort glyphId)
            {
                GlyphCount++;

                // Get glyph outline bounds
                var geometry = _glyphTypeface.GetGlyphOutline(glyphId, Matrix.Identity);
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

            public void ColrGlyph(ushort glyphId)
            {
                GlyphCount++;
            }
        }

        private static IDisposable Start()
        {
            var disposable = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            return disposable;
        }
    }
}
