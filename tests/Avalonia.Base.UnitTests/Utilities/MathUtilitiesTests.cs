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
        }
    }
}
