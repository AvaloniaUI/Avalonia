using System;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Utilities
{
    public class MathUtilitiesTests
    {
        [Fact]
        public void Two_Equivalent_Double_Values_Are_Close()
        {
            const int N = 10;
            var x = 42.42;
            var y = 0.0;
            var dx = x / N;

            for (var i = 0; i < N; ++i)
                y += dx;
            var actual = MathUtilities.AreClose(x, y);

            Assert.True(actual);
            Assert.Equal(x, Math.Round(y, 14));
        }

        [Fact]
        public void Calculated_Double_One_Is_One()
        {
            const int N = 10;
            var dx = 1.0 / N;
            var x = 0.0;
            
            for (var i = 0; i < N; ++i)
                x += dx;
            var actual = MathUtilities.IsOne(x);

            Assert.True(actual);
            Assert.Equal(1.0, Math.Round(x, 15));
        }

        [Fact]
        public void Calculated_Double_Zero_Is_Zero()
        {
            const int N = 10;
            var x = 1.0;
            var dx = x / N;
            
            for (var i = 0; i < N; ++i)
                x -= dx;
            var actual = MathUtilities.IsZero(x);

            Assert.True(actual);
            Assert.Equal(0.0, Math.Round(x, 15));
        }

        [Fact]
        public void Float_Clamp_Input_NaN_Return_NaN()
        {
            var clamp = MathUtilities.Clamp(double.NaN, 0.0, 1.0);
            Assert.True(double.IsNaN(clamp));
        }

        [Fact]
        public void Float_Clamp_Input_NegativeInfinity_Return_Min()
        {
            const double min = 0.0;
            const double max = 1.0;
            var actual = MathUtilities.Clamp(double.NegativeInfinity, min, max);
            Assert.Equal(min, actual);
        }

        [Fact]
        public void Float_Clamp_Input_PositiveInfinity_Return_Max()
        {
            const double min = 0.0;
            const double max = 1.0;
            var actual = MathUtilities.Clamp(double.PositiveInfinity, min, max);
            Assert.Equal(max, actual);
        }

        [Fact]
        public void Zero_Less_Than_One()
        {
            var actual = MathUtilities.LessThan(0, 1);
            Assert.True(actual);
        }

        [Fact]
        public void One_Not_Less_Than_Zero()
        {
            var actual = MathUtilities.LessThan(1, 0);
            Assert.False(actual);
        }

        [Fact]
        public void Zero_Not_Greater_Than_One()
        {
            var actual = MathUtilities.GreaterThan(0, 1);
            Assert.False(actual);
        }

        [Fact]
        public void One_Greater_Than_Zero()
        {
            var actual = MathUtilities.GreaterThan(1, 0);
            Assert.True(actual);
        }

        [Fact]
        public void One_Less_Than_Or_Close_One()
        {
            var actual = MathUtilities.LessThanOrClose(1, 1);
            Assert.True(actual);
        }

        [Fact]
        public void One_Greater_Than_Or_Close_One()
        {
            var actual = MathUtilities.GreaterThanOrClose(1, 1);
            Assert.True(actual);
        }
    }
}
