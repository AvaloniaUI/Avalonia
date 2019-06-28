// -----------------------------------------------------------------------
// <copyright file="WindowTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
                var parent = Mock.Of<IWindowImpl>();
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
                var parent = Mock.Of<IWindowImpl>();
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
                var parent = new Mock<IWindowImpl>();
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
                var windowImpl = Mock.Of<IWindowImpl>(x => x.Scaling == 1);
                var target = new Window(windowImpl);

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
                var parent = new Mock<IWindowImpl>();
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
            var screen1 = new Mock<Screen>(new PixelRect(new PixelSize(1920, 1080)), new PixelRect(new PixelSize(1920, 1040)), true);
            var screen2 = new Mock<Screen>(new PixelRect(new PixelSize(1366, 768)), new PixelRect(new PixelSize(1366, 728)), false);

            var screens = new Mock<IScreenImpl>();
            screens.Setup(x => x.AllScreens).Returns(new Screen[] { screen1.Object, screen2.Object });

            var windowImpl = new Mock<IWindowImpl>();
            windowImpl.SetupProperty(x => x.Position);
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
            var parentWindowImpl = new Mock<IWindowImpl>();
            parentWindowImpl.SetupProperty(x => x.Position);
            parentWindowImpl.Setup(x => x.ClientSize).Returns(new Size(800, 480));
            parentWindowImpl.Setup(x => x.MaxClientSize).Returns(new Size(1920, 1080));
            parentWindowImpl.Setup(x => x.Scaling).Returns(1);

            var windowImpl = new Mock<IWindowImpl>();
            windowImpl.SetupProperty(x => x.Position);
            windowImpl.Setup(x => x.ClientSize).Returns(new Size(320, 200));
            windowImpl.Setup(x => x.MaxClientSize).Returns(new Size(1920, 1080));
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
                    window.Owner = parentWindow;

                    window.Show();

                    var expectedPosition = new PixelPoint(
                        (int)(parentWindow.Position.X + parentWindow.ClientSize.Width / 2 - window.ClientSize.Width / 2),
                        (int)(parentWindow.Position.Y + parentWindow.ClientSize.Height / 2 - window.ClientSize.Height / 2));

                    Assert.Equal(window.Position, expectedPosition);
                }
            }
        }

        private IWindowImpl CreateImpl(Mock<IRenderer> renderer)
        {
            return Mock.Of<IWindowImpl>(x =>
                x.Scaling == 1 &&
                x.CreateRenderer(It.IsAny<IRenderRoot>()) == renderer.Object);
        }
    }
}
