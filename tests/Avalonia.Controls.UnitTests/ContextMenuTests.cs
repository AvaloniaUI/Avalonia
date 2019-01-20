﻿using System;
using System.Windows.Input;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Data;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ContextMenuTests
    {
        private Mock<IPopupImpl> popupImpl;

        [Fact]
        public void Clicking_On_Control_Toggles_ContextMenu()
        {
            using (Application())
            {
                popupImpl.Setup(x => x.Show()).Verifiable();
                popupImpl.Setup(x => x.Hide()).Verifiable();

                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };

                new Window { Content = target };

                target.RaiseEvent(new PointerReleasedEventArgs
                {
                    RoutedEvent = InputElement.PointerReleasedEvent,
                    MouseButton = MouseButton.Right
                });

                Assert.True(sut.IsOpen);

                target.RaiseEvent(new PointerReleasedEventArgs
                {
                    RoutedEvent = InputElement.PointerReleasedEvent,
                    MouseButton = MouseButton.None
                });

                Assert.False(sut.IsOpen);
                popupImpl.Verify(x => x.Show(), Times.Once);
                popupImpl.Verify(x => x.Hide(), Times.Once);
            }
        }

        [Fact]
        public void Right_Clicking_On_Control_Twice_Re_Opens_ContextMenu()
        {
            using (Application())
            {
                popupImpl.Setup(x => x.Show()).Verifiable();
                popupImpl.Setup(x => x.Hide()).Verifiable();

                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };

                var window = new Window { Content = target };

                Avalonia.Application.Current.MainWindow = window;

                target.RaiseEvent(new PointerReleasedEventArgs
                {
                    RoutedEvent = InputElement.PointerReleasedEvent,
                    MouseButton = MouseButton.Right
                });

                Assert.True(sut.IsOpen);

                target.RaiseEvent(new PointerReleasedEventArgs
                {
                    RoutedEvent = InputElement.PointerReleasedEvent,
                    MouseButton = MouseButton.Right
                });

                Assert.True(sut.IsOpen);
                popupImpl.Verify(x => x.Hide(), Times.Once);
                popupImpl.Verify(x => x.Show(), Times.Exactly(2));
            }
        }

        [Fact]
        public void Cancelling_Opening_Does_Not_Show_ContextMenu()
        {
            using (Application())
            {
                popupImpl.Setup(x => x.Show()).Verifiable();

                bool eventCalled = false;
                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };
                new Window { Content = target };

                sut.ContextMenuOpening += (c, e) => { eventCalled = true; e.Cancel = true; };

                target.RaiseEvent(new PointerReleasedEventArgs
                {
                    RoutedEvent = InputElement.PointerReleasedEvent,
                    MouseButton = MouseButton.Right
                });

                Assert.True(eventCalled);
                Assert.False(sut.IsOpen);
                popupImpl.Verify(x => x.Show(), Times.Never);
            }
        }

        [Fact]
        public void Cancelling_Closing_Leaves_ContextMenuOpen()
        {
            using (Application())
            {
                popupImpl.Setup(x => x.Show()).Verifiable();
                popupImpl.Setup(x => x.Hide()).Verifiable();

                bool eventCalled = false;
                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };
                new Window { Content = target };

                sut.ContextMenuClosing += (c, e) => { eventCalled = true; e.Cancel = true; };

                target.RaiseEvent(new PointerReleasedEventArgs
                {
                    RoutedEvent = InputElement.PointerReleasedEvent,
                    MouseButton = MouseButton.Right
                });

                Assert.True(sut.IsOpen);

                target.RaiseEvent(new PointerReleasedEventArgs
                {
                    RoutedEvent = InputElement.PointerReleasedEvent,
                    MouseButton = MouseButton.None
                });

                Assert.True(eventCalled);
                Assert.True(sut.IsOpen);

                popupImpl.Verify(x => x.Show(), Times.Once());
                popupImpl.Verify(x => x.Hide(), Times.Never);
            }
        }

        private IDisposable Application()
        {
            var screen = new PixelRect(new PixelPoint(), new PixelSize(100, 100));
            var screenImpl = new Mock<IScreenImpl>();
            screenImpl.Setup(x => x.ScreenCount).Returns(1);
            screenImpl.Setup(X => X.AllScreens).Returns( new[] { new Screen(screen, screen, true) });

            var windowImpl = new Mock<IWindowImpl>();
            windowImpl.Setup(x => x.Screen).Returns(screenImpl.Object);

            popupImpl = new Mock<IPopupImpl>();
            popupImpl.SetupGet(x => x.Scaling).Returns(1);

            var services = TestServices.StyledWindow.With(
                                        inputManager: new InputManager(),
                                        windowImpl: windowImpl.Object,
                                        windowingPlatform: new MockWindowingPlatform(() => windowImpl.Object, () => popupImpl.Object));

            return UnitTestApplication.Start(services);
        }
    }
}
