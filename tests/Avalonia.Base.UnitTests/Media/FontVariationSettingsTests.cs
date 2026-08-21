using System;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    /// <summary>
    /// The user-space variation settings value: parse round-trips, order-independent
    /// structural equality, last-wins duplicate handling, and the lookups — the contract
    /// that lets the type serve as a style value and a cache key.
    /// </summary>
    public class FontVariationSettingsTests
    {
        private static readonly OpenTypeTag s_wght = OpenTypeTag.Parse("wght");
        private static readonly OpenTypeTag s_wdth = OpenTypeTag.Parse("wdth");
        private static readonly OpenTypeTag s_opsz = OpenTypeTag.Parse("opsz");

        [Fact]
        public void Empty_Is_Empty_And_Round_Trips()
        {
            Assert.True(FontVariationSettings.Empty.IsEmpty);
            Assert.Empty(FontVariationSettings.Empty.Variations);
            Assert.Equal(string.Empty, FontVariationSettings.Empty.ToString());
            Assert.Equal(FontVariationSettings.Empty, FontVariationSettings.Parse(""));
            Assert.Equal(FontVariationSettings.Empty, FontVariationSettings.Parse("   "));
        }

        [Fact]
        public void Parse_Reads_Comma_Separated_Tag_Value_Pairs()
        {
            var settings = FontVariationSettings.Parse(" wght = 700 , wdth=85.5 ");

            Assert.Equal(2, settings.Variations.Length);
            Assert.True(settings.TryGetValue(s_wght, out var wght));
            Assert.Equal(700, wght);
            Assert.True(settings.TryGetValue(s_wdth, out var wdth));
            Assert.Equal(85.5, wdth);
        }

        [Theory]
        [InlineData("wght")]
        [InlineData("wght=")]
        [InlineData("=700")]
        [InlineData("weight=700")]
        [InlineData("wght=seven")]
        [InlineData("wght=NaN")]
        public void Parse_Rejects_Malformed_Input(string input)
        {
            Assert.Throws<FormatException>(() => FontVariationSettings.Parse(input));
        }

        [Fact]
        public void ToString_Round_Trips_Through_Parse()
        {
            var settings = FontVariationSettings.Parse("opsz=14.25,wght=650");
            var roundTripped = FontVariationSettings.Parse(settings.ToString());

            Assert.Equal(settings, roundTripped);
            Assert.Equal("opsz=14.25,wght=650", settings.ToString());
        }

        [Fact]
        public void Equality_Is_Order_Independent_With_Matching_Hashes()
        {
            var a = new FontVariationSettings(new[]
            {
                new FontVariation(s_wght, 700),
                new FontVariation(s_opsz, 36),
            });
            var b = new FontVariationSettings(new[]
            {
                new FontVariation(s_opsz, 36),
                new FontVariation(s_wght, 700),
            });

            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.NotEqual(a, FontVariationSettings.Parse("wght=700"));
            Assert.False(a.Equals(null));
        }

        [Fact]
        public void Duplicate_Tags_Collapse_To_The_Last_Value()
        {
            // CSS font-variation-settings behavior: the last occurrence wins.
            var settings = FontVariationSettings.Parse("wght=400,wght=700");

            Assert.Equal(1, settings.Variations.Length);
            Assert.True(settings.TryGetValue(s_wght, out var wght));
            Assert.Equal(700, wght);
        }

        [Fact]
        public void Variations_Are_Sorted_By_Tag()
        {
            var settings = FontVariationSettings.Parse("wght=700,opsz=14,wdth=85");

            Assert.Equal(s_opsz, settings.Variations[0].Tag);
            Assert.Equal(s_wdth, settings.Variations[1].Tag);
            Assert.Equal(s_wght, settings.Variations[2].Tag);
        }

        [Fact]
        public void Constructor_Rejects_Non_Finite_Values()
        {
            Assert.Throws<ArgumentException>(() =>
                new FontVariationSettings(new[] { new FontVariation(s_wght, double.NaN) }));
            Assert.Throws<ArgumentException>(() =>
                new FontVariationSettings(new[] { new FontVariation(s_wght, double.PositiveInfinity) }));
            Assert.Throws<ArgumentNullException>(() => new FontVariationSettings(null!));
        }

        [Fact]
        public void TryGetValue_Misses_Report_False_And_Zero()
        {
            var settings = FontVariationSettings.Parse("wght=700");

            Assert.False(settings.TryGetValue(s_opsz, out var value));
            Assert.Equal(0, value);
        }

        [Fact]
        public void Variation_ToString_Is_The_Pair_Form()
        {
            Assert.Equal("wght=700", new FontVariation(s_wght, 700).ToString());
            Assert.Equal("opsz=14.25", new FontVariation(s_opsz, 14.25).ToString());
        }
    }
}
