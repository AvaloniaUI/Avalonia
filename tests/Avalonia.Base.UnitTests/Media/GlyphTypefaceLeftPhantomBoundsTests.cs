using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    /// <summary>
    /// glyf header boxes follow the outline, which is placed so that its left phantom point, at
    /// xMin - lsb, lands on the origin: an hmtx left side bearing that differs from xMin moves the
    /// box, on the single and the batch metrics paths alike.
    /// </summary>
    public class GlyphTypefaceLeftPhantomBoundsTests
    {
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
