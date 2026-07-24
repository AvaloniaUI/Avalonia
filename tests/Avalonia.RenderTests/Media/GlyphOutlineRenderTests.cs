using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.RenderTests
{
    public class GlyphOutlineRenderTests : TestBase
    {
        // Direct asset URIs (not via FontManager) so we can address Inter-Regular and
        // InterVariable separately even though they share the family name "Inter".
        private const string InterRegularAsset =
            "resm:Avalonia.Skia.RenderTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Skia.RenderTests";

        private const string InterVariableAsset =
            "resm:Avalonia.Skia.RenderTests.Assets.InterVariable.ttf?assembly=Avalonia.Skia.RenderTests";

        private const string MiSansAsset =
            "resm:Avalonia.Skia.RenderTests.Assets.MiSans-Normal.ttf?assembly=Avalonia.Skia.RenderTests";

        private const string AdobeBlankAsset =
            "resm:Avalonia.Skia.RenderTests.Assets.AdobeBlank2VF.ttf?assembly=Avalonia.Skia.RenderTests";

        // Purpose-built fixture (no production font encodes point matching): its 'P' glyph is a
        // composite assembled with ARGS_ARE_XY_VALUES clear, i.e. by point matching.
        private const string PointMatchAsset =
            "resm:Avalonia.Skia.RenderTests.Assets.PointMatch.ttf?assembly=Avalonia.Skia.RenderTests";

        public GlyphOutlineRenderTests()
            : base(@"Media\GlyphOutline")
        {
        }

        [Fact]
        public async Task Should_Render_Inter_Latin_Glyph()
        {
            await RenderToFile(BuildTarget(LoadGlyphTypeface(InterRegularAsset), 'A'));
            CompareImages();
        }

        [Fact]
        public async Task Should_Render_Inter_Composite_Glyph()
        {
            // Á = base 'A' + combining acute accent. Inter stores it as a composite glyph,
            // which exercises GlyfTable's recursive component path.
            await RenderToFile(BuildTarget(LoadGlyphTypeface(InterRegularAsset), 'Á'));
            CompareImages();
        }

        [Fact]
        public async Task Should_Render_PointMatched_Composite_Glyph()
        {
            // 'P' is a composite whose second component (a small square) is placed by point
            // matching — aligning its bottom-left point onto the top-right point of the base
            // rectangle — rather than by an x/y offset. This is the only way to exercise
            // GlyfTable's point-matching build path through the full Skia pipeline, since no
            // production font uses point matching. A broken implementation would drop the square
            // at the origin instead of the rectangle's corner, which the image diff would catch.
            await RenderToFile(BuildTarget(LoadGlyphTypeface(PointMatchAsset), 'P'));
            CompareImages();
        }

        [Fact]
        public async Task Should_Render_MiSans_CJK_Glyph()
        {
            // 中 — a CJK ideograph with many contours and counters.
            await RenderToFile(BuildTarget(LoadGlyphTypeface(MiSansAsset), '中'));
            CompareImages();
        }

        [Fact]
        public async Task Should_Render_InterVariable_At_Default()
        {
            // Variable font with fvar / gvar tables present. PR2 doesn't apply variation,
            // so this renders the default-instance outline — the baseline against which a
            // future variable-font render test will compare deformed outputs.
            await RenderToFile(BuildTarget(LoadGlyphTypeface(InterVariableAsset), 'R'));
            CompareImages();
        }

        [Fact]
        public void Blank_Variable_Font_Returns_Null_Outline()
        {
            // AdobeBlank2VF is a variable font whose glyphs are intentionally empty.
            // GetGlyphOutline must return null without throwing on a font with fvar but
            // no usable contour data.
            var gt = LoadGlyphTypeface(AdobeBlankAsset);
            var glyphId = gt.CharacterToGlyphMap.ContainsGlyph('A')
                ? gt.CharacterToGlyphMap['A']
                : (ushort)0;

            Assert.Null(gt.GetGlyphOutline(glyphId));
        }

        private static GlyphTypeface LoadGlyphTypeface(string assetUri)
        {
            var loader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();
            using var stream = loader.Open(new Uri(assetUri));
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            // Copy the bytes into Skia-owned memory so the typeface's table reads keep
            // working after the source stream is disposed. SKTypeface.FromStream's
            // lifetime story varies by font; SKData.CreateCopy is bulletproof.
            var skData = SKData.CreateCopy(memory.ToArray());
            var skTypeface = SKTypeface.FromData(skData)
                ?? throw new InvalidOperationException("SkiaSharp failed to load the font.");
            return new GlyphTypeface(new SkiaTypeface(skTypeface, FontSimulations.None));
        }

        private static Border BuildTarget(GlyphTypeface glyphTypeface, char ch)
            => new Border
            {
                Width = 240,
                Height = 240,
                Background = Brushes.White,
                Child = new GlyphOutlineControl(glyphTypeface, ch),
            };

        /// <summary>
        /// Renders a single glyph outline produced by
        /// <see cref="GlyphTypeface.GetGlyphOutline(ushort)"/>, filled in black.
        /// The hosting <see cref="Border"/> supplies the white background and the layout
        /// size; the control itself fills the area assigned by layout.
        /// </summary>
        public class GlyphOutlineControl : Control
        {
            private readonly IGeometryImpl? _outline;

            public GlyphOutlineControl(GlyphTypeface glyphTypeface, char ch)
            {
                const double emSize = 200;
                const double margin = 20;

                if (!glyphTypeface.CharacterToGlyphMap.ContainsGlyph(ch))
                {
                    return;
                }

                var glyphId = glyphTypeface.CharacterToGlyphMap[ch];
                var scale = emSize / glyphTypeface.Metrics.DesignEmHeight;

                // glyf is y-up; flip the y axis and translate so the baseline lands inside
                // the bitmap with a margin of `margin` on every side.
                var transform = Matrix.CreateScale(scale, -scale)
                              * Matrix.CreateTranslation(margin, emSize + margin);

                _outline = glyphTypeface.GetGlyphOutline(glyphId)?.WithTransform(transform);
            }

            public override void Render(DrawingContext context)
            {
                if (_outline != null)
                {
                    context.DrawGeometry(Brushes.Black, null, _outline);
                }
            }
        }
    }
}
