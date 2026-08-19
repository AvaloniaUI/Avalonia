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
        public void Constructor_Rejects_NaN_And_Null()
        {
            Assert.Throws<ArgumentException>(() =>
                new FontVariationSettings(new[] { new FontVariation(s_wght, double.NaN) }));
            Assert.Throws<ArgumentNullException>(() => new FontVariationSettings(null!));
        }

        [Fact]
        public void Infinite_Values_Are_Accepted_And_Round_Trip()
        {
            // Infinities clamp to the axis range when applied, like any out-of-range value.
            var settings = new FontVariationSettings(new[]
            {
                new FontVariation(s_wght, double.PositiveInfinity),
                new FontVariation(s_opsz, double.NegativeInfinity),
            });

            Assert.True(settings.TryGetValue(s_wght, out var wght));
            Assert.Equal(double.PositiveInfinity, wght);

            var roundTripped = FontVariationSettings.Parse(settings.ToString());

            Assert.Equal(settings, roundTripped);
            Assert.True(roundTripped.TryGetValue(s_opsz, out var opsz));
            Assert.Equal(double.NegativeInfinity, opsz);
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

        [Fact]
        public void Interpolate_Lerps_Matching_Axis_Sets_Per_Axis()
        {
            var from = FontVariationSettings.Parse("wght=400, wdth=75");
            var to = FontVariationSettings.Parse("wght=800, wdth=125");

            var quarter = FontVariationSettings.Interpolate(from, to, 0.25);

            Assert.NotNull(quarter);
            Assert.True(quarter.TryGetValue(s_wght, out var wght));
            Assert.Equal(500, wght);
            Assert.True(quarter.TryGetValue(s_wdth, out var wdth));
            Assert.Equal(87.5, wdth);

            Assert.Equal(from, FontVariationSettings.Interpolate(from, to, 0));
            Assert.Equal(to, FontVariationSettings.Interpolate(from, to, 1));
        }

        [Fact]
        public void Interpolate_Matches_Axis_Sets_Order_Independently()
        {
            // Both endpoints store sorted variations, so the authoring order of the
            // pairs must not force the discrete path.
            var from = FontVariationSettings.Parse("wdth=75, wght=400");
            var to = FontVariationSettings.Parse("wght=800, wdth=125");

            var mid = FontVariationSettings.Interpolate(from, to, 0.5);

            Assert.NotNull(mid);
            Assert.True(mid.TryGetValue(s_wght, out var wght));
            Assert.Equal(600, wght);
        }

        [Fact]
        public void Interpolate_Extrapolates_On_Easing_Overshoot()
        {
            // Springy easings report progress outside [0, 1]; matching axis sets keep
            // lerping through, like every other continuous animator.
            var from = FontVariationSettings.Parse("wght=400");
            var to = FontVariationSettings.Parse("wght=800");

            var overshot = FontVariationSettings.Interpolate(from, to, 1.25);

            Assert.NotNull(overshot);
            Assert.True(overshot.TryGetValue(s_wght, out var wght));
            Assert.Equal(900, wght);
        }

        [Fact]
        public void Interpolate_Snaps_Axes_With_Infinite_Endpoints()
        {
            // Lerping between an infinite and a finite endpoint is NaN arithmetic; such
            // axes snap to the nearer endpoint instead.
            var from = new FontVariationSettings(new[]
            {
                new FontVariation(s_wght, double.PositiveInfinity),
            });
            var to = FontVariationSettings.Parse("wght=800");

            var early = FontVariationSettings.Interpolate(from, to, 0.25);
            var late = FontVariationSettings.Interpolate(from, to, 0.75);

            Assert.NotNull(early);
            Assert.True(early.TryGetValue(s_wght, out var earlyWght));
            Assert.Equal(double.PositiveInfinity, earlyWght);

            Assert.NotNull(late);
            Assert.True(late.TryGetValue(s_wght, out var lateWght));
            Assert.Equal(800, lateWght);
        }

        [Fact]
        public void Interpolate_Is_Discrete_When_Axis_Sets_Differ()
        {
            // CSS font-variation-settings semantics: a missing axis means "the font's
            // default", which is unknowable in user space — so mismatched sets switch
            // at the midpoint instead of blending.
            var from = FontVariationSettings.Parse("wght=700");
            var toOtherAxis = FontVariationSettings.Parse("wdth=85");
            var toSuperset = FontVariationSettings.Parse("wght=700, wdth=85");

            Assert.Same(from, FontVariationSettings.Interpolate(from, toOtherAxis, 0.49));
            Assert.Same(toOtherAxis, FontVariationSettings.Interpolate(from, toOtherAxis, 0.5));

            Assert.Same(from, FontVariationSettings.Interpolate(from, toSuperset, 0.49));
            Assert.Same(toSuperset, FontVariationSettings.Interpolate(from, toSuperset, 0.5));
        }

        [Fact]
        public void Interpolate_Is_Discrete_Against_Null_And_Empty()
        {
            var settings = FontVariationSettings.Parse("wght=700");

            Assert.Null(FontVariationSettings.Interpolate(null, settings, 0.49));
            Assert.Same(settings, FontVariationSettings.Interpolate(null, settings, 0.5));

            Assert.Same(settings, FontVariationSettings.Interpolate(settings, null, 0.49));
            Assert.Null(FontVariationSettings.Interpolate(settings, null, 0.5));

            Assert.Null(FontVariationSettings.Interpolate(null, null, 0.5));
            Assert.Same(FontVariationSettings.Empty,
                FontVariationSettings.Interpolate(null, FontVariationSettings.Empty, 0.5));
        }
    }
}
