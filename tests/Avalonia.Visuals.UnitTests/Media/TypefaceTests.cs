using System;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class TypefaceTests
    {
        [Fact]
        public void Exception_Should_Be_Thrown_If_FontWeight_LessThanEqualTo_0()
        {
            Assert.Throws<ArgumentException>(() => new Typeface((string) "foo", (FontStyle) 12, (FontWeight) 0));
        }
    }
}
