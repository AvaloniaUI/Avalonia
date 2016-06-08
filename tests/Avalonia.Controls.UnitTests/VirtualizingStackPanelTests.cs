// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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

                controller.Verify(x => x.UpdateControls());
            }

            [Fact]
            public void Arrange_Invokes_Controller_UpdateControls()
            {
                var target = (IVirtualizingPanel)new VirtualizingStackPanel();
                var controller = new Mock<IVirtualizingController>();

                target.Controller = controller.Object;
                target.Measure(new Size(100, 100));
                target.Arrange(new Rect(0, 0, 110, 110));

                controller.Verify(x => x.UpdateControls());
            }

            [Fact]
            public void Reports_IsFull_False_Until_Measure_Height_Is_Reached()
            {
                var target = (IVirtualizingPanel)new VirtualizingStackPanel();

                target.Measure(new Size(100, 100));

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
        }
    }
}