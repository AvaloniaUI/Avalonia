using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class GridLengthTests
    {
        [Fact]
        public void Parse_Should_Parse_Auto()
        {
            var result = GridLength.Parse("Auto");

            Assert.Equal(GridLength.Auto, result);
        }

        [Fact]
        public void Parse_Should_Parse_Auto_Lowercase()
        {
            var result = GridLength.Parse("auto");

            Assert.Equal(GridLength.Auto, result);
        }

        [Fact]
        public void Parse_Should_Parse_Star()
        {
            var result = GridLength.Parse("*");

            Assert.Equal(new GridLength(1, GridUnitType.Star), result);
        }

        [Fact]
        public void Parse_Should_Parse_Star_Value()
        {
            var result = GridLength.Parse("2*");

            Assert.Equal(new GridLength(2, GridUnitType.Star), result);
        }

        [Fact]
        public void Parse_Should_Parse_Pixel_Value()
        {
            var result = GridLength.Parse("2");

            Assert.Equal(new GridLength(2, GridUnitType.Pixel), result);
        }

        [Fact]
        public void Parse_Should_Throw_FormatException_For_Invalid_String()
        {
            Assert.Throws<FormatException>(() => GridLength.Parse("2x"));
        }

        [Fact]
        public void ParseLengths_Accepts_Comma_Separators()
        {
            var result = GridLength.ParseLengths("*,Auto,2*,4").ToList();

            Assert.Equal(
                new[]
                {
                    new GridLength(1, GridUnitType.Star),
                    GridLength.Auto,
                    new GridLength(2, GridUnitType.Star),
                    new GridLength(4, GridUnitType.Pixel),
                },
                result);
        }

        [Fact]
        public void ParseLengths_Accepts_Space_Separators()
        {
            var result = GridLength.ParseLengths("* Auto 2* 4").ToList();

            Assert.Equal(
                new[]
                {
                    new GridLength(1, GridUnitType.Star),
                    GridLength.Auto,
                    new GridLength(2, GridUnitType.Star),
                    new GridLength(4, GridUnitType.Pixel),
                },
                result);
        }

        [Fact]
        public void ParseLengths_Accepts_Comma_Separators_With_Spaces()
        {
            var result = GridLength.ParseLengths("*, Auto, 2* ,4").ToList();

            Assert.Equal(
                new[]
                {
                    new GridLength(1, GridUnitType.Star),
                    GridLength.Auto,
                    new GridLength(2, GridUnitType.Star),
                    new GridLength(4, GridUnitType.Pixel),
                },
                result);
        }

        [Theory]
        [InlineData(1.2d, GridUnitType.Pixel, "1.2")]
        [InlineData(1.2d, GridUnitType.Star, "1.2*")]
        [InlineData(1.2d, GridUnitType.Auto, "Auto")]
        public async void ToString_AllCulture_Should_Pass(double d, GridUnitType type, string result)
        {
            List<CultureInfo> cultureInfos = CultureInfo.GetCultures(CultureTypes.AllCultures).ToList();
            GridLength length = new GridLength(d, type);
            foreach(var culture in cultureInfos)
            {
                await Task.Run(() =>
                {
                    CultureInfo.CurrentCulture = culture;
                    Assert.Equal(result, length.ToString());
                });
            }
        }
    }
}
