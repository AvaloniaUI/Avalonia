// -----------------------------------------------------------------------
// <copyright file="WindowTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
        public void IsVisible_Should_Be_False_Atfer_Hide()
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
        public void IsVisible_Should_Be_False_Atfer_Close()
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
        public void IsVisible_Should_Be_False_Atfer_Impl_Signals_Close()
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

        private void ClearOpenWindows()
        {
            // HACK: We really need a decent way to have "statics" that can be scoped to
            // AvaloniaLocator scopes.
            ((IList<Window>)Window.OpenWindows).Clear();
        }

        private IWindowImpl CreateImpl(Mock<IRenderer> renderer)
        {
            return Mock.Of<IWindowImpl>(x =>
                x.Scaling == 1 &&
                x.CreateRenderer(It.IsAny<IRenderRoot>()) == renderer.Object);
        }
    }
}
