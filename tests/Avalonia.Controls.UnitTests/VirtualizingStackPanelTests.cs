// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
                var target = (IVirtualizingPanel)new VirtualizingStackPanel();
                var controller = new Mock<IVirtualizingController>();

                target.Controller = controller.Object;
                target.Measure(new Size(100, 100));

                controller.Verify(x => x.UpdateControls(), Times.Once());
            }

            [Fact]
            public void Measure_Invokes_Controller_UpdateControls_If_AvailableSize_Changes()
            {
                var target = (IVirtualizingPanel)new VirtualizingStackPanel();
                var controller = new Mock<IVirtualizingController>();

                target.Controller = controller.Object;
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
                var target = (IVirtualizingPanel)new VirtualizingStackPanel();
                var controller = new Mock<IVirtualizingController>();

                target.Controller = controller.Object;
                target.Measure(new Size(100, 100));
                target.InvalidateMeasure();
                target.Measure(new Size(100, 100));

                controller.Verify(x => x.UpdateControls(), Times.Once());
            }

            [Fact]
            public void Measure_Invokes_Controller_UpdateControls_If_AvailableSize_Is_The_Same_After_ForceInvalidateMeasure()
            {
                var target = (IVirtualizingPanel)new VirtualizingStackPanel();
                var controller = new Mock<IVirtualizingController>();

                target.Controller = controller.Object;
                target.Measure(new Size(100, 100));
                target.ForceInvalidateMeasure();
                target.Measure(new Size(100, 100));

                controller.Verify(x => x.UpdateControls(), Times.Exactly(2));
            }

            [Fact]
            public void Arrange_Invokes_Controller_UpdateControls()
            {
                var target = (IVirtualizingPanel)new VirtualizingStackPanel();
                var controller = new Mock<IVirtualizingController>();

                target.Controller = controller.Object;
                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 110, 110));

                controller.Verify(x => x.UpdateControls(), Times.Exactly(2));
            }

            [Fact]
            public void Reports_IsFull_False_Until_Measure_Height_Is_Reached()
            {
                var target = (IVirtualizingPanel)new VirtualizingStackPanel();

                target.Measure(new Size(100, 100));

                Assert.Equal(new Size(0, 0), target.DesiredSize);
                Assert.Equal(new Size(0, 0), target.Bounds.Size);

                Assert.False(target.IsFull);
                Assert.Equal(0, target.OverflowCount);
                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                Assert.False(target.IsFull);
                Assert.Equal(0, target.OverflowCount);
                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                Assert.True(target.IsFull);
                Assert.Equal(0, target.OverflowCount);
            }

            [Fact]
            public void Reports_Overflow_After_Arrange()
            {
                var target = (IVirtualizingPanel)new VirtualizingStackPanel();

                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(target.DesiredSize));

                Assert.Equal(new Size(0, 0), target.Bounds.Size);

                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                Assert.Equal(0, target.OverflowCount);

                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(target.DesiredSize));

                Assert.Equal(2, target.OverflowCount);
            }

            [Fact]
            public void Reports_Correct_Overflow_During_Arrange()
            {
                var target = (IVirtualizingPanel)new VirtualizingStackPanel();
                var controller = new Mock<IVirtualizingController>();
                var called = false;

                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                target.Children.Add(new Canvas { Width = 50, Height = 52 });
                target.Measure(new Size(100, 100));

                controller.Setup(x => x.UpdateControls()).Callback(() =>
                {
                    Assert.Equal(2, target.PixelOverflow);
                    Assert.Equal(0, target.OverflowCount);
                    called = true;
                });

                target.Controller = controller.Object;
                target.Arrange(new Rect(target.DesiredSize));

                Assert.True(called);
            }

            [Fact]
            public void Reports_PixelOverflow_After_Arrange()
            {
                var target = (IVirtualizingPanel)new VirtualizingStackPanel();

                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                target.Children.Add(new Canvas { Width = 50, Height = 52 });

                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(target.DesiredSize));

                Assert.Equal(2, target.PixelOverflow);
            }

            [Fact]
            public void Reports_PixelOverflow_After_Arrange_Smaller_Than_Measure()
            {
                var target = (IVirtualizingPanel)new VirtualizingStackPanel();

                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                target.Children.Add(new Canvas { Width = 50, Height = 52 });

                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 50, 50));

                Assert.Equal(52, target.PixelOverflow);
            }

            [Fact]
            public void Reports_PixelOverflow_With_PixelOffset()
            {
                var target = (IVirtualizingPanel)new VirtualizingStackPanel();

                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                target.Children.Add(new Canvas { Width = 50, Height = 52 });
                target.PixelOffset = 2;

                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(target.DesiredSize));

                Assert.Equal(2, target.PixelOverflow);
            }

            [Fact]
            public void PixelOffset_Can_Be_More_Than_Child_Without_Affecting_IsFull()
            {
                var target = (IVirtualizingPanel)new VirtualizingStackPanel();

                target.Children.Add(new Canvas { Width = 50, Height = 50 });
                target.Children.Add(new Canvas { Width = 50, Height = 52 });
                target.PixelOffset = 55;

                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(target.DesiredSize));

                Assert.Equal(55, target.PixelOffset);
                Assert.Equal(2, target.PixelOverflow);
                Assert.True(target.IsFull);
            }

            [Fact]
            public void Passes_Navigation_Request_To_ILogicalScrollable_Parent()
            {
                var presenter = new Mock<ILogical>().As<IControl>();
                var scrollable = presenter.As<ILogicalScrollable>();
                var target = (IVirtualizingPanel)new VirtualizingStackPanel();
                var from = new Canvas();

                scrollable.Setup(x => x.IsLogicalScrollEnabled).Returns(true);

                ((ISetLogicalParent)target).SetParent(presenter.Object);
                ((INavigableContainer)target).GetControl(NavigationDirection.Next, from);

                scrollable.Verify(x => x.GetControlInDirection(NavigationDirection.Next, from));
            }
        }
    }
}