using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class MenuItemTests
    {
        private Mock<IPopupImpl> popupImpl;

        [Fact]
        public void Header_Of_Minus_Should_Apply_Separator_Pseudoclass()
        {
            var target = new MenuItem { Header = "-" };

            Assert.True(target.Classes.Contains(":separator"));
        }

        [Fact]
        public void Separator_Item_Should_Set_Focusable_False()
        {
            var target = new MenuItem { Header = "-" };

            Assert.False(target.Focusable);
        }


        [Fact]
        public void MenuItem_Is_Disabled_When_Command_Is_Enabled_But_IsEnabled_Is_False()
        {
            var command = new TestCommand(true);
            var target = new MenuItem
            {
                IsEnabled = false,
                Command = command,
            };

            var root = new TestRoot { Child = target };

            Assert.False(((IInputElement)target).IsEffectivelyEnabled);
        }

        [Fact]
        public void MenuItem_Is_Disabled_When_Bound_Command_Doesnt_Exist()
        {
            var target = new MenuItem
            {
                [!MenuItem.CommandProperty] = new Binding("Command"),
            };

            Assert.True(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);
        }

        [Fact]
        public void MenuItem_Is_Disabled_When_Bound_Command_Is_Removed()
        {
            var viewModel = new
            {
                Command = new TestCommand(true),
            };

            var target = new MenuItem
            {
                DataContext = viewModel,
                [!MenuItem.CommandProperty] = new Binding("Command"),
            };

            Assert.True(target.IsEnabled);
            Assert.True(target.IsEffectivelyEnabled);

            target.DataContext = null;

            Assert.True(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);
        }

        [Fact]
        public void MenuItem_Is_Enabled_When_Added_To_Logical_Tree_And_Bound_Command_Is_Added()
        {
            var viewModel = new
            {
                Command = new TestCommand(true),
            };

            var target = new MenuItem
            {
                DataContext = new object(),
                [!MenuItem.CommandProperty] = new Binding("Command"),
            };
            var root = new TestRoot { Child = target };
                
            Assert.True(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);

            target.DataContext = viewModel;

            Assert.True(target.IsEnabled);
            Assert.True(target.IsEffectivelyEnabled);
        }

        [Fact]
        public void MenuItem_Is_Disabled_When_Disabled_Bound_Command_Is_Added()
        {
            var viewModel = new
            {
                Command = new TestCommand(false),
            };

            var target = new MenuItem
            {
                DataContext = new object(),
                [!MenuItem.CommandProperty] = new Binding("Command"),
            };

            Assert.True(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);

            target.DataContext = viewModel;

            Assert.True(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);
        }

        [Fact]
        public void MenuItem_Does_Not_Subscribe_To_Command_CanExecuteChanged_Until_Added_To_Logical_Tree()
        {
            var command = new TestCommand();
            var target = new MenuItem
            {
                Command = command,
            };

            Assert.Equal(0, command.SubscriptionCount);
        }

        [Fact]
        public void MenuItem_Subscribes_To_Command_CanExecuteChanged_When_Added_To_Logical_Tree()
        {
            var command = new TestCommand();
            var target = new MenuItem { Command = command };
            var root = new TestRoot { Child = target };

            Assert.Equal(1, command.SubscriptionCount);
        }

        [Fact]
        public void MenuItem_Unsubscribes_From_Command_CanExecuteChanged_When_Removed_From_Logical_Tree()
        {
            var command = new TestCommand();
            var target = new MenuItem { Command = command };
            var root = new TestRoot { Child = target };

            root.Child = null;
            Assert.Equal(0, command.SubscriptionCount);
        }
        
        [Fact]
        public void MenuItem_Invokes_CanExecute_When_Added_To_Logical_Tree_And_CommandParameter_Changed()
        {
            var command = new TestCommand(p => p is bool value && value);
            var target = new MenuItem { Command = command };
            var root = new TestRoot { Child = target };

            target.CommandParameter = true;
            Assert.True(target.IsEffectivelyEnabled);

            target.CommandParameter = false;
            Assert.False(target.IsEffectivelyEnabled);
        }
        
        [Fact]
        public void MenuItem_Does_Not_Invoke_CanExecute_When_ContextMenu_Closed()
        {
            using (Application())
            {
                var canExecuteCallCount = 0;
                var command = new TestCommand(_ =>
                {
                    canExecuteCallCount++;
                    return true;
                });
                var target = new MenuItem();
                var contextMenu = new ContextMenu { Items = new AvaloniaList<MenuItem> { target } };
                var window = new Window { Content = new Panel { ContextMenu = contextMenu } };
                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();
                
                Assert.True(target.IsEffectivelyEnabled);
                target.Command = command;
                Assert.Equal(0, canExecuteCallCount);

                target.CommandParameter = false;
                Assert.Equal(0, canExecuteCallCount);

                command.RaiseCanExecuteChanged();
                Assert.Equal(0, canExecuteCallCount);
                
                contextMenu.Open();
                Assert.Equal(2, canExecuteCallCount);//2 because popup is changing logical child

                command.RaiseCanExecuteChanged();
                Assert.Equal(3, canExecuteCallCount);

                target.CommandParameter = true;
                Assert.Equal(4, canExecuteCallCount);
            }
        }
        
        [Fact]
        public void MenuItem_Does_Not_Invoke_CanExecute_When_MenuFlyout_Closed()
        {
            using (Application())
            {
                var canExecuteCallCount = 0;
                var command = new TestCommand(_ =>
                {
                    canExecuteCallCount++;
                    return true;
                });
                var target = new MenuItem();
                var flyout = new MenuFlyout { Items = new AvaloniaList<MenuItem> { target } };
                var button = new Button { Flyout = flyout };
                var window = new Window { Content = button };
                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();
                
                Assert.True(target.IsEffectivelyEnabled);
                target.Command = command;
                Assert.Equal(0, canExecuteCallCount);

                target.CommandParameter = false;
                Assert.Equal(0, canExecuteCallCount);

                command.RaiseCanExecuteChanged();
                Assert.Equal(0, canExecuteCallCount);

                flyout.ShowAt(button);
                Assert.Equal(2, canExecuteCallCount);

                command.RaiseCanExecuteChanged();
                Assert.Equal(3, canExecuteCallCount);

                target.CommandParameter = true;
                Assert.Equal(4, canExecuteCallCount);
            }
        }
        
        [Fact]
        public void MenuItem_Does_Not_Invoke_CanExecute_When_Parent_MenuItem_Closed()
        {
            using (Application())
            {
                var canExecuteCallCount = 0;
                var command = new TestCommand(_ =>
                {
                    canExecuteCallCount++;
                    return true;
                });
                var target = new MenuItem();
                var parentMenuItem = new MenuItem { Items = new AvaloniaList<MenuItem> { target } };
                var contextMenu = new ContextMenu { Items = new AvaloniaList<MenuItem> { parentMenuItem } };
                var window = new Window { Content = new Panel { ContextMenu = contextMenu } };
                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();
                contextMenu.Open();
                
                Assert.True(target.IsEffectivelyEnabled);
                target.Command = command;
                Assert.Equal(0, canExecuteCallCount);

                target.CommandParameter = false;
                Assert.Equal(0, canExecuteCallCount);

                command.RaiseCanExecuteChanged();
                Assert.Equal(0, canExecuteCallCount);

                try
                {
                    parentMenuItem.IsSubMenuOpen = true;
                }
                catch (InvalidOperationException)
                {
                    //popup host creation failed exception
                }
                Assert.Equal(1, canExecuteCallCount);

                command.RaiseCanExecuteChanged();
                Assert.Equal(2, canExecuteCallCount);

                target.CommandParameter = true;
                Assert.Equal(3, canExecuteCallCount);
            }
        }


        [Fact]
        public void TemplatedParent_Should_Not_Be_Applied_To_Submenus()
        {
            using (Application())
            {
                MenuItem topLevelMenu;
                MenuItem childMenu1;
                MenuItem childMenu2;
                var menu = new Menu
                {
                    Items = new[]
                    {
                        (topLevelMenu = new MenuItem
                        {
                            Header = "Foo",
                            Items = new[]
                            {
                                (childMenu1 = new MenuItem { Header = "Bar" }),
                                (childMenu2 = new MenuItem { Header = "Baz" }),
                            }
                        }),
                    }
                };

                var window = new Window { Content = menu };
                window.LayoutManager.ExecuteInitialLayoutPass();

                topLevelMenu.IsSubMenuOpen = true;

                Assert.True(childMenu1.IsAttachedToVisualTree);
                Assert.Null(childMenu1.TemplatedParent);
                Assert.Null(childMenu2.TemplatedParent);

                topLevelMenu.IsSubMenuOpen = false;
                topLevelMenu.IsSubMenuOpen = true;

                Assert.Null(childMenu1.TemplatedParent);
                Assert.Null(childMenu2.TemplatedParent);
            }
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
                inputManager: new InputManager(),
                windowImpl: windowImpl.Object,
                windowingPlatform: new MockWindowingPlatform(() => windowImpl.Object, x => popupImpl.Object));

            return UnitTestApplication.Start(services);
        }

        private class TestCommand : ICommand
        {
            private readonly Func<object, bool> _canExecute;
            private readonly Action<object> _execute;
            private EventHandler _canExecuteChanged;

            public TestCommand(bool enabled = true)
                : this(_ => enabled, _ => { })
            {
            }

            public TestCommand(Func<object, bool> canExecute, Action<object> execute = null)
            {
                _canExecute = canExecute;
                _execute = execute ?? (_ => { });
            }

            public int SubscriptionCount { get; private set; }

            public event EventHandler CanExecuteChanged
            {
                add { _canExecuteChanged += value; ++SubscriptionCount; }
                remove { _canExecuteChanged -= value; --SubscriptionCount; }
            }

            public bool CanExecute(object parameter) => _canExecute(parameter);

            public void Execute(object parameter) => _execute(parameter);

            public void RaiseCanExecuteChanged() => _canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
