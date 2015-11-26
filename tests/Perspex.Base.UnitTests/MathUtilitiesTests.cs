using Perspex.Utilities;
using Xunit;

namespace Perspex.Base.UnitTests
{
    public class MathUtilitiesTests
    {
        [Fact]
        public void Number_Equals_Itself()
        {
            Assert.True(MathUtilities.Equal(2.0, 2.0));
        }

        [Fact]
        public void Near_Numbers_Are_Equal()
        {
            Assert.True(MathUtilities.Equal(0.4 - 0.3, 0.1));
            Assert.True(MathUtilities.Equal(0.3 - 0.4, -0.1));
        }

        [Fact]
        public void Not_Near_Numbers_Are_Not_Equal()
        {
            Assert.False(MathUtilities.Equal(1.0 + MathUtilities.DoubleEpsilon * 2, 1.0));
        }
    }
}
