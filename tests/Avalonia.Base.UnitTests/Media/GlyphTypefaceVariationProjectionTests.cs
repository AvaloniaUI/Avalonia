using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    /// <summary>
    /// A variation clone reports the Weight / Style / Stretch of the face it draws, derived from
    /// its wght, ital / slnt and wdth coordinates, so font matching can tell a wght=700 clone
    /// from the default instance.
    /// </summary>
    public class GlyphTypefaceVariationProjectionTests
    {
        [Fact]
        public void Default_Position_Reports_The_Static_Values()
        {
            var gt = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).CreateGlyphTypeface();

            var same = gt.WithVariations(FontVariationSettings.Parse("wght=400"));

            Assert.Same(gt, same);
            Assert.Equal(FontWeight.Normal, same.Weight);
            Assert.Equal(FontStyle.Normal, same.Style);
            Assert.Equal(FontStretch.Normal, same.Stretch);
        }

        [Theory]
        [InlineData(700, FontWeight.Bold)]
        [InlineData(600, FontWeight.SemiBold)]
        [InlineData(100, FontWeight.Thin)]
        [InlineData(900, FontWeight.Black)]
        [InlineData(450, (FontWeight)450)]
        public void Weight_Axis_Projects_Onto_Weight(double wght, FontWeight expected)
        {
            // Inter's avar bends the wght axis, so this also covers the avar inverse.
            var gt = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).CreateGlyphTypeface();

            var varied = gt.WithVariations(new FontVariationSettings(
                new[] { new FontVariation(OpenTypeTag.Parse("wght"), wght) }));

            Assert.Equal(expected, varied.Weight);
            Assert.Equal(FontStyle.Normal, varied.Style);
            Assert.Equal(FontStretch.Normal, varied.Stretch);
        }

        [Fact]
        public void Axis_The_Font_Lacks_Keeps_The_Static_Value()
        {
            // Inter varies opsz and wght only: moving opsz changes nothing font matching sees.
            var gt = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).CreateGlyphTypeface();

            var varied = gt.WithVariations(FontVariationSettings.Parse("opsz=32"));

            Assert.NotSame(gt, varied);
            Assert.Equal(FontWeight.Normal, varied.Weight);
            Assert.Equal(FontStyle.Normal, varied.Style);
            Assert.Equal(FontStretch.Normal, varied.Stretch);
        }

        [Fact]
        public void Clone_Key_Reflects_The_Projection()
        {
            var gt = SyntheticFont.FromAsset(SyntheticFont.Assets.InterVariable).CreateGlyphTypeface();

            var varied = gt.WithVariations(FontVariationSettings.Parse("wght=700"));
            var key = varied.ToFontCollectionKey();

            Assert.Equal(FontWeight.Bold, key.Weight);

            // The variation stays part of the key, so the clone never collides with a static
            // Bold face registered for the same family.
            Assert.NotEqual(new FontCollectionKey(FontStyle.Normal, FontWeight.Bold, FontStretch.Normal), key);
            Assert.Equal(varied.VariationPosition, key.Variation);
        }

        [Theory]
        [InlineData(50, FontStretch.UltraCondensed)]
        [InlineData(75, FontStretch.Condensed)]
        [InlineData(90, FontStretch.SemiCondensed)]
        [InlineData(125, FontStretch.Expanded)]
        [InlineData(200, FontStretch.UltraExpanded)]
        public void Width_Axis_Projects_Onto_The_Nearest_Stretch(double wdth, FontStretch expected)
        {
            var gt = CreateWidthSlantItalicFont();

            var varied = gt.WithVariations(new FontVariationSettings(
                new[] { new FontVariation(OpenTypeTag.Parse("wdth"), wdth) }));

            Assert.Equal(expected, varied.Stretch);
        }

        [Fact]
        public void Italic_Axis_Projects_Onto_Italic()
        {
            var gt = CreateWidthSlantItalicFont();

            Assert.Equal(FontStyle.Italic, gt.WithVariations(FontVariationSettings.Parse("ital=1")).Style);
            Assert.Equal(FontStyle.Normal, gt.WithVariations(FontVariationSettings.Parse("ital=0.25")).Style);
        }

        [Fact]
        public void Negative_Slant_Projects_Onto_Oblique()
        {
            var gt = CreateWidthSlantItalicFont();

            Assert.Equal(FontStyle.Oblique, gt.WithVariations(FontVariationSettings.Parse("slnt=-10")).Style);
        }

        [Fact]
        public void Italic_Axis_Wins_Over_Slant()
        {
            var gt = CreateWidthSlantItalicFont();

            var varied = gt.WithVariations(FontVariationSettings.Parse("ital=1, slnt=-10"));

            Assert.Equal(FontStyle.Italic, varied.Style);
        }

        /// <summary>
        /// Static Inter Regular with an fvar declaring wdth 50..200, ital 0..1 and slnt -15..0,
        /// all defaulting to the upright normal-width design. No other variation tables are
        /// present, so only the declared axes and their projection are exercised.
        /// </summary>
        private static GlyphTypeface CreateWidthSlantItalicFont()
        {
            var fvar = new BigEndianBuffer();

            fvar.UInt16(1);   // majorVersion
            fvar.UInt16(0);   // minorVersion
            fvar.UInt16(16);  // axesArrayOffset
            fvar.UInt16(2);   // reserved
            fvar.UInt16(3);   // axisCount
            fvar.UInt16(20);  // axisSize
            fvar.UInt16(0);   // instanceCount
            fvar.UInt16(0);   // instanceSize

            WriteAxis(fvar, "wdth", 50, 100, 200);
            WriteAxis(fvar, "ital", 0, 0, 1);
            WriteAxis(fvar, "slnt", -15, 0, 0);

            return SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular)
                .Replace("fvar", fvar.ToArray())
                .CreateGlyphTypeface();

            static void WriteAxis(BigEndianBuffer buffer, string tag, double min, double def, double max)
            {
                buffer.Tag(tag);
                buffer.Fixed(min);
                buffer.Fixed(def);
                buffer.Fixed(max);
                buffer.UInt16(0); // flags
                buffer.UInt16(0); // axisNameID
            }
        }
    }
}
