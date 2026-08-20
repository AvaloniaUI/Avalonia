using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class FlexBasisTests : ScopedTestBase
    {
        [Fact]
        public void Parse_Should_Parse_Auto()
        {
            var result = FlexBasis.Parse("Auto");

            Assert.Equal(FlexBasis.Auto, result);
        }

        [Fact]
        public void Parse_Should_Parse_Auto_Lowercase()
        {
            var result = FlexBasis.Parse("auto");

            Assert.Equal(FlexBasis.Auto, result);
        }

        [Fact]
        public void Parse_Should_Parse_Percentage()
        {
            var result = FlexBasis.Parse("50%");

            Assert.Equal(new FlexBasis(0.5, FlexBasisKind.Relative), result);
        }

        [Fact]
        public void Parse_Should_Parse_Absolute_Value()
        {
            var result = FlexBasis.Parse("2");

            Assert.Equal(new FlexBasis(2, FlexBasisKind.Absolute), result);
        }

        [Fact]
        public void Parse_Should_Throw_ArgumentException_For_Invalid_String()
        {
            Assert.Throws<ArgumentException>(() => FlexBasis.Parse("2x"));
        }

        [Fact]
        public async Task ToString_AllCulture_Absolute_Should_Pass()
        {
            List<CultureInfo> cultureInfos = CultureInfo.GetCultures(CultureTypes.AllCultures).ToList();
            var length = new FlexBasis(1.2d, FlexBasisKind.Absolute);

            foreach (var culture in cultureInfos)
            {
                await Task.Run(() =>
                {
                    CultureInfo.CurrentCulture = culture;
                    Assert.Equal("1.2", length.ToString());
                }, TestContext.Current.CancellationToken);
            }
        }

        [Fact]
        public async Task ToString_AllCulture_Relative_Should_Pass()
        {
            List<CultureInfo> cultureInfos = CultureInfo.GetCultures(CultureTypes.AllCultures).ToList();
            var length = new FlexBasis(0.012d, FlexBasisKind.Relative); // 1.2%

            foreach (var culture in cultureInfos)
            {
                await Task.Run(() =>
                {
                    CultureInfo.CurrentCulture = culture;
                    Assert.Equal("1.2%", length.ToString());
                }, TestContext.Current.CancellationToken);
            }
        }

        [Fact]
        public async Task ToString_AllCulture_Auto_Should_Pass()
        {
            List<CultureInfo> cultureInfos = CultureInfo.GetCultures(CultureTypes.AllCultures).ToList();
            var length = FlexBasis.Auto;

            foreach (var culture in cultureInfos)
            {
                await Task.Run(() =>
                {
                    CultureInfo.CurrentCulture = culture;
                    Assert.Equal("Auto", length.ToString());
                }, TestContext.Current.CancellationToken);
            }
        }
    }
}
