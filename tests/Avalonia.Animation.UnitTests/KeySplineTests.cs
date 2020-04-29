using System;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Animation.UnitTests
{
    public class KeySplineTests
    {
        [Theory]
        [InlineData("1,2 3,4")]
        [InlineData("1 2 3 4")]
        [InlineData("1 2,3 4")]
        [InlineData("1,2,3,4")]
        public void Can_Parse_KeySpline_Via_TypeConverter(string input)
        {
            var conv = new KeySplineTypeConverter();

            var keySpline = (KeySpline)conv.ConvertFrom(input);

            Assert.Equal(1, keySpline.ControlPointX1);
            Assert.Equal(2, keySpline.ControlPointY1);
            Assert.Equal(3, keySpline.ControlPointX2);
            Assert.Equal(4, keySpline.ControlPointY2);
        }

        [Theory]
        [InlineData(0.00)]
        [InlineData(0.50)]
        [InlineData(1.00)]
        public void KeySpline_X_Values_In_Range_Do_Not_Throw(double input)
        {
            var keySpline = new KeySpline();
            keySpline.ControlPointX1 = input; // no exception will be thrown -- test will fail if exception thrown
            keySpline.ControlPointX2 = input; // no exception will be thrown -- test will fail if exception thrown
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(1.01)]
        public void KeySpline_X_Values_Cannot_Be_Out_Of_Range(double input)
        {
            var keySpline = new KeySpline();
            Assert.Throws<ArgumentException>(() => keySpline.ControlPointX1 = input);
            Assert.Throws<ArgumentException>(() => keySpline.ControlPointX2 = input);
        }
    }
}
