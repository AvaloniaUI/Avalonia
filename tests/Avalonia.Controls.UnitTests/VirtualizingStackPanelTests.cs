using System;
using System.Collections.Generic;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class VirtualizingStackPanelTests
    {
        public class Vertical
        {
            [Fact]
            public void Measure_Invokes_Controller_UpdateControls()
            {
                var target = new VirtualizingStackPanel();
                var controller = new Mock<IVirtualizingController>();

                ((IVirtualizingPanel)target).Controller = controller.Object;
                target.Measure(new Size(100, 100));

                controller.Verify(x => x.UpdateControls(), Times.Once());
            }

            [Fact]
            public void Measure_Invokes_Controller_UpdateControls_If_AvailableSize_Changes()
            {
                var target = new VirtualizingStackPanel();
                var controller = new Mock<IVirtualizingController>();

                ((IVirtualizingPanel)target).Controller = controller.Object;
                target.Measure(new Size(100, 100));
                target.InvalidateMeasure();
                target.Measure(new Size(100, 100));
                target.InvalidateMeasure();
                target.Measure(new Size(100, 101));

                controller.Verify(x => x.UpdateControls(), Times.Exactly(2));
            }

            [Fact]
            public void Measure_Does_Not_Invoke_Controller_UpdateControls_If_AvailableSize_Is_The_Same()
            {
                var target = new VirtualizingStackPanel();
                var controller = new Mock<IVirtualizingController>();

                ((IVirtualizingPanel)target).Controller = controller.Object;
                target.Measure(new Size(100, 100));
                target.InvalidateMeasure();
                target.Measure(new Size(100, 100));

                controller.Verify(x => x.UpdateControls(), Times.Once());
            }

            [Fact]
            public void Measure_Invokes_Controller_UpdateControls_If_AvailableSize_Is_The_Same_After_ForceInvalidateMeasure()
            {
                var target = new VirtualizingStackPanel();
                var controller = new Mock<IVirtualizingController>();

                ((IVirtualizingPanel)target).Controller = controller.Object;
                target.Measure(new Size(100, 100));
                ((IVirtualizingPanel)target).ForceInvalidateMeasure();
                target.Measure(new Size(100, 100));

                controller.Verify(x => x.UpdateControls(), Times.Exactly(2));
            }

            [Fact]
            public void Arrange_Invokes_Controller_UpdateControls()
            {
                var target = new VirtualizingStackPanel();
                var controller = new Mock<IVirtualizingController>();

                ((IVirtualizingPanel)target).Controller = controller.Object;
                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 110, 110));

                controller.Verify(x => x.UpdateControls(), Times.Exactly(2));
            }

            [Fact]
            public void Reports_IsFull_False_Until_Measure_Height_Is_Reached()
            {
                var target = new VirtualizingStackPanel();
                var vp = (IVirtualizingPanel)target;

                target.Measure(new Size(100, 100));

                Assert.Equal(new Size(0, 0), target.DesiredSize);
                Assert.Equal(new Size(0, 0), target.Bounds.Size);

                Assert.False(vp.IsFull);
                Assert.Equal(0, vp.OverflowCount);
                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                Assert.False(vp.IsFull);
                Assert.Equal(0, vp.OverflowCount);
                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                Assert.True(vp.IsFull);
                Assert.Equal(0, vp.OverflowCount);
            }

            [Fact]
            public void Reports_Overflow_After_Arrange()
            {
                var target = new VirtualizingStackPanel();
                var vp = (IVirtualizingPanel)target;

                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(target.DesiredSize));

                Assert.Equal(new Size(0, 0), target.Bounds.Size);

                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                Assert.Equal(0, vp.OverflowCount);

                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(target.DesiredSize));

                Assert.Equal(2, vp.OverflowCount);
            }

            [Fact]
            public void Reports_Correct_Overflow_During_Arrange()
            {
                var target = new VirtualizingStackPanel();
                var vp = (IVirtualizingPanel)target;
                var controller = new Mock<IVirtualizingController>();
                var called = false;

                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                target.Children.Add(new Canvas { Width = 50, Height = 52 });
                target.Measure(new Size(100, 100));

                controller.Setup(x => x.UpdateControls()).Callback(() =>
                {
                    Assert.Equal(2, vp.PixelOverflow);
                    Assert.Equal(0, vp.OverflowCount);
                    called = true;
                });

                vp.Controller = controller.Object;
                target.Arrange(new Rect(target.DesiredSize));

                Assert.True(called);
            }

            [Fact]
            public void Reports_PixelOverflow_After_Arrange()
            {
                var target = new VirtualizingStackPanel();

                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                target.Children.Add(new Canvas { Width = 50, Height = 52 });

                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(target.DesiredSize));

                Assert.Equal(2, ((IVirtualizingPanel)target).PixelOverflow);
            }

            [Fact]
            public void Reports_PixelOverflow_After_Arrange_Smaller_Than_Measure()
            {
                var target = new VirtualizingStackPanel();

                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                target.Children.Add(new Canvas { Width = 50, Height = 52 });

                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 50, 50));

                Assert.Equal(52, ((IVirtualizingPanel)target).PixelOverflow);
            }

            [Fact]
            public void Reports_PixelOverflow_With_PixelOffset()
            {
                var target = new VirtualizingStackPanel();
                var vp = (IVirtualizingPanel)target;

                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                target.Children.Add(new Canvas { Width = 50, Height = 52 });
                vp.PixelOffset = 2;

                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(target.DesiredSize));

                Assert.Equal(2, vp.PixelOverflow);
            }

            [Fact]
            public void PixelOffset_Can_Be_More_Than_Child_Without_Affecting_IsFull()
            {
                var target = new VirtualizingStackPanel();
                var vp = (IVirtualizingPanel)target;

                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                target.Children.Add(new Canvas { Width = 50, Height = 52 });
                vp.PixelOffset = 55;

                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(target.DesiredSize));

                Assert.Equal(55, vp.PixelOffset);
                Assert.Equal(2, vp.PixelOverflow);
                Assert.True(vp.IsFull);
            }

            [Fact]
            public void Passes_Navigation_Request_To_ILogicalScrollable_Parent()
            {
                var target = new VirtualizingStackPanel();
                var presenter = new TestPresenter { Child = target };
                var from = new Canvas();

                ((INavigableContainer)target).GetControl(NavigationDirection.Next, from, false);

                Assert.Equal(1, presenter.NavigationRequests.Count);
                Assert.Equal((NavigationDirection.Next, from), presenter.NavigationRequests[0]);
            }

            private class TestPresenter : Decorator, ILogicalScrollable
            {
                public bool CanHorizontallyScroll { get; set; }
                public bool CanVerticallyScroll { get; set; }
                public bool IsLogicalScrollEnabled => true;
                public Size ScrollSize { get; }
                public Size PageScrollSize { get; }
                public Size Extent { get; }
                public Vector Offset { get; set; }
                public Size Viewport { get; }

                public event EventHandler ScrollInvalidated;

                public List<(NavigationDirection, Control)> NavigationRequests { get; } = new();

                public bool BringIntoView(Control target, Rect targetRect)
                {
                    throw new NotImplementedException();
                }

                public Control GetControlInDirection(NavigationDirection direction, Control from)
                {
                    NavigationRequests.Add((direction, from));
                    return null;
                }

                public void RaiseScrollInvalidated(EventArgs e)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
