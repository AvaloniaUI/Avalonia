using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class WindowTests
    {
        [Fact]
        public void Setting_Title_Should_Set_Impl_Title()
        {
            var windowImpl = new Mock<IWindowImpl>();
            var windowingPlatform = new MockWindowingPlatform(() => windowImpl.Object);

            using (UnitTestApplication.Start(new TestServices(windowingPlatform: windowingPlatform)))
            {
                var target = new Window();

                target.Title = "Hello World";

                windowImpl.Verify(x => x.SetTitle("Hello World"));
            }
        }

        [Fact]
        public void IsVisible_Should_Initially_Be_False()
        {
            using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
            {
                var window = new Window();

                Assert.False(window.IsVisible);
            }
        }

        [Fact]
        public void IsVisible_Should_Be_True_After_Show()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();

                window.Show();

                Assert.True(window.IsVisible);
            }
        }

        [Fact]
        public void IsVisible_Should_Be_True_After_ShowDialog()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parent = new Window();
                parent.Show();
                var window = new Window();

                var task = window.ShowDialog(parent);

                Assert.True(window.IsVisible);
            }
        }

        [Fact]
        public void IsVisible_Should_Be_False_After_Hide()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();

                window.Show();
                window.Hide();

                Assert.False(window.IsVisible);
            }
        }

        [Fact]
        public void IsVisible_Should_Be_False_After_Close()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();

                window.Show();
                window.Close();

                Assert.False(window.IsVisible);
            }
        }

        [Fact]
        public void IsVisible_Should_Be_False_After_Impl_Signals_Close()
        {
            var windowImpl = new Mock<IWindowImpl>();
            windowImpl.SetupProperty(x => x.Closed);
            windowImpl.Setup(x => x.Scaling).Returns(1);

            var services = TestServices.StyledWindow.With(
                windowingPlatform: new MockWindowingPlatform(() => windowImpl.Object));

            using (UnitTestApplication.Start(services))
            {
                var window = new Window();

                window.Show();
                windowImpl.Object.Closed();

                Assert.False(window.IsVisible);
            }
        }

        [Fact]
        public void Closing_Should_Only_Be_Invoked_Once()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();
                var count = 0;

                window.Closing +=
                    (sender, e) =>
                    {
                        count++;
                    };

                window.Show();
                window.Close();

                Assert.Equal(1, count);
            }
        }

        [Fact]
        public void Showing_Should_Start_Renderer()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var renderer = new Mock<IRenderer>();
                var target = new Window(CreateImpl(renderer));

                target.Show();

                renderer.Verify(x => x.Start(), Times.Once);
            }
        }

        [Fact]
        public void ShowDialog_Should_Start_Renderer()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parent = Mock.Of<Window>();
                var renderer = new Mock<IRenderer>();
                var target = new Window(CreateImpl(renderer));

                target.ShowDialog<object>(parent);

                renderer.Verify(x => x.Start(), Times.Once);
            }
        }

        [Fact]
        public void ShowDialog_Should_Raise_Opened()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parent = Mock.Of<Window>();
                var target = new Window();
                var raised = false;

                target.Opened += (s, e) => raised = true;

                target.ShowDialog<object>(parent);

                Assert.True(raised);
            }
        }

        [Fact]
        public void Hiding_Should_Stop_Renderer()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var renderer = new Mock<IRenderer>();
                var target = new Window(CreateImpl(renderer));

                target.Show();
                target.Hide();

                renderer.Verify(x => x.Stop(), Times.Once);
            }
        }

        [Fact]
        public async Task ShowDialog_With_ValueType_Returns_Default_When_Closed()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parent = new Mock<Window>();
                var windowImpl = new Mock<IWindowImpl>();
                windowImpl.SetupProperty(x => x.Closed);
                windowImpl.Setup(x => x.Scaling).Returns(1);

                var target = new Window(windowImpl.Object);
                var task = target.ShowDialog<bool>(parent.Object);

                windowImpl.Object.Closed();

                var result = await task;
                Assert.False(result);
            }
        }

        [Fact]
        public void Calling_Show_On_Closed_Window_Should_Throw()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var target = new Window();

                target.Show();
                target.Close();

                var openedRaised = false;
                target.Opened += (s, e) => openedRaised = true;

                var ex = Assert.Throws<InvalidOperationException>(() => target.Show());
                Assert.Equal("Cannot re-show a closed window.", ex.Message);
                Assert.False(openedRaised);
            }
        }

        [Fact]
        public async Task Calling_ShowDialog_On_Closed_Window_Should_Throw()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var parent = new Mock<Window>();
                var windowImpl = new Mock<IWindowImpl>();
                windowImpl.SetupProperty(x => x.Closed);
                windowImpl.Setup(x => x.Scaling).Returns(1);

                var target = new Window(windowImpl.Object);
                var task = target.ShowDialog<bool>(parent.Object);

                windowImpl.Object.Closed();
                await task;

                var openedRaised = false;
                target.Opened += (s, e) => openedRaised = true;

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => target.ShowDialog<bool>(parent.Object));
                Assert.Equal("Cannot re-show a closed window.", ex.Message);
                Assert.False(openedRaised);
            }
        }

        [Fact]
        public void Window_Should_Be_Centered_When_WindowStartupLocation_Is_CenterScreen()
        {
            var screen1 = new Mock<Screen>(1.0, new PixelRect(new PixelSize(1920, 1080)), new PixelRect(new PixelSize(1920, 1040)), true);
            var screen2 = new Mock<Screen>(1.0, new PixelRect(new PixelSize(1366, 768)), new PixelRect(new PixelSize(1366, 728)), false);

            var screens = new Mock<IScreenImpl>();
            screens.Setup(x => x.AllScreens).Returns(new Screen[] { screen1.Object, screen2.Object });

            var windowImpl = MockWindowingPlatform.CreateWindowMock();
            windowImpl.Setup(x => x.ClientSize).Returns(new Size(800, 480));
            windowImpl.Setup(x => x.Scaling).Returns(1);
            windowImpl.Setup(x => x.Screen).Returns(screens.Object);

            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window(windowImpl.Object);
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                window.Position = new PixelPoint(60, 40);

                window.Show();

                var expectedPosition = new PixelPoint(
                    (int)(screen1.Object.WorkingArea.Size.Width / 2 - window.ClientSize.Width / 2),
                    (int)(screen1.Object.WorkingArea.Size.Height / 2 - window.ClientSize.Height / 2));

                Assert.Equal(window.Position, expectedPosition);
            }
        }

        [Fact]
        public void Window_Should_Be_Centered_Relative_To_Owner_When_WindowStartupLocation_Is_CenterOwner()
        {
            var parentWindowImpl = MockWindowingPlatform.CreateWindowMock();
            parentWindowImpl.Setup(x => x.ClientSize).Returns(new Size(800, 480));
            parentWindowImpl.Setup(x => x.MaxAutoSizeHint).Returns(new Size(1920, 1080));
            parentWindowImpl.Setup(x => x.Scaling).Returns(1);

            var windowImpl = MockWindowingPlatform.CreateWindowMock();
            windowImpl.Setup(x => x.ClientSize).Returns(new Size(320, 200));
            windowImpl.Setup(x => x.MaxAutoSizeHint).Returns(new Size(1920, 1080));
            windowImpl.Setup(x => x.Scaling).Returns(1);

            var parentWindowServices = TestServices.StyledWindow.With(
                windowingPlatform: new MockWindowingPlatform(() => parentWindowImpl.Object));

            var windowServices = TestServices.StyledWindow.With(
                windowingPlatform: new MockWindowingPlatform(() => windowImpl.Object));

            using (UnitTestApplication.Start(parentWindowServices))
            {
                var parentWindow = new Window();
                parentWindow.Position = new PixelPoint(60, 40);

                parentWindow.Show();

                using (UnitTestApplication.Start(windowServices))
                {
                    var window = new Window();
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.Position = new PixelPoint(60, 40);                    

                    window.ShowDialog(parentWindow);

                    var expectedPosition = new PixelPoint(
                        (int)(parentWindow.Position.X + parentWindow.ClientSize.Width / 2 - window.ClientSize.Width / 2),
                        (int)(parentWindow.Position.Y + parentWindow.ClientSize.Height / 2 - window.ClientSize.Height / 2));

                    Assert.Equal(window.Position, expectedPosition);
                }
            }
        }

        public class SizingTests
        {
            [Fact]
            public void Child_Should_Be_Measured_With_Width_And_Height_If_SizeToContent_Is_Manual()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var child = new ChildControl();
                    var target = new Window
                    {
                        Width = 100,
                        Height = 50,
                        SizeToContent = SizeToContent.Manual,
                        Content = child
                    };

                    Show(target);

                    Assert.Equal(1, child.MeasureSizes.Count);
                    Assert.Equal(new Size(100, 50), child.MeasureSizes[0]);
                }
            }

            [Fact]
            public void Child_Should_Be_Measured_With_ClientSize_If_SizeToContent_Is_Manual_And_No_Width_Height_Specified()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var windowImpl = MockWindowingPlatform.CreateWindowMock();
                    windowImpl.Setup(x => x.ClientSize).Returns(new Size(550, 450));

                    var child = new ChildControl();
                    var target = new Window(windowImpl.Object)
                    {
                        SizeToContent = SizeToContent.Manual,
                        Content = child
                    };

                    Show(target);

                    Assert.Equal(1, child.MeasureSizes.Count);
                    Assert.Equal(new Size(550, 450), child.MeasureSizes[0]);
                }
            }

            [Fact]
            public void Child_Should_Be_Measured_With_MaxAutoSizeHint_If_SizeToContent_Is_WidthAndHeight()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var windowImpl = MockWindowingPlatform.CreateWindowMock();
                    windowImpl.Setup(x => x.MaxAutoSizeHint).Returns(new Size(1200, 1000));

                    var child = new ChildControl();
                    var target = new Window(windowImpl.Object)
                    {
                        Width = 100,
                        Height = 50,
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Content = child
                    };

                    target.Show();

                    Assert.Equal(1, child.MeasureSizes.Count);
                    Assert.Equal(new Size(1200, 1000), child.MeasureSizes[0]);
                }
            }

            [Fact]
            public void Should_Not_Have_Offset_On_Bounds_When_Content_Larger_Than_Max_Window_Size()
            {
                // Issue #3784.
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var windowImpl = MockWindowingPlatform.CreateWindowMock();
                    var clientSize = new Size(200, 200);
                    var maxClientSize = new Size(480, 480);

                    windowImpl.Setup(x => x.Resize(It.IsAny<Size>())).Callback<Size>(size =>
                    {
                        clientSize = size.Constrain(maxClientSize);
                        windowImpl.Object.Resized?.Invoke(clientSize);
                    });

                    windowImpl.Setup(x => x.ClientSize).Returns(() => clientSize);

                    var child = new Canvas
                    {
                        Width = 400,
                        Height = 800,
                    };
                    var target = new Window(windowImpl.Object)
                    {
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Content = child
                    };

                    Show(target);

                    Assert.Equal(new Size(400, 480), target.Bounds.Size);

                    // Issue #3784 causes this to be (0, 160) which makes no sense as Window has no
                    // parent control to be offset against.
                    Assert.Equal(new Point(0, 0), target.Bounds.Position);
                }
            }

            [Fact]
            public void Width_Height_Should_Not_Be_NaN_After_Show_With_SizeToContent_WidthAndHeight()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var child = new Canvas
                    {
                        Width = 400,
                        Height = 800,
                    };

                    var target = new Window()
                    {
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Content = child
                    };

                    Show(target);

                    Assert.Equal(400, target.Width);
                    Assert.Equal(800, target.Height);
                }
            }

            [Fact]
            public void SizeToContent_Should_Not_Be_Lost_On_Show()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var child = new Canvas
                    {
                        Width = 400,
                        Height = 800,
                    };

                    var target = new Window()
                    {
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Content = child
                    };

                    Show(target);

                    Assert.Equal(SizeToContent.WidthAndHeight, target.SizeToContent);
                }
            }

            [Fact]
            public void Width_Height_Should_Be_Updated_When_SizeToContent_Is_WidthAndHeight()
            {
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var child = new Canvas
                    {
                        Width = 400,
                        Height = 800,
                    };

                    var target = new Window()
                    {
                        SizeToContent = SizeToContent.WidthAndHeight,
                        Content = child
                    };

                    Show(target);

                    Assert.Equal(400, target.Width);
                    Assert.Equal(800, target.Height);

                    child.Width = 410;
                    target.LayoutManager.ExecuteLayoutPass();

                    Assert.Equal(410, target.Width);
                    Assert.Equal(800, target.Height);
                    Assert.Equal(SizeToContent.WidthAndHeight, target.SizeToContent);
                }
            }

            [Fact]
            public void Setting_Width_Should_Resize_WindowImpl()
            {
                // Issue #3796
                using (UnitTestApplication.Start(TestServices.StyledWindow))
                {
                    var target = new Window()
                    {
                        Width = 400,
                        Height = 800,
                    };

                    Show(target);

                    Assert.Equal(400, target.Width);
                    Assert.Equal(800, target.Height);

                    target.Width = 410;
                    target.LayoutManager.ExecuteLayoutPass();

                    var windowImpl = Mock.Get(target.PlatformImpl);
                    windowImpl.Verify(x => x.Resize(new Size(410, 800)));
                    Assert.Equal(410, target.Width);
                }
            }

            protected virtual void Show(Window window)
            {
                window.Show();
            }
        }

        public class DialogSizingTests : SizingTests
        {
            protected override void Show(Window window)
            {
                var owner = new Window();
                window.ShowDialog(owner);
            }
        }

        private IWindowImpl CreateImpl(Mock<IRenderer> renderer)
        {
            return Mock.Of<IWindowImpl>(x =>
                x.Scaling == 1 &&
                x.CreateRenderer(It.IsAny<IRenderRoot>()) == renderer.Object);
        }

        private class ChildControl : Control
        {
            public List<Size> MeasureSizes { get; } = new List<Size>();

            protected override Size MeasureOverride(Size availableSize)
            {
                MeasureSizes.Add(availableSize);
                return base.MeasureOverride(availableSize);
            }
        }
    }
}
