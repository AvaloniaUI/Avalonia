using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    /// <summary>
    /// glyf ink boxes follow the outline, which is placed so that its left phantom point, at
    /// xMin - lsb plus its gvar delta when the font has no HVAR, lands on the origin: an hmtx left
    /// side bearing that differs from xMin, or a gvar delta on the left phantom point, moves the
    /// box, on the single and the batch metrics paths alike.
    /// </summary>
    public class GlyphTypefaceLeftPhantomBoundsTests
    {
        private static readonly OpenTypeTag s_wght = OpenTypeTag.Parse("wght");

        [Fact]
        public void Bounds_Are_Shifted_By_A_Left_Side_Bearing_That_Differs_From_xMin()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var original = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular);
            var glyph = original.CreateGlyphTypeface().CharacterToGlyphMap['H'];

            var shifted = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).AddToLeftSideBearing(glyph, 40);

            AssertShiftedBy(40, original.CreateGlyphTypeface(), shifted.CreateGlyphTypeface(), glyph);
        }

        [Fact]
        public void Composite_Bounds_Are_Shifted_By_Its_Use_My_Metrics_Component_Left_Phantom_Point()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var original = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular);
            var map = original.CreateGlyphTypeface().CharacterToGlyphMap;

            var shifted = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).AddToLeftSideBearing(map['A'], 40);

            AssertShiftedBy(40, original.CreateGlyphTypeface(), shifted.CreateGlyphTypeface(), map['Á']);
        }

        [Fact]
        public void Composite_Bounds_Ignore_Its_Own_Left_Side_Bearing_When_A_Component_Supplies_The_Metrics()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var original = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular);
            var map = original.CreateGlyphTypeface().CharacterToGlyphMap;

            var shifted = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).AddToLeftSideBearing(map['Á'], 40);

            AssertShiftedBy(0, original.CreateGlyphTypeface(), shifted.CreateGlyphTypeface(), map['Á']);
        }

        [Fact]
        public void Varied_Bounds_Are_Shifted_By_The_gvar_Delta_Of_Their_Left_Phantom_Point()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).Remove("HVAR");
            var glyph = font.CreateGlyphTypeface().CharacterToGlyphMap['H'];

            var typeface = font.WithLeftPhantomGvar(glyph, axis: 1, delta: -30).CreateGlyphTypeface();

            AssertInkBoxShiftedBy(30, typeface, AtWeight900(typeface), glyph);
        }

        [Fact]
        public void Left_Phantom_gvar_Delta_Is_Ignored_When_The_Font_Has_Hvar()
        {
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

            // With HVAR, FreeType drops the gvar deltas of the horizontal phantom points.
            var font = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable);
            var glyph = font.CreateGlyphTypeface().CharacterToGlyphMap['H'];

            var typeface = font.WithLeftPhantomGvar(glyph, axis: 1, delta: -30).CreateGlyphTypeface();

            AssertInkBoxShiftedBy(0, typeface, AtWeight900(typeface), glyph);
        }

        private static GlyphTypeface AtWeight900(GlyphTypeface typeface)
            => typeface.WithVariation(typeface.CreateNormalizedPosition(new FontVariationSettings(
                new[] { new FontVariation(s_wght, 900) })));

        private static void AssertInkBoxShiftedBy(int expected, GlyphTypeface original, GlyphTypeface shifted, ushort glyph)
        {
            var originalBox = InkBox(original, glyph);
            var shiftedBox = InkBox(shifted, glyph);

            Assert.Equal(
                originalBox with { XMin = (short)(originalBox.XMin + expected), XMax = (short)(originalBox.XMax + expected) },
                shiftedBox);

            // Every metrics path reports the same box.
            Assert.True(shifted.TryGetGlyphMetrics(glyph, out var metrics));
            Assert.Equal(shiftedBox.XMin, metrics.XBearing);
            Assert.Equal(shiftedBox.YMax, metrics.YBearing);

            var batchMetrics = new GlyphMetrics[1];
            Assert.True(shifted.TryGetGlyphMetrics(new[] { glyph }, batchMetrics));
            Assert.Equal(shiftedBox.XMin, batchMetrics[0].XBearing);

            var batchBounds = new GlyphBounds[1];
            Assert.True(shifted.TryGetGlyphBounds(new[] { glyph }, batchBounds));
            Assert.Equal(shiftedBox, batchBounds[0]);
        }

        private static GlyphBounds InkBox(GlyphTypeface typeface, ushort glyph)
        {
            Assert.True(typeface.TryGetGlyphInkBounds(glyph, out var box));
            return box;
        }

        private static void AssertShiftedBy(int expected, GlyphTypeface original, GlyphTypeface shifted, ushort glyph)
        {
            Assert.True(original.TryGetGlyphMetrics(glyph, out var originalMetrics));
            Assert.True(shifted.TryGetGlyphMetrics(glyph, out var metrics));

            Assert.Equal(originalMetrics with { XBearing = (short)(originalMetrics.XBearing + expected) }, metrics);

            // The batch paths report the same box.
            var batchMetrics = new GlyphMetrics[1];
            Assert.True(shifted.TryGetGlyphMetrics(new[] { glyph }, batchMetrics));
            Assert.Equal(metrics, batchMetrics[0]);

            var batchBounds = new GlyphBounds[1];
            Assert.True(shifted.TryGetGlyphBounds(new[] { glyph }, batchBounds));
            Assert.Equal(
                new GlyphBounds(
                    (short)metrics.XBearing,
                    (short)(metrics.YBearing - metrics.Height),
                    (short)(metrics.XBearing + metrics.Width),
                    (short)metrics.YBearing),
                batchBounds[0]);
        }
    }
}
