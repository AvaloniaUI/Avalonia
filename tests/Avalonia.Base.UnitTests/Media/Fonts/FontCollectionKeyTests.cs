using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts
{
    public class FontCollectionKeyTests
    {
        [Fact]
        public void Equal_Keys_Compare_Equal()
        {
            var a = new FontCollectionKey(FontStyle.Italic, FontWeight.Bold, FontStretch.Condensed);
            var b = new FontCollectionKey(FontStyle.Italic, FontWeight.Bold, FontStretch.Condensed);

            Assert.Equal(0, a.CompareTo(b));
            Assert.False(a < b);
            Assert.False(a > b);
            Assert.True(a <= b);
            Assert.True(a >= b);
        }

        [Fact]
        public void Style_Is_The_Primary_Sort_Key()
        {
            // Normal style, max weight/stretch
            var normal = new FontCollectionKey(FontStyle.Normal, FontWeight.Black, FontStretch.UltraExpanded);
            // Italic style, min weight/stretch
            var italic = new FontCollectionKey(FontStyle.Italic, FontWeight.Thin, FontStretch.UltraCondensed);

            Assert.True(normal.CompareTo(italic) < 0);
            Assert.True(italic.CompareTo(normal) > 0);
            Assert.True(normal < italic);
        }

        [Fact]
        public void Weight_Is_The_Secondary_Sort_Key_When_Style_Matches()
        {
            var light = new FontCollectionKey(FontStyle.Normal, FontWeight.Light, FontStretch.UltraExpanded);
            var bold = new FontCollectionKey(FontStyle.Normal, FontWeight.Bold, FontStretch.UltraCondensed);

            Assert.True(light.CompareTo(bold) < 0);
            Assert.True(light < bold);
        }

        [Fact]
        public void Stretch_Is_The_Tertiary_Sort_Key_When_Style_And_Weight_Match()
        {
            var condensed = new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Condensed);
            var expanded = new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Expanded);

            Assert.True(condensed.CompareTo(expanded) < 0);
            Assert.True(condensed < expanded);
        }

        [Fact]
        public void Array_Sort_Produces_Lexicographic_Order()
        {
            var keys = new[]
            {
                new FontCollectionKey(FontStyle.Italic, FontWeight.Normal, FontStretch.Normal),
                new FontCollectionKey(FontStyle.Normal, FontWeight.Bold, FontStretch.Normal),
                new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Expanded),
                new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Normal),
            };

            Array.Sort(keys);

            Assert.Equal(
                new[]
                {
                    new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Normal),
                    new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Expanded),
                    new FontCollectionKey(FontStyle.Normal, FontWeight.Bold, FontStretch.Normal),
                    new FontCollectionKey(FontStyle.Italic, FontWeight.Normal, FontStretch.Normal),
                },
                keys);
        }

        [Fact]
        public void Non_Generic_CompareTo_Treats_Null_As_Smaller()
        {
            var key = new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Normal);

            Assert.Equal(1, key.CompareTo((object?)null));
        }

        [Fact]
        public void Non_Generic_CompareTo_Throws_For_Wrong_Type()
        {
            var key = new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Normal);

            Assert.Throws<ArgumentException>(() => key.CompareTo((object)"not a key"));
        }

        [Fact]
        public void CompareTo_Is_Consistent_With_Equality()
        {
            var a = new FontCollectionKey(FontStyle.Oblique, FontWeight.Medium, FontStretch.SemiExpanded);
            var b = new FontCollectionKey(FontStyle.Oblique, FontWeight.Medium, FontStretch.SemiExpanded);
            var c = new FontCollectionKey(FontStyle.Oblique, FontWeight.Medium, FontStretch.SemiCondensed);

            Assert.True(a.Equals(b));
            Assert.Equal(0, a.CompareTo(b));

            Assert.False(a.Equals(c));
            Assert.NotEqual(0, a.CompareTo(c));
        }

        [Fact]
        public void CompareTo_Includes_Variation()
        {
            // The synthesized record equality includes Variation; the hand-written CompareTo
            // must agree, or keys differing only in variation would sort as duplicates.
            var wght = OpenTypeTag.Parse("wght");
            var baseKey = new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Normal);
            var varied = baseKey with
            {
                Variation = FontVariationSettings.FromCoordinates([new FontVariationCoordinate(wght, 0.5f)]),
            };
            var variedSame = baseKey with
            {
                Variation = FontVariationSettings.FromCoordinates([new FontVariationCoordinate(wght, 0.5f)]),
            };
            var variedOther = baseKey with
            {
                Variation = FontVariationSettings.FromCoordinates([new FontVariationCoordinate(wght, -0.5f)]),
            };

            Assert.NotEqual(0, baseKey.CompareTo(varied));
            Assert.Equal(0, varied.CompareTo(variedSame));
            Assert.NotEqual(0, varied.CompareTo(variedOther));

            // Antisymmetry of the variation leg.
            Assert.Equal(-Math.Sign(varied.CompareTo(variedOther)), Math.Sign(variedOther.CompareTo(varied)));
        }
    }
}
