using System;
using Avalonia.Controls;
using Moq;
using Xunit;

namespace Avalonia.Layout.UnitTests
{
    public class LayoutableTests
    {
        [Fact]
        public void Only_Calls_LayoutManager_InvalidateMeasure_Once()
        {
            var target = new Mock<ILayoutManager>();

            using (Start(target.Object))
            {
                var control = new Decorator();
                var root = new LayoutTestRoot { Child = control };

                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));
                target.ResetCalls();

                control.InvalidateMeasure();
                control.InvalidateMeasure();

                target.Verify(x => x.InvalidateMeasure(control), Times.Once());
            }
        }

        [Fact]
        public void Only_Calls_LayoutManager_InvalidateArrange_Once()
        {
            var target = new Mock<ILayoutManager>();

            using (Start(target.Object))
            {
                var control = new Decorator();
                var root = new LayoutTestRoot { Child = control };

                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));
                target.ResetCalls();

                control.InvalidateArrange();
                control.InvalidateArrange();

                target.Verify(x => x.InvalidateArrange(control), Times.Once());
            }
        }

        [Fact]
        public void Attaching_Control_To_Tree_Invalidates_Parent_Measure()
        {
            var target = new Mock<ILayoutManager>();

            using (Start(target.Object))
            {
                var control = new Decorator();
                var root = new LayoutTestRoot { Child = control };

                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));
                Assert.True(control.IsMeasureValid);

                root.Child = null;
                root.Measure(Size.Infinity);
                root.Arrange(new Rect(root.DesiredSize));

                Assert.False(control.IsMeasureValid);
                Assert.True(root.IsMeasureValid);

                target.ResetCalls();

                root.Child = control;

                Assert.False(root.IsMeasureValid);
                Assert.False(control.IsMeasureValid);
                target.Verify(x => x.InvalidateMeasure(root), Times.Once());
            }
        }

        private IDisposable Start(ILayoutManager layoutManager)
        {
            var result = AvaloniaLocator.EnterScope();
            AvaloniaLocator.CurrentMutable.Bind<ILayoutManager>().ToConstant(layoutManager);
            return result;
        }
    }
}
