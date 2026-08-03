using System;
using Avalonia.Layout;
using Xunit;

namespace Avalonia.Base.UnitTests.Layout
{
    public class LayoutHelperTests
    {
        [Fact]
        public void Round_Layout_Value_Without_DPI_Aware()
        {
            const double value = 42.5;
            var expectedValue = Math.Round(value);
            var actualValue = LayoutHelper.RoundLayoutValue(value, 1.0);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void Round_Layout_Value_With_DPI_Aware()
        {
            const double dpiScale = 1.25;
            const double value = 42.5;
            var expectedValue = Math.Round(value * dpiScale) / dpiScale;
            var actualValue = LayoutHelper.RoundLayoutValue(value, dpiScale);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void ValidateScaling_Returns_Exact_One_For_Approximate_One()
        {
            var result = LayoutHelper.ValidateScaling(1.000000000000001);
            Assert.Equal(1.0, result);
        }

        [Fact]
        public void ValidateScaling_Returns_Valid_Scaling_Value()
        {
            const double scaling = 1.5;
            var result = LayoutHelper.ValidateScaling(scaling);
            Assert.Equal(scaling, result);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-1.5)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void ValidateScaling_Throws_For_Invalid_Values(double scaling)
        {
            Assert.Throws<InvalidOperationException>(() => LayoutHelper.ValidateScaling(scaling));
        }
    }
}
