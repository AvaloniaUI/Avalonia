using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Fonts;
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
        public async Task Should_Render_InterVariable_Glyph_At_wght_900()
        {
            // gvar deformation applied to the 'R' at wght=900 (Black). The baseline
            // (Should_Render_InterVariable_Glyph_At_wght_900.expected.png) captures
            // the deformed outline produced by GvarTable + IUP.
            var gt = LoadGlyphTypeface(InterVariableAsset);
            var black = gt.WithVariation(WghtSettings(gt, 900f));

            await RenderToFile(BuildTarget(black, 'R'));
            CompareImages();
        }

        [Fact]
        public async Task Should_Render_InterVariable_Composite_At_wght_900()
        {
            // gvar applies to each component of a composite glyph independently.
            // 'Á' = base 'A' + acute accent; at wght=900 both components are visibly
            // thicker. Composite-level offset deformation is deferred (the accent's
            // position relative to the base stays at the designer's default).
            var gt = LoadGlyphTypeface(InterVariableAsset);
            var black = gt.WithVariation(WghtSettings(gt, 900f));

            await RenderToFile(BuildTarget(black, 'Á'));
            CompareImages();
        }

        [Fact]
        public void Varied_Outline_Differs_From_Default_Instance_Outline()
        {
            // Behavioral: a wght=900 typeface produces a different geometry than the
            // default instance. Asserting on the geometry's bounds is enough to prove
            // the deformation pipeline ran without depending on platform-specific
            // pixel output.
            var gt = LoadGlyphTypeface(InterVariableAsset);
            var black = gt.WithVariation(WghtSettings(gt, 900f));
            var glyph = gt.CharacterToGlyphMap['A'];

            var regularBounds = gt.GetGlyphOutline(glyph)!.Bounds;
            var blackBounds = black.GetGlyphOutline(glyph)!.Bounds;

            Assert.NotEqual(regularBounds, blackBounds);
        }

        [Fact]
        public void Intermediate_Weight_Lands_Between_Regular_And_Black()
        {
            // wght=700 is between regular (400) and black (900). The tuple scalers in
            // gvar are linear ramps so the intermediate weight produces an intermediate
            // outline — width grows monotonically across weights.
            var gt = LoadGlyphTypeface(InterVariableAsset);
            var semiBold = gt.WithVariation(WghtSettings(gt, 700f));
            var black = gt.WithVariation(WghtSettings(gt, 900f));
            var glyph = gt.CharacterToGlyphMap['A'];

            var regularBounds = gt.GetGlyphOutline(glyph)!.Bounds;
            var semiBoldBounds = semiBold.GetGlyphOutline(glyph)!.Bounds;
            var blackBounds = black.GetGlyphOutline(glyph)!.Bounds;

            Assert.True(semiBoldBounds.Width >= regularBounds.Width,
                $"semiBold {semiBoldBounds.Width} should be >= regular {regularBounds.Width}");
            Assert.True(blackBounds.Width >= semiBoldBounds.Width,
                $"black {blackBounds.Width} should be >= semiBold {semiBoldBounds.Width}");
        }

        private static FontVariationSettings WghtSettings(GlyphTypeface gt, float weight)
            => gt.CreateVariationSettings(new Dictionary<OpenTypeTag, float>
            {
                [OpenTypeTag.Parse("wght")] = weight
            });

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
