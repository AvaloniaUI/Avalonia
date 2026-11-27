using System;
using Avalonia.Animation;
using Xunit;

namespace Avalonia.Base.UnitTests.Animation
{
    public class IterationCountTests
    {
        [Fact]
        public void Parse_Should_Not_Accept_Infinite_Followed_By_Soft_Hyphen()
        {
            // Culture-sensitive comparisons ignore the soft hyphen, so they see this string as ending with "INFINITE".
            Assert.Throws<FormatException>(() => IterationCount.Parse("Infinite\u00AD"));
        }
    }
}
