using System;
using System.ComponentModel;
using System.Linq;

using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.UnitTests;
using Avalonia.VisualTree;

using Moq;

using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class FlyoutTests
    {
        [Fact]
        public void Opening_Raises_Single_Opening_Event()
        {
            using (CreateServicesWithFocus())
            {
                var window = PreparedWindow();
                window.Show();

                int tracker = 0;
                Flyout f = new Flyout();
                f.Opening += (s, e) =>
                {
                    tracker++;
                };
                f.ShowAt(window);

                Assert.Equal(1, tracker);
                Assert.True(f.IsOpen);
            }
        }

        [Fact]
        public void Opening_Raises_Single_Opened_Event()
        {
            using (CreateServicesWithFocus())
            {
                var window = PreparedWindow();
                window.Show();

                int tracker = 0;
                Flyout f = new Flyout();
                f.Opened += (s, e) =>
                {
                    tracker++;
                };
                f.ShowAt(window);

                Assert.Equal(1, tracker);
            }
        }

        [Fact]
        public void Opening_Is_Cancellable()
        {
            using (CreateServicesWithFocus())
            {
                var window = PreparedWindow();
                window.Show();

                int tracker = 0;
                Flyout f = new Flyout();
                f.Opening += (s, e) =>
                {
                    tracker++;
                    if (e is CancelEventArgs cancelEventArgs)
                    {
                        cancelEventArgs.Cancel = true;
                    }
                };
                f.ShowAt(window);

                Assert.Equal(1, tracker);
                Assert.False(f.IsOpen);
            }
        }

        [Fact]
        public void Closing_Raises_Single_Closing_Event()
        {
            using (CreateServicesWithFocus())
            {
                var window = PreparedWindow();
                window.Show();

                int tracker = 0;
                Flyout f = new Flyout();
                f.Closing += (s, e) =>
                {
                    tracker++;
                };
                f.ShowAt(window);
                f.Hide();

                Assert.Equal(1, tracker);
            }
        }

        [Fact]
        public void Closing_Raises_Single_Closed_Event()
        {
            using (CreateServicesWithFocus())
            {
                var window = PreparedWindow();
                window.Show();

                int tracker = 0;
                Flyout f = new Flyout();
                f.Closed += (s, e) =>
                {
                    tracker++;
                };
                f.ShowAt(window);
                f.Hide();

                Assert.Equal(1, tracker);
            }
        }

        [Fact]
        public void Cancel_Closing_Keeps_Flyout_Open()
        {
            using (CreateServicesWithFocus())
            {
                var window = PreparedWindow();
                window.Show();

                var tracker = 0;
                var f = new Flyout();
                f.Closing += (s, e) =>
                {
                    tracker++;
                    e.Cancel = true;
                };
                f.ShowAt(window);
                f.Hide();

                Assert.True(f.IsOpen);
                Assert.Equal(1, tracker);
            }
        }

        [Fact]
        public void Cancel_Light_Dismiss_Closing_Keeps_Flyout_Open()
        {
            using (CreateServicesWithFocus())
            {
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

                window.Show();

                var tracker = 0;
                var f = new Flyout();
                f.Content = new Border { Width = 10, Height = 10 };
                f.Closing += (s, e) =>
                {
                    tracker++;
                    e.Cancel = true;
                };
                f.ShowAt(window);

                var e = CreatePointerPressedEventArgs(window, new Point(90, 90));
                var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);
                overlay.RaiseEvent(e);

                Assert.Equal(1, tracker);
                Assert.True(f.IsOpen);
            }
        }

        [Fact]
        public void Light_Dismiss_Closes_Flyout()
        {
            using (CreateServicesWithFocus())
            {
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

                window.Show();

                var f = new Flyout();
                f.Content = new Border { Width = 10, Height = 10 };
                f.ShowAt(window);

                var e = CreatePointerPressedEventArgs(window, new Point(90, 90));
                var overlay = LightDismissOverlayLayer.GetLightDismissOverlayLayer(window);
                overlay.RaiseEvent(e);

                Assert.False(f.IsOpen);
            }
        }

        [Fact]
        public void Flyout_Has_Uncancellable_Close_Before_Showing_On_A_Different_Target()
        {
            using (CreateServicesWithFocus())
            {
                var window = PreparedWindow();
                Button target1 = new Button();
                Button target2 = new Button();

                window.Content = new StackPanel
                {
                    Children =
                    {
                        target1,
                        target2
                    }
                };
                window.Show();

                bool closingFired = false;
                bool closedFired = false;
                Flyout f = new Flyout();
                f.Closing += (s, e) =>
                {
                    closingFired = true; //This shouldn't happen
                };
                f.Closed += (s, e) =>
                {
                    closedFired = true;
                };

                f.ShowAt(target1);

                f.ShowAt(target2);

                Assert.False(closingFired);
                Assert.True(closedFired);
            }
        }

        [Fact]
        public void ShowMode_Standard_Attemps_Focus_Flyout_Content()
        {
            using (CreateServicesWithFocus())
            {
                var window = PreparedWindow();

                var flyoutTextBox = new TextBox();
                var button = new Button
                {
                    Flyout = new Flyout
                    {
                        ShowMode = FlyoutShowMode.Standard,
                        Content = new Panel
                        {
                            Children =
                            {
                                flyoutTextBox
                            }
                        }
                    }
                };

                window.Content = button;
                window.Show();

                button.Focus();
                Assert.True(window.FocusManager.GetFocusedElement() == button);
                button.Flyout.ShowAt(button);
                Assert.False(button.IsFocused);
                Assert.True(window.FocusManager.GetFocusedElement() == flyoutTextBox);
            }
        }

        [Fact]
        public void ShowMode_Transient_Does_Not_Move_Focus_From_Target()
        {
            using (CreateServicesWithFocus())
            {
                var window = PreparedWindow();

                var flyoutTextBox = new TextBox();
                var button = new Button
                {
                    Flyout = new Flyout
                    {
                        ShowMode = FlyoutShowMode.Transient,
                        Content = new Panel
                        {
                            Children =
                            {
                                flyoutTextBox
                            }
                        }
                    },
                    Content = "Test"
                };

                window.Content = button;
                window.Show();

                button.Focus();
                Assert.True(window.FocusManager.GetFocusedElement() == button);
                button.Flyout.ShowAt(button);
                Assert.True(window.FocusManager.GetFocusedElement() == button);
            }
        }

        [Fact]
        public void ContextRequested_Opens_ContextFlyout()
        {
            using (CreateServicesWithFocus())
            {
                var flyout = new Flyout();
                var target = new Panel
                {
                    ContextFlyout = flyout
                };

                var window = PreparedWindow(target);
                window.Show();

                int openedCount = 0;

                flyout.Opened += (sender, args) =>
                {
                    openedCount++;
                };

                target.RaiseEvent(new ContextRequestedEventArgs());

                Assert.True(flyout.IsOpen);
                Assert.Equal(1, openedCount);
            }
        }

        [Fact]
        public void KeyUp_Raised_On_Target_Opens_ContextFlyout()
        {
            using (CreateServicesWithFocus())
            {
                var flyout = new Flyout();
                var target = new Panel
                {
                    ContextFlyout = flyout
                };
                var contextRequestedCount = 0;
                target.AddHandler(Control.ContextRequestedEvent, (s, a) => contextRequestedCount++, Interactivity.RoutingStrategies.Tunnel);

                var window = PreparedWindow(target);
                window.Show();

                target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Apps, Source = window });

                Assert.True(flyout.IsOpen);
                Assert.Equal(1, contextRequestedCount);
            }
        }

        [Fact]
        public void KeyUp_Raised_On_Target_Closes_Opened_ContextFlyout()
        {
            using (CreateServicesWithFocus())
            {
                var flyout = new Flyout();
                var target = new Panel
                {
                    ContextFlyout = flyout
                };

                var window = PreparedWindow(target);
                window.Show();

                target.RaiseEvent(new ContextRequestedEventArgs());

                Assert.True(flyout.IsOpen);

                target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Apps, Source = window });

                Assert.False(flyout.IsOpen);
            }
        }

        [Fact]
        public void KeyUp_Raised_On_Flyout_Closes_Opened_ContextFlyout()
        {
            using (CreateServicesWithFocus())
            {
                var flyoutContent = new Button();
                var flyout = new Flyout()
                {
                    Content = flyoutContent
                };
                var target = new Panel
                {
                    ContextFlyout = flyout
                };

                var window = PreparedWindow(target);
                window.Show();

                target.RaiseEvent(new ContextRequestedEventArgs());

                Assert.True(flyout.IsOpen);

                flyoutContent.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = Key.Apps, Source = window });

                Assert.False(flyout.IsOpen);
            }
        }

        [Fact]
        public void Should_Reset_Popup_Parent_On_Target_Detached()
        {
            using (CreateServicesWithFocus())
            {
                var userControl = new UserControl();
                var window = PreparedWindow(userControl);
                window.Show();
                
                var flyout = new TestFlyout();
                flyout.ShowAt(userControl);
                
                var popup = Assert.IsType<Popup>(flyout.Popup);
                Assert.NotNull(popup.Parent);
                
                window.Content = null;
                Assert.Null(popup.Parent);
            }
        }
        
        [Fact]
        public void Should_Reset_Popup_Parent_On_Target_Attach_Following_Detach()
        {
            using (CreateServicesWithFocus())
            {
                var userControl = new UserControl();
                var window = PreparedWindow(userControl);
                window.Show();
                
                var flyout = new TestFlyout();
                flyout.ShowAt(userControl);
                
                var popup = Assert.IsType<Popup>(flyout.Popup);
                Assert.NotNull(popup.Parent);
                
                flyout.Hide();
                
                flyout.ShowAt(userControl);
                Assert.NotNull(popup.Parent);
            }
        }

        [Fact]
        public void ContextFlyout_Can_Be_Set_In_Styles()
        {
            using (CreateServicesWithFocus())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='TextBlock'>
            <Setter Property='ContextFlyout'>
                <MenuFlyout>
                    <MenuItem>Foo</MenuItem>
                </MenuFlyout>
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

                Assert.NotNull(target1.ContextFlyout);
                Assert.NotNull(target2.ContextFlyout);
                Assert.Same(target1.ContextFlyout, target2.ContextFlyout);

                window.Show();

                var menu = target1.ContextFlyout;
                mouse.Click(target1, MouseButton.Right);
                Assert.True(menu.IsOpen);
                mouse.Click(target2, MouseButton.Right);
                Assert.True(menu.IsOpen);
            }
        }

        [Fact]
        public void Setting_FlyoutPresenterClasses_Sets_Classes_On_FlyoutPresenter()
        {
            using (CreateServicesWithFocus())
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='FlyoutPresenter.TestClass'>
            <Setter Property='Background' Value='Red' />
        </Style>
	</Window.Styles>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var flyoutPanel = new Panel();
                var button = new Button
                {
                    Content = "Test",
                    Flyout = new Flyout
                    {
                        Content = flyoutPanel
                    }
                };
                window.Content = button;
                window.Show();

                (button.Flyout as Flyout).FlyoutPresenterClasses.Add("TestClass");

                button.Flyout.ShowAt(button);

                var presenter = flyoutPanel.GetVisualAncestors().OfType<FlyoutPresenter>().FirstOrDefault();
                Assert.NotNull(presenter);
                Assert.True((presenter.Background as ISolidColorBrush).Color == Colors.Red);
            }
        }

        private static IDisposable CreateServicesWithFocus()
        {
            return UnitTestApplication.Start(TestServices.StyledWindow.With(windowingPlatform:
                new MockWindowingPlatform(null,
                    x =>
                    {
                        return MockWindowingPlatform.CreatePopupMock(x).Object;
                    }),
                    focusManager: new FocusManager(),
                    keyboardDevice: () => new KeyboardDevice()));
        }

        private static Window PreparedWindow(object content = null)
        {
            var platform = AvaloniaLocator.Current.GetRequiredService<IWindowingPlatform>();
            var windowImpl = Mock.Get(platform.CreateWindow());
            windowImpl.Setup(x => x.Compositor).Returns(RendererMocks.CreateDummyCompositor());

            var w = new Window(windowImpl.Object) { Content = content };
            w.ApplyTemplate();
            return w;
        }

        private static PointerPressedEventArgs CreatePointerPressedEventArgs(Window source, Point p)
        {
            var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            return new PointerPressedEventArgs(
                source,
                pointer,
                source,
                p,
                0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None);
        }
        
        public class TestFlyout : Flyout
        {
            public new Popup Popup => base.Popup;
        }
    }
}
