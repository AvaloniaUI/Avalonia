using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class TrackTests : ScopedTestBase
    {
        [Theory]
        [InlineData(Orientation.Vertical, true, false, false, 100)]
        [InlineData(Orientation.Vertical, true, false, true, 100)]
        [InlineData(Orientation.Vertical, false, false, false, 100)]
        [InlineData(Orientation.Horizontal, false, false, false, double.NaN)]
        [InlineData(Orientation.Horizontal, true, false, false, double.NaN)]
        [InlineData(Orientation.Vertical, true, true, false, 100)]
        [InlineData(Orientation.Vertical, true, true, true, 100)]
        public void Thumb_Should_Follow_Pointer_Regardless_Of_Layout_Timing(
            Orientation orientation, bool reversed, bool deferred, bool arrangeBetweenMoves, double viewportSize)
        {
            var thumb = new Thumb { MinWidth = 100, MinHeight = 100 };
            var target = new Track
            {
                Thumb = thumb,
                Orientation = orientation,
                IsDirectionReversed = reversed,
                DeferThumbDrag = deferred,
                Maximum = 400,
                ViewportSize = viewportSize,
                Value = 200,
            };
            var root = new TestRoot(target) { Width = 200, Height = 200 };
            root.ExecuteInitialLayoutPass();

            var mouse = new MouseTestHelper();
            var start = thumb.Bounds.Center;
            var initialPosition = thumb.Bounds.Position;
            var movement = orientation == Orientation.Vertical ? new Vector(0, 20) : new Vector(20, 0);

            mouse.Down(thumb, position: start);
            mouse.Move(thumb, start + movement / 2);
            if (arrangeBetweenMoves)
                root.LayoutManager.ExecuteLayoutPass();
            mouse.Move(thumb, start + movement);
            if (arrangeBetweenMoves)
                root.LayoutManager.ExecuteLayoutPass();
            mouse.Move(thumb, start + movement);
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(initialPosition + movement, thumb.Bounds.Position);
            if (deferred)
                Assert.Equal(200, target.Value);

            mouse.Up(thumb, position: start + movement);
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(initialPosition + movement, thumb.Bounds.Position);
            var increasing = orientation == Orientation.Vertical ? reversed : !reversed;
            Assert.Equal(increasing ? 280 : 120, target.Value);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Thumb_Should_Follow_Pointer_When_Reversing_And_Clamping(bool deferred)
        {
            var thumb = new Thumb();
            var target = new Track
            {
                Thumb = thumb,
                Orientation = Orientation.Vertical,
                IsDirectionReversed = true,
                DeferThumbDrag = deferred,
                Maximum = 100,
                ViewportSize = 100,
                Value = 50,
            };
            var root = new TestRoot(target) { Width = 200, Height = 200 };
            root.ExecuteInitialLayoutPass();

            var mouse = new MouseTestHelper();
            var start = thumb.Bounds.Center;
            var initialPosition = thumb.Bounds.Position;
            var axis = new Vector(0, 1);
            mouse.Down(thumb, position: start);

            mouse.Move(thumb, start + axis * 20);
            mouse.Move(thumb, start + axis * 10);
            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(initialPosition + axis * 10, thumb.Bounds.Position);

            mouse.Move(thumb, start + axis * 10);
            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(initialPosition + axis * 10, thumb.Bounds.Position);

            mouse.Move(thumb, start + axis * 100);
            mouse.Move(thumb, start + axis * 200);
            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(initialPosition + axis * 50, thumb.Bounds.Position);

            mouse.Move(thumb, start + axis * 40);
            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(initialPosition + axis * 40, thumb.Bounds.Position);

            mouse.Move(thumb, start - axis * 100);
            mouse.Move(thumb, start - axis * 200);
            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(initialPosition - axis * 50, thumb.Bounds.Position);

            mouse.Move(thumb, start - axis * 40);
            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(initialPosition - axis * 40, thumb.Bounds.Position);

            mouse.Up(thumb, position: start - axis * 40);
            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(initialPosition - axis * 40, thumb.Bounds.Position);
            Assert.Equal(10, target.Value);
        }

        [Fact]
        public void New_Drag_Should_Not_Reuse_Movement_From_Previous_Drag_Before_Arrange()
        {
            var thumb = new Thumb();
            var target = new Track
            {
                Thumb = thumb,
                Orientation = Orientation.Vertical,
                IsDirectionReversed = true,
                Maximum = 100,
                ViewportSize = 100,
                Value = 50,
            };
            var root = new TestRoot(target) { Width = 200, Height = 200 };
            root.ExecuteInitialLayoutPass();
            var mouse = new MouseTestHelper();
            var start = thumb.Bounds.Center;
            var initialPosition = thumb.Bounds.Position;

            mouse.Down(thumb, position: start);
            mouse.Move(thumb, start + new Vector(0, 10));
            mouse.Up(thumb, position: start + new Vector(0, 10));
            mouse.Down(thumb, position: start + new Vector(0, 10));
            mouse.Move(thumb, start + new Vector(0, 15));
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(65, target.Value);
            Assert.Equal(initialPosition + new Vector(0, 15), thumb.Bounds.Position);

            mouse.Move(thumb, start + new Vector(0, 20));
            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(70, target.Value);
            Assert.Equal(initialPosition + new Vector(0, 20), thumb.Bounds.Position);
            mouse.Up(thumb, position: start + new Vector(0, 20));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Drag_Should_Preserve_Anchor_When_Range_Or_Viewport_Changes(bool changeViewport)
        {
            var thumb = new Thumb();
            var target = new Track
            {
                Thumb = thumb,
                Orientation = Orientation.Vertical,
                IsDirectionReversed = true,
                Maximum = 100,
                ViewportSize = 100,
                Value = 50,
            };
            var root = new TestRoot(target) { Width = 200, Height = 200 };
            root.ExecuteInitialLayoutPass();
            var mouse = new MouseTestHelper();
            var start = thumb.Bounds.Center;
            mouse.Down(thumb, position: start);
            mouse.Move(thumb, start + new Vector(0, 10));

            if (changeViewport)
            {
                target.ViewportSize = 300;
                target.InvalidateMeasure();
            }
            else
                target.Maximum = 300;
            root.LayoutManager.ExecuteLayoutPass();
            var position = thumb.Bounds.Position;

            mouse.Move(thumb, start + new Vector(0, 10));
            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(60, target.Value);
            Assert.Equal(position, thumb.Bounds.Position);

            mouse.Move(thumb, start + new Vector(0, 30));
            mouse.Move(thumb, start + new Vector(0, 20));
            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(position + new Vector(0, 10), thumb.Bounds.Position);
            mouse.Up(thumb, position: start + new Vector(0, 20));
        }

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
