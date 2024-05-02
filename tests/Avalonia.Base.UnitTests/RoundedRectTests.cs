using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class RoundedRectTests
    {

        [Theory,
            // Corners
            InlineData(0, 0, false),
            InlineData(100, 0, false),
            InlineData(100, 100, false),
            InlineData(0, 100, false),
            // Indent 10px
            InlineData(10, 10, false),
            InlineData(90, 10, true),
            InlineData(90, 90, false),
            InlineData(10, 90, true),
            // Indent 17px
            InlineData(17, 17, false),
            InlineData(83, 17, true),
            InlineData(83, 83, true),
            InlineData(17, 83, true),
            // Center
            InlineData(50, 50, true),
        ]
        public void ContainsExclusive_Should_Return_Expected_Result_For_Point(double x, double y, bool expectedResult)
        {
            var rrect = new RoundedRect(new Rect(0, 0, 100, 100), new CornerRadius(60, 10, 50, 30));

            Assert.Equal(expectedResult, rrect.ContainsExclusive(new Point(x, y)));
        }

    }
}
