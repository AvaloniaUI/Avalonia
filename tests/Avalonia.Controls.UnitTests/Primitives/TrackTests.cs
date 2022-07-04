using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class TrackTests
    {
        [Fact]
        public void Measure_Should_Return_Thumb_DesiredWidth_In_Vertical_Orientation()
        {
            var thumb = new Thumb
            {
                Width = 12,
            };

            var target = new Track
            {
                Thumb = thumb,
                Orientation = Orientation.Vertical,
            };

            target.Measure(new Size(100, 100));

            Assert.Equal(new Size(12, 0), target.DesiredSize);
        }

        [Fact]
        public void Measure_Should_Return_Thumb_DesiredHeight_In_Horizontal_Orientation()
        {
            var thumb = new Thumb
            {
                Height = 12,
            };

            var target = new Track
            {
                Thumb = thumb,
                Orientation = Orientation.Horizontal,
            };

            target.Measure(new Size(100, 100));

            Assert.Equal(new Size(0, 12), target.DesiredSize);
        }

        [Fact]
        public void Should_Arrange_Thumb_In_Horizontal_Orientation()
        {
            var thumb = new Thumb
            {
                Height = 12,
            };

            var target = new Track
            {
                Thumb = thumb,
                Orientation = Orientation.Horizontal,
                Minimum = 100,
                Maximum = 200,
                Height = 12,
                Value = 150,
                ViewportSize = 50,
            };

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(33, 0, 34, 12), thumb.Bounds);
        }

        [Fact]
        public void Should_Arrange_Thumb_In_Vertical_Orientation()
        {
            var thumb = new Thumb
            {
                Width = 12,
            };

            var target = new Track
            {
                Thumb = thumb,
                Orientation = Orientation.Vertical,
                Minimum = 100,
                Maximum = 200,
                Value = 150,
                ViewportSize = 50,
                Width = 12,
            };

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(0, 33, 12, 34), thumb.Bounds);
        }

        [Fact]
        public void Thumb_Should_Have_Zero_Width_When_Minimum_Equals_Maximum()
        {
            var thumb = new Thumb
            {
                Height = 12,
            };

            var target = new Track
            {
                Height = 12,
                Thumb = thumb,
                Orientation = Orientation.Horizontal,
                Minimum = 100,
                Maximum = 100,
            };

            target.Measure(new Size(100, 100));
            target.Arrange(new Rect(0, 0, 100, 100));

            Assert.Equal(new Rect(0, 0, 0, 12), thumb.Bounds);
        }

        [Fact]
        public void Thumb_Should_Be_Logical_Child()
        {
            var thumb = new Thumb
            {
                Height = 12,
            };

            var target = new Track
            {
                Height = 12,
                Thumb = thumb,
                Orientation = Orientation.Horizontal,
                Minimum = 100,
                Maximum = 100,
            };

            Assert.Same(thumb.Parent, target);
            Assert.Equal(new[] { thumb }, ((ILogical)target).LogicalChildren);
        }

        [Fact]
        public void Should_Not_Pass_Invalid_Arrange_Rect()
        {
            var thumb = new Thumb { Width = 100.873106060606 };
            var increaseButton = new Button { Width = 10 };
            var decreaseButton = new Button { Width = 10 };

            var target = new Track
            {
                Height = 12,
                Thumb = thumb,
                IncreaseButton = increaseButton,
                DecreaseButton = decreaseButton,
                Orientation = Orientation.Horizontal,
                Minimum = 0,
                Maximum = 287,
                Value = 287,
                ViewportSize = 241,
            };

            target.Measure(Size.Infinity);

            // #1297 was occuring here.
            target.Arrange(new Rect(0, 0, 221, 12));
        }
    }
}
