using System.Linq;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class UnicodeRangeTests
    {
        [Fact]
        public void Should_Parse_Segments()
        {
            var range = UnicodeRange.Parse("U+0, U+1, U+2, U+3");

            Assert.Equal(new[] { 0, 1, 2, 3 }, range.Segments.Select(x => x.Start));
        }
    }
}
