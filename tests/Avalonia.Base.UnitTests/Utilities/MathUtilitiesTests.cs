using System;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Utilities
{
    public class MathUtilitiesTests
    {
        private const double AnyValue = 42.42;
        private readonly double _calculatedAnyValue;
        private readonly double _one;
        private readonly double _zero;

        public MathUtilitiesTests()
        {
            _calculatedAnyValue = 0.0;
            _one = 0.0;
            _zero = 1.0;

            const int N = 10;
            var dxAny = AnyValue / N;
            var dxOne = 1.0 / N;
            var dxZero = _zero / N;

            for (var i = 0; i < N; ++i)
            {
                _calculatedAnyValue += dxAny;
                _one += dxOne;
                _zero -= dxZero;
            }
        }

        [Fact]
        public void Two_Equivalent_Double_Values_Are_Close()
        {
            var actual = MathUtilities.AreClose(AnyValue, _calculatedAnyValue);

            Assert.True(actual);
            Assert.Equal(AnyValue, Math.Round(_calculatedAnyValue, 14));
        }

        [Fact]
        public void Two_Equivalent_Single_Values_Are_Close()
        {
            var expectedValue = (float)AnyValue;
            var actualValue = (float)_calculatedAnyValue;
            
            var actual = MathUtilities.AreClose(expectedValue, actualValue);

            Assert.True(actual);
            Assert.Equal((float) Math.Round(expectedValue, 5), (float) Math.Round(actualValue, 4));
        }

        [Fact]
        public void Calculated_Double_One_Is_One()
        {
            var actual = MathUtilities.IsOne(_one);

            Assert.True(actual);
            Assert.Equal(1.0, Math.Round(_one, 15));
        }

        [Fact]
        public void Calculated_Single_One_Is_One()
        {
            var actualValue = (float)_one;
            
            var actual = MathUtilities.IsOne(actualValue);

            Assert.True(actual);
            Assert.Equal(1.0f, (float) Math.Round(actualValue, 7));
        }

        [Fact]
        public void Calculated_Double_Zero_Is_Zero()
        {
            var actual = MathUtilities.IsZero(_zero);

            Assert.True(actual);
            Assert.Equal(0.0, Math.Round(_zero, 15));
        }

        [Fact]
        public void Calculated_Single_Zero_Is_Zero()
        {
            var actualValue = (float)_zero;

            var actual = MathUtilities.IsZero(actualValue);

            Assert.True(actual);
            Assert.Equal(0.0f, (float) Math.Round(actualValue, 7));
        }

        [Fact]
        public void Clamp_Input_NaN_Return_NaN()
        {
            var clamp = MathUtilities.Clamp(double.NaN, 0.0, 1.0);
            Assert.True(double.IsNaN(clamp));
        }

        [Fact]
        public void Clamp_Input_NegativeInfinity_Return_Min()
        {
            const double min = 0.0;
            const double max = 1.0;
            var actual = MathUtilities.Clamp(double.NegativeInfinity, min, max);
            Assert.Equal(min, actual);
        }

        [Fact]
        public void Clamp_Input_PositiveInfinity_Return_Max()
        {
            const double min = 0.0;
            const double max = 1.0;
            var actual = MathUtilities.Clamp(double.PositiveInfinity, min, max);
            Assert.Equal(max, actual);
        }
    }
}
