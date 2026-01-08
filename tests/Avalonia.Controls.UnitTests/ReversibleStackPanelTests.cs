using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ReversibleStackPanelTests : ScopedTestBase
    {
        [Fact]
        public void Arranges_In_Reverse_Order()
        {
            var target = new ReversibleStackPanel
            {
                ReverseOrder = true,
                Children =
                {
                    new Border { Height = 30, Width = 10 },
                    new Border { Height = 50 },
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));

            Assert.Equal(new Rect(0, 50, 10, 30), target.Children[0].Bounds);
            Assert.Equal(new Rect(0, 0, 10, 50), target.Children[1].Bounds);
        }

        [Fact]
        public void Invalidates_Arrange_On_Reverse_Order_Change()
        {
            var target = new ReversibleStackPanel
            {
                Children =
                {
                    new Border(),
                    new Border(),
                }
            };

            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));
            target.ReverseOrder = true;

            Assert.True(target.IsMeasureValid);
            Assert.False(target.IsArrangeValid);
        }
    }
}
