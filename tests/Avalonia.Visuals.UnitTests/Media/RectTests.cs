using System.Globalization;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class RectTests
    {
        [Fact]
        public void Parse_Parses()
        {
            var rect = Rect.Parse("1,2 3,-4", CultureInfo.CurrentCulture);
            var expected = new Rect(1, 2, 3, -4);
            Assert.Equal(expected, rect);
        }
    }
}