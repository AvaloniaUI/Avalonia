using System;
using System.Linq;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
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

                int tracker = 0;
                Flyout f = new Flyout();
                f.Closing += (s, e) =>
                {
                    e.Cancel = true;
                };
                f.ShowAt(window);
                f.Hide();

                Assert.True(f.IsOpen);
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
                Assert.True(FocusManager.Instance?.Current == button);
                button.Flyout.ShowAt(button);
                Assert.False(button.IsFocused);
                Assert.True(FocusManager.Instance?.Current == flyoutTextBox);
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

                FocusManager.Instance?.Focus(button);
                Assert.True(FocusManager.Instance?.Current == button);
                button.Flyout.ShowAt(button);
                Assert.True(FocusManager.Instance?.Current == button);
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

        private IDisposable CreateServicesWithFocus()
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

        private Window PreparedWindow(object content = null)
        {
            var w = new Window { Content = content };
            w.ApplyTemplate();
            return w;
        }
    }
}
