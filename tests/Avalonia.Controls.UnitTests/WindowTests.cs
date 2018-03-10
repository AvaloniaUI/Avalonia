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
                var window = new Window();

                var task = window.ShowDialog();

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
        public void Show_Should_Add_Window_To_OpenWindows()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                ClearOpenWindows();
                var window = new Window();

                window.Show();

                Assert.Equal(new[] { window }, Window.OpenWindows);
            }
        }

        [Fact]
        public void Window_Should_Be_Added_To_OpenWindows_Only_Once()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                ClearOpenWindows();
                var window = new Window();

                window.Show();
                window.Show();
                window.IsVisible = true;

                Assert.Equal(new[] { window }, Window.OpenWindows);

                window.Close();
            }
        }

        [Fact]
        public void Close_Should_Remove_Window_From_OpenWindows()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                ClearOpenWindows();
                var window = new Window();

                window.Show();
                window.Close();

                Assert.Empty(Window.OpenWindows);
            }
        }

        [Fact]
        public void Impl_Closing_Should_Remove_Window_From_OpenWindows()
        {
            var windowImpl = new Mock<IWindowImpl>();
            windowImpl.SetupProperty(x => x.Closed);
            windowImpl.Setup(x => x.Scaling).Returns(1);

            var services = TestServices.StyledWindow.With(
                windowingPlatform: new MockWindowingPlatform(() => windowImpl.Object));

            using (UnitTestApplication.Start(services))
            {
                ClearOpenWindows();
                var window = new Window();

                window.Show();
                windowImpl.Object.Closed();

                Assert.Empty(Window.OpenWindows);
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
                var renderer = new Mock<IRenderer>();
                var target = new Window(CreateImpl(renderer));

                target.Show();

                renderer.Verify(x => x.Start(), Times.Once);
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
                var windowImpl = new Mock<IWindowImpl>();
                windowImpl.SetupProperty(x => x.Closed);
                windowImpl.Setup(x => x.Scaling).Returns(1);

                var target = new Window(windowImpl.Object);
                var task = target.ShowDialog<bool>();

                windowImpl.Object.Closed();

                var result = await task;
                Assert.False(result);
            }
        }

        [Fact]
        public void Window_Should_Be_Centered_When_WindowStartupLocation_Is_CenterScreen()
        {
            var screen1 = new Mock<Screen>(new Rect(new Size(1920, 1080)), new Rect(new Size(1920, 1040)), true);
            var screen2 = new Mock<Screen>(new Rect(new Size(1366, 768)), new Rect(new Size(1366, 728)), false);

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
                window.Position = new Point(60, 40);

                window.Show();

                var expectedPosition = new Point(
                    screen1.Object.WorkingArea.Size.Width / 2 - window.ClientSize.Width / 2,
                    screen1.Object.WorkingArea.Size.Height / 2 - window.ClientSize.Height / 2);

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
                parentWindow.Position = new Point(60, 40);

                parentWindow.Show();

                using (UnitTestApplication.Start(windowServices))
                {
                    var window = new Window();
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.Position = new Point(60, 40);
                    window.Owner = parentWindow;

                    window.Show();

                    var expectedPosition = new Point(
                        parentWindow.Position.X + parentWindow.ClientSize.Width / 2 - window.ClientSize.Width / 2,
                        parentWindow.Position.Y + parentWindow.ClientSize.Height / 2 - window.ClientSize.Height / 2);

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

        private void ClearOpenWindows()
        {
            // HACK: We really need a decent way to have "statics" that can be scoped to
            // AvaloniaLocator scopes.
            ((IList<Window>)Window.OpenWindows).Clear();
        }
    }
}
