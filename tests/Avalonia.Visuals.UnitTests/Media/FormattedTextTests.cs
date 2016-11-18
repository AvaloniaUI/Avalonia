using System;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class FormattedTextTests
    {
        [Fact]
        public void Exception_Should_Be_Thrown_If_FontSize_0()
        {
            Assert.Throws<ArgumentException>(() => new FormattedText(
                "foo",
                "Ariel",
                0,
                Size.Infinity));
        }

        [Fact]
        public void Exception_Should_Be_Thrown_If_FontWeight_0()
        {
            Assert.Throws<ArgumentException>(() => new FormattedText(
                "foo",
                "Ariel",
                12,
                Size.Infinity,
                fontWeight: 0));
        }
    }
}
