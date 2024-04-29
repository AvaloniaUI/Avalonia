using System;

using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.UnitTests;

using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ContextMenuTests
    {
        private Mock<IPopupImpl> popupImpl;
        private MouseTestHelper _mouse = new MouseTestHelper();

        [Fact]
        public void ContextRequested_Opens_ContextMenu()
        {
            using (Application())
            {
                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };

                var window = new Window { Content = target };
                window.ApplyStyling();
                window.ApplyTemplate();
                ((Control)window.Presenter).ApplyTemplate();

                int openedCount = 0;

                sut.Opened += (sender, args) =>
                {
                    openedCount++;
                };

                target.RaiseEvent(new ContextRequestedEventArgs());

                Assert.True(sut.IsOpen);
                Assert.Equal(1, openedCount);
            }
        }

        [Fact]
        public void ContextMenu_Is_Opened_When_ContextFlyout_Is_Also_Set()
        {
            // We have this test for backwards compatability with the code that already sets custom ContextMenu.
            using (Application())
            {
                var sut = new ContextMenu();
                var flyout = new Flyout();
                var target = new Panel
                {
                    ContextMenu = sut,
                    ContextFlyout = flyout
                };

                var window = new Window { Content = target };
                window.ApplyStyling();
                window.ApplyTemplate();
                ((Control)window.Presenter).ApplyTemplate();

                target.RaiseEvent(new ContextRequestedEventArgs());

                Assert.True(sut.IsOpen);
                Assert.False(flyout.IsOpen);
            }
        }

        [Fact]
        public void KeyUp_Raised_On_Target_Opens_ContextFlyout()
        {
            using (Application())
            {
                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };
                var contextRequestedCount = 0;
                target.AddHandler(Control.ContextRequestedEvent, (s, a) => contextRequestedCount++, Interactivity.RoutingStrategies.Tunnel);

                var window = PreparedWindow(target);
                window.Show();

                target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Apps, Source = window });

                Assert.True(sut.IsOpen);
                Assert.Equal(1, contextRequestedCount);
            }
        }

        [Fact]
        public void KeyUp_Raised_On_Flyout_Closes_Opened_ContextMenu()
        {
            using (Application())
            {
                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };

                var window = PreparedWindow(target);
                window.Show();

                target.RaiseEvent(new ContextRequestedEventArgs());

                Assert.True(sut.IsOpen);

                sut.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Apps, Source = window });

                Assert.False(sut.IsOpen);
            }
        }

        [Fact]
        public void Opening_Raises_Single_Opened_Event()
        {
            using (Application())
            {
                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };

                var window = new Window { Content = target };
                window.ApplyStyling();
                window.ApplyTemplate();
                ((Control)window.Presenter).ApplyTemplate();

                int openedCount = 0;

                sut.Opened += (sender, args) =>
                {
                    openedCount++;
                };

                sut.Open(target);

                Assert.Equal(1, openedCount);
            }
        }

        [Fact]
        public void Open_Should_Use_Default_Control()
        {
            using (Application())
            {
                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };

                var window = new Window { Content = target };
                window.ApplyStyling();
                window.ApplyTemplate();
                ((Control)window.Presenter).ApplyTemplate();

                bool opened = false;

                sut.Opened += (sender, args) =>
                {
                    opened = true;
                };

                sut.Open();

                Assert.True(opened);
            }
        }

        [Fact]
        public void Open_Should_Raise_Exception_If_AlreadyDetached()
        {
            using (Application())
            {
                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };

                var window = new Window { Content = target };
                window.ApplyStyling();
                window.ApplyTemplate();
                ((Control)window.Presenter).ApplyTemplate();

                target.ContextMenu = null;

               Assert.ThrowsAny<Exception>(()=> sut.Open());
            }
        }

        [Fact]
        public void Closing_Raises_Single_Closed_Event()
        {
            using (Application())
            {
                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };

                var window = new Window { Content = target };
                window.ApplyStyling();
                window.ApplyTemplate();
                ((Control)window.Presenter).ApplyTemplate();

                sut.Open(target);

                int closedCount = 0;

                sut.Closed += (sender, args) =>
                {
                    closedCount++;
                };

                sut.Close();

                Assert.Equal(1, closedCount);
            }
        }

        [Fact]
        public void Cancel_Light_Dismiss_Closing_Keeps_Flyout_Open()
        {
            using (Application())
            {
                popupImpl.Setup(x => x.Show(true, false)).Verifiable();
                popupImpl.Setup(x => x.Hide()).Verifiable();

                var window = PreparedWindow();
                window.Width = 100;
                window.Height = 100;

                var button = new Button
                {
                    Height = 10,
                    Width = 10,
                    HorizontalAlignment = Layout.HorizontalAlignment.Left,
                    VerticalAlignment = Layout.VerticalAlignment.Top
                };
                window.Content = button;

                window.ApplyTemplate();
                window.Show();

                var tracker = 0;

                var c = new ContextMenu();
                c.Closing += (s, e) =>
                {
                    tracker++;
                    e.Cancel = true;
                };
                button.ContextMenu = c;
                c.Open(button);

                var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);
                _mouse.Down(overlay, MouseButton.Left, new Point(90, 90));
                _mouse.Up(button, MouseButton.Left, new Point(90, 90));

                Assert.Equal(1, tracker);
                Assert.True(c.IsOpen);

                popupImpl.Verify(x => x.Hide(), Times.Never);
                popupImpl.Verify(x => x.Show(true, false), Times.Exactly(1));
            }
        }

        [Fact]
        public void Light_Dismiss_Closes_Flyout()
        {
            using (Application())
            {
                popupImpl.Setup(x => x.Show(true, false)).Verifiable();
                popupImpl.Setup(x => x.Hide()).Verifiable();

                var window = PreparedWindow();
                window.Width = 100;
                window.Height = 100;

                var button = new Button
                {
                    Height = 10,
                    Width = 10,
                    HorizontalAlignment = Layout.HorizontalAlignment.Left,
                    VerticalAlignment = Layout.VerticalAlignment.Top
                };
                window.Content = button;

                window.ApplyTemplate();
                window.Show();

                var c = new ContextMenu();
                c.Placement = PlacementMode.Bottom;
                c.Open(button);

                var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);
                _mouse.Down(overlay, MouseButton.Left, new Point(90, 90));
                _mouse.Up(button, MouseButton.Left, new Point(90, 90));

                Assert.False(c.IsOpen);
                popupImpl.Verify(x => x.Hide(), Times.Exactly(1));
                popupImpl.Verify(x => x.Show(true, false), Times.Exactly(1));
            }
        }

        [Fact]
        public void Clicking_On_Control_Toggles_ContextMenu()
        {
            using (Application())
            {
                popupImpl.Setup(x => x.Show(true, false)).Verifiable();
                popupImpl.Setup(x => x.Hide()).Verifiable();

                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };

                var window = PreparedWindow(target);
                window.Show();
                var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);

                _mouse.Click(target, MouseButton.Right);

                Assert.True(sut.IsOpen);

                _mouse.Down(overlay);
                _mouse.Up(target);

                Assert.False(sut.IsOpen);
                popupImpl.Verify(x => x.Show(true, false), Times.Once);
                popupImpl.Verify(x => x.Hide(), Times.Once);
            }
        }

        [Fact]
        public void Right_Clicking_On_Control_Twice_Re_Opens_ContextMenu()
        {
            using (Application())
            {
                popupImpl.Setup(x => x.Show(true, false)).Verifiable();
                popupImpl.Setup(x => x.Hide()).Verifiable();

                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };

                var window = PreparedWindow(target);
                window.Show();

                var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);

                _mouse.Click(target, MouseButton.Right);
                Assert.True(sut.IsOpen);

                _mouse.Down(overlay, MouseButton.Right);
                _mouse.Up(target, MouseButton.Right);

                Assert.True(sut.IsOpen);
                popupImpl.Verify(x => x.Hide(), Times.Once);
                popupImpl.Verify(x => x.Show(true, false), Times.Exactly(2));
            }
        }
        
        [Fact]
        public void Context_Menu_Can_Be_Shared_Between_Controls_Even_After_A_Control_Is_Removed_From_Visual_Tree()
        {
            using (Application())
            {
                var sut = new ContextMenu();
                var target1 = new Panel
                {
                    ContextMenu = sut
                };

                var target2 = new Panel
                {
                    ContextMenu = sut
                };

                var sp = new StackPanel { Children = { target1, target2 } };
                var window = new Window { Content = sp };

                window.ApplyStyling();
                window.ApplyTemplate();
                ((Control)window.Presenter).ApplyTemplate();

                _mouse.Click(target1, MouseButton.Right);

                Assert.True(sut.IsOpen);

                sp.Children.Remove(target1);

                Assert.False(sut.IsOpen);

                _mouse.Click(target2, MouseButton.Right);
                
                Assert.True(sut.IsOpen);
            }
        }

        [Fact]
        public void Cancelling_Opening_Does_Not_Show_ContextMenu()
        {
            using (Application())
            {
                popupImpl.Setup(x => x.Show(true, false)).Verifiable();

                bool eventCalled = false;
                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };
                new Window { Content = target };

                sut.Opening += (c, e) => { eventCalled = true; e.Cancel = true; };

                _mouse.Click(target, MouseButton.Right);

                Assert.True(eventCalled);
                Assert.False(sut.IsOpen);
                popupImpl.Verify(x => x.Show(true, false), Times.Never);
            }
        }

        [Fact]
        public void Can_Set_Clear_ContextMenu_Property()
        {
            using (Application())
            {
                var target = new ContextMenu();
                var control = new Panel();

                control.ContextMenu = target;
                control.ContextMenu = null;
            }
        }

        [Fact]
        public void Should_Reset_Popup_Parent_On_Target_Detached()
        {
            using (Application())
            {
                var userControl = new UserControl();
                var window = PreparedWindow(userControl);
                window.Show();
                
                var menu = new ContextMenu();
                userControl.ContextMenu = menu;
                menu.Open();
                
                var popup = Assert.IsType<Popup>(menu.Parent);
                Assert.NotNull(popup.Parent);
                
                window.Content = null;
                Assert.Null(popup.Parent);
            }
        }

        [Fact]
        public void Context_Menu_In_Resources_Can_Be_Shared()
        {
            using (Application())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <ContextMenu x:Key='contextMenu'>
            <MenuItem>Foo</MenuItem>
        </ContextMenu>
	</Window.Resources>

    <StackPanel>
        <TextBlock Name='target1' ContextMenu='{StaticResource contextMenu}'/>
        <TextBlock Name='target2' ContextMenu='{StaticResource contextMenu}'/>
    </StackPanel>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target1 = window.Find<TextBlock>("target1");
                var target2 = window.Find<TextBlock>("target2");
                var mouse = new MouseTestHelper();

                Assert.NotNull(target1.ContextMenu);
                Assert.NotNull(target2.ContextMenu);
                Assert.Same(target1.ContextMenu, target2.ContextMenu);

                window.Show();

                var menu = target1.ContextMenu;
                mouse.Click(target1, MouseButton.Right);
                Assert.True(menu.IsOpen);
                mouse.Click(target2, MouseButton.Right);
                Assert.True(menu.IsOpen);
            }
        }

        [Fact]
        public void Context_Menu_Can_Be_Set_In_Style()
        {
            using (Application())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='TextBlock'>
            <Setter Property='ContextMenu'>
                <ContextMenu>
                    <MenuItem>Foo</MenuItem>
                </ContextMenu>
            </Setter>
        </Style>
	</Window.Styles>

    <StackPanel>
        <TextBlock Name='target1'/>
        <TextBlock Name='target2'/>
    </StackPanel>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var target1 = window.Find<TextBlock>("target1");
                var target2 = window.Find<TextBlock>("target2");
                var mouse = new MouseTestHelper();

                Assert.NotNull(target1.ContextMenu);
                Assert.NotNull(target2.ContextMenu);
                Assert.Same(target1.ContextMenu, target2.ContextMenu);

                window.Show();

                var menu = target1.ContextMenu;
                mouse.Click(target1, MouseButton.Right);
                Assert.True(menu.IsOpen);
                mouse.Click(target2, MouseButton.Right);
                Assert.True(menu.IsOpen);
            }
        }

        [Fact]
        public void Cancelling_Closing_Leaves_ContextMenuOpen()
        {
            using (Application())
            {
                popupImpl.Setup(x => x.Show(true, false)).Verifiable();
                popupImpl.Setup(x => x.Hide()).Verifiable();

                bool eventCalled = false;
                var sut = new ContextMenu();
                var target = new Panel
                {
                    ContextMenu = sut
                };

                var window = PreparedWindow(target);
                var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);

                sut.Closing += (c, e) => { eventCalled = true; e.Cancel = true; };

                window.Show();

                _mouse.Click(target, MouseButton.Right);

                Assert.True(sut.IsOpen);

                _mouse.Down(overlay, MouseButton.Right);
                _mouse.Up(target, MouseButton.Right);

                Assert.True(eventCalled);
                Assert.True(sut.IsOpen);

                popupImpl.Verify(x => x.Show(true, false), Times.Once());
                popupImpl.Verify(x => x.Hide(), Times.Never);
            }
        }

        [Fact]
        public void Closing_Should_Restore_Focus()
        {
            using (Application())
            {
                popupImpl.Setup(x => x.Show(true, false)).Verifiable();
                popupImpl.Setup(x => x.Hide()).Verifiable();

                var item = new MenuItem();
                var sut = new ContextMenu
                {
                    Items = { item }
                };

                var button = new Button();
                var target = new Panel
                {
                    Children =
                    {
                        button,
                    },
                    ContextMenu = sut
                };

                var window = PreparedWindow(target);
                var focusManager = Assert.IsType<FocusManager>(window.FocusManager);

                // Show the window and focus the button.
                window.Show();
                button.Focus();
                Assert.Same(button, focusManager.GetFocusedElement());

                // Click to show the context menu.
                _mouse.Click(target, MouseButton.Right);
                Assert.True(sut.IsOpen);

                // Hover over the context menu item: this should focus it.
                _mouse.Enter(item);
                Assert.Same(item, focusManager.GetFocusedElement());

                // Click the menu item to close the menu.
                _mouse.Click(item);
                Assert.False(sut.IsOpen);

                // Focus should be restored to the button.
                Assert.Same(button, focusManager.GetFocusedElement());
            }
        }

        private static Window PreparedWindow(object content = null)
        {
            
            var platform = AvaloniaLocator.Current.GetRequiredService<IWindowingPlatform>();
            var windowImpl = Mock.Get(platform.CreateWindow());
            windowImpl.Setup(x => x.Compositor).Returns(RendererMocks.CreateDummyCompositor());

            var w = new Window(windowImpl.Object) { Content = content };
            w.ApplyStyling();
            w.ApplyTemplate();
            ((Control)w.Presenter).ApplyTemplate();
            return w;
        }

        private IDisposable Application()
        {
            var screen = new PixelRect(new PixelPoint(), new PixelSize(100, 100));
            var screenImpl = new Mock<IScreenImpl>();
            screenImpl.Setup(x => x.ScreenCount).Returns(1);
            screenImpl.Setup(X => X.AllScreens).Returns( new[] { new Screen(1, screen, screen, true) });

            var windowImpl = MockWindowingPlatform.CreateWindowMock();
            popupImpl = MockWindowingPlatform.CreatePopupMock(windowImpl.Object);
            popupImpl.SetupGet(x => x.RenderScaling).Returns(1);
            windowImpl.Setup(x => x.CreatePopup()).Returns(popupImpl.Object);

            windowImpl.Setup(x => x.Screen).Returns(screenImpl.Object);

            var services = TestServices.StyledWindow.With(
                                        focusManager: new FocusManager(),
                                        keyboardDevice: () => new KeyboardDevice(),
                                        inputManager: new InputManager(),
                                        windowImpl: windowImpl.Object,
                                        windowingPlatform: new MockWindowingPlatform(() => windowImpl.Object, x => popupImpl.Object));

            return UnitTestApplication.Start(services);
        }
    }
}
