using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.UnitTests;
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
                var contextMenu = new ContextMenu { Items = { target } };
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
                Assert.Equal(3, canExecuteCallCount);// 3 because popup is changing logical child and moreover we need to invalidate again after the item is attached to the visual tree

                command.RaiseCanExecuteChanged();
                Assert.Equal(4, canExecuteCallCount);

                target.CommandParameter = true;
                Assert.Equal(5, canExecuteCallCount);
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
                var flyout = new MenuFlyout { Items = { target } };
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
                Assert.Equal(2, canExecuteCallCount); // 2 because we need to invalidate after the item is attached to the visual tree

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
                var parentMenuItem = new MenuItem { Items = { target } };
                var contextMenu = new ContextMenu { Items = { parentMenuItem } };
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
                    Items =
                    {
                        (topLevelMenu = new MenuItem
                        {
                            Header = "Foo",
                            Items =
                            {
                                (childMenu1 = new MenuItem { Header = "Bar" }),
                                (childMenu2 = new MenuItem { Header = "Baz" }),
                            }
                        }),
                    }
                };

                var window = new Window { Content = menu };
                window.Show();
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

        [Fact]
        public void Menu_ItemTemplate_Should_Be_Applied_To_TopLevel_MenuItem_Header()
        {
            using var app = Application();

            var items = new[]
            {
                new MenuViewModel("Foo"),
                new MenuViewModel("Bar"),
            };

            var itemTemplate = new FuncDataTemplate<MenuViewModel>((x, _) =>
                new TextBlock { Text = x.Header });

            var menu = new Menu
            {
                ItemTemplate = itemTemplate,
                ItemsSource = items,
            };

            var window = new Window { Content = menu };
            window.Show();
            window.LayoutManager.ExecuteInitialLayoutPass();

            var panel = Assert.IsType<StackPanel>(menu.Presenter.Panel);
            Assert.Equal(2, panel.Children.Count);

            for (var i = 0; i <  panel.Children.Count; i++)
            {
                var menuItem = Assert.IsType<MenuItem>(panel.Children[i]);

                Assert.Equal(items[i], menuItem.Header);
                Assert.Same(itemTemplate, menuItem.HeaderTemplate);

                var headerPresenter = Assert.IsType<ContentPresenter>(menuItem.HeaderPresenter);
                Assert.Same(itemTemplate, headerPresenter.ContentTemplate);

                var headerControl = Assert.IsType<TextBlock>(headerPresenter.Child);
                Assert.Equal(items[i].Header, headerControl.Text);
            }
        }

        [Fact]
        public void Header_And_ItemsSource_Can_Be_Bound_In_Style()
        {
            using var app = Application();
            var items = new[]
            {
                new MenuViewModel("Foo")
                {
                    Children = new[]
                    {
                        new MenuViewModel("FooChild"),
                    },
                },
                new MenuViewModel("Bar"),
            };

            var target = new Menu
            {
                ItemsSource = items,
                Styles =
                {
                    new Style(x => x.OfType<MenuItem>())
                    {
                        Setters =
                        {
                            new Setter(MenuItem.HeaderProperty, new Binding("Header")),
                            new Setter(MenuItem.ItemsSourceProperty, new Binding("Children")),
                        }
                    }
                }
            };

            var root = new TestRoot(true, target);
            root.LayoutManager.ExecuteInitialLayoutPass();

            var children = target.GetRealizedContainers().Cast<MenuItem>().ToList();
            Assert.Equal(2, children.Count);
            Assert.Equal("Foo", children[0].Header);
            Assert.Equal("Bar", children[1].Header);
            Assert.Same(items[0].Children, children[0].ItemsSource);
        }

        [Fact]
        public void Header_And_ItemsSource_Can_Be_Bound_In_ItemContainerTheme()
        {
            using var app = Application();
            var items = new[]
            {
                new MenuViewModel("Foo")
                {
                    Children = new[]
                    {
                        new MenuViewModel("FooChild"),
                    },
                },
                new MenuViewModel("Bar"),
            };

            var target = new Menu
            {
                ItemsSource = items,
                ItemContainerTheme = new ControlTheme(typeof(MenuItem))
                {
                    Setters =
                    {
                        new Setter(MenuItem.HeaderProperty, new Binding("Header")),
                        new Setter(MenuItem.ItemsSourceProperty, new Binding("Children")),
                    }
                }
            };

            var root = new TestRoot(true, target);
            root.LayoutManager.ExecuteInitialLayoutPass();

            var children = target.GetRealizedContainers().Cast<MenuItem>().ToList();
            Assert.Equal(2, children.Count);
            Assert.Equal("Foo", children[0].Header);
            Assert.Equal("Bar", children[1].Header);
            Assert.Same(items[0].Children, children[0].ItemsSource);
        }

        [Fact]
        public void Radio_MenuItem_In_Same_Group_Is_Unchecked()
        {
            using var app = Application();

            MenuItem menuItem1, menuItem2, menuItem3;

            var menu = new Menu
            {
                Items =
                {
                    (menuItem1 = new MenuItem
                    {
                        GroupName = "A", IsChecked = false, ToggleType = MenuItemToggleType.Radio
                    }),
                    (menuItem2 = new MenuItem
                    {
                        GroupName = "A", IsChecked = true, ToggleType = MenuItemToggleType.Radio
                    }),
                    (menuItem3 = new MenuItem
                    {
                        GroupName = "A", IsChecked = false, ToggleType = MenuItemToggleType.Radio
                    })
                }
            };

            var window = new Window { Content = menu };
            window.Show();
            
            Assert.False(menuItem1.IsChecked);
            Assert.True(menuItem2.IsChecked);
            Assert.False(menuItem3.IsChecked);

            menuItem3.IsChecked = true;

            Assert.False(menuItem1.IsChecked);
            Assert.False(menuItem2.IsChecked);
            Assert.True(menuItem3.IsChecked);
        }
        
        [Fact]
        public void Radio_Menu_Group_Can_Be_Changed_In_Runtime()
        {
            using var app = Application();

            MenuItem menuItem1, menuItem2, menuItem3;

            var menu = new Menu
            {
                Items =
                {
                    (menuItem1 = new MenuItem
                    {
                        GroupName = "A", IsChecked = false, ToggleType = MenuItemToggleType.Radio
                    }),
                    (menuItem2 = new MenuItem
                    {
                        GroupName = "A", IsChecked = true, ToggleType = MenuItemToggleType.Radio
                    }),
                    (menuItem3 = new MenuItem
                    {
                        GroupName = null, IsChecked = false, ToggleType = MenuItemToggleType.Radio
                    })
                }
            };

            var window = new Window { Content = menu };
            window.Show();
            
            Assert.False(menuItem1.IsChecked);
            Assert.True(menuItem2.IsChecked);
            Assert.False(menuItem3.IsChecked);

            menuItem3.GroupName = "A";
            menuItem3.IsChecked = true;

            Assert.False(menuItem1.IsChecked);
            Assert.False(menuItem2.IsChecked);
            Assert.True(menuItem3.IsChecked);

            menuItem3.GroupName = null;
            menuItem1.IsChecked = true;
            
            Assert.True(menuItem1.IsChecked);
            Assert.False(menuItem2.IsChecked);
            Assert.True(menuItem3.IsChecked);
        }
        
        [Fact]
        public void Radio_MenuItem_In_Same_Group_But_Submenu_Is_Unchecked()
        {
            using var app = Application();

            MenuItem menuItem1, menuItem2, menuItem3, menuItem4;

            var menu = new Menu
            {
                Items =
                {
                    (menuItem1 = new MenuItem
                    {
                        GroupName = "A", IsChecked = false, ToggleType = MenuItemToggleType.Radio
                    }),
                    (menuItem2 = new MenuItem
                    {
                        GroupName = "A", IsChecked = false, ToggleType = MenuItemToggleType.Radio
                    }),
                    (menuItem3 = new MenuItem
                    {
                        GroupName = "A",
                        IsChecked = true,
                        ToggleType = MenuItemToggleType.Radio,
                        Items =
                        {
                            (menuItem4 = new MenuItem
                            {
                                GroupName = "A",
                                IsChecked = true,
                                ToggleType = MenuItemToggleType.Radio
                            })
                        }
                    }),
                }
            };

            var window = new Window { Content = menu };
            window.Show();
            
            Assert.False(menuItem1.IsChecked);
            Assert.False(menuItem2.IsChecked);
            Assert.True(menuItem3.IsChecked);
            Assert.True(menuItem4.IsChecked);

            menuItem2.IsChecked = true;

            Assert.False(menuItem1.IsChecked);
            Assert.True(menuItem2.IsChecked);
            Assert.False(menuItem3.IsChecked);
            Assert.False(menuItem4.IsChecked);
        }

        [Fact]
        public void Radio_MenuItem_In_Same_Group_But_Submenu_Is_Checked()
        {
            using var app = Application();

            MenuItem menuItem1, menuItem2, menuItem3, menuItem4;

            var menu = new Menu
            {
                Items =
                {
                    (menuItem1 = new MenuItem
                    {
                        GroupName = "A", IsChecked = false, ToggleType = MenuItemToggleType.Radio
                    }),
                    (menuItem2 = new MenuItem
                    {
                        GroupName = "A", IsChecked = true, ToggleType = MenuItemToggleType.Radio
                    }),
                    (menuItem3 = new MenuItem
                    {
                        GroupName = "A",
                        IsChecked = false,
                        ToggleType = MenuItemToggleType.Radio,
                        Items =
                        {
                            (menuItem4 = new MenuItem
                            {
                                GroupName = "A",
                                IsChecked = false,
                                ToggleType = MenuItemToggleType.Radio
                            })
                        }
                    }),
                }
            };

            var window = new Window { Content = menu };
            window.Show();

            Assert.False(menuItem1.IsChecked);
            Assert.True(menuItem2.IsChecked);
            Assert.False(menuItem3.IsChecked);
            Assert.False(menuItem4.IsChecked);

            menuItem4.IsChecked = true;

            Assert.False(menuItem1.IsChecked);
            Assert.False(menuItem2.IsChecked);
            Assert.True(menuItem3.IsChecked);
            Assert.True(menuItem4.IsChecked);
        }

        [Fact]
        public void Radio_MenuItem_Empty_GroupName_Not_Influence_Other_Groups()
        {
            using var app = Application();

            MenuItem menuItem1, menuItem2, menuItem3, menuItem4;

            var menu = new Menu
            {
                Items =
                {
                    (menuItem1 = new MenuItem
                    {
                        GroupName = "A", IsChecked = true, ToggleType = MenuItemToggleType.Radio
                    }),
                    (menuItem2 = new MenuItem
                    {
                        GroupName = "A", IsChecked = false, ToggleType = MenuItemToggleType.Radio
                    }),
                    (menuItem3 = new MenuItem
                    {
                        GroupName = null, IsChecked = false, ToggleType = MenuItemToggleType.Radio
                    }),
                    (menuItem4 = new MenuItem
                    {
                        GroupName = null, IsChecked = true, ToggleType = MenuItemToggleType.Radio
                    })
                }
            };

            var window = new Window { Content = menu };
            window.Show();

            Assert.True(menuItem1.IsChecked);
            Assert.False(menuItem2.IsChecked);
            Assert.False(menuItem3.IsChecked);
            Assert.True(menuItem4.IsChecked);

            menuItem3.IsChecked = true;

            Assert.True(menuItem1.IsChecked);
            Assert.False(menuItem2.IsChecked);
            Assert.True(menuItem3.IsChecked);
            Assert.False(menuItem4.IsChecked);
        }

        [Fact]
        public void Radio_Menus_With_Empty_Group_On_Different_Levels_Can_Be_Checked_Simultaneously()
        {
            using var app = Application();

            MenuItem menuItem1, menuItem2, menuItem3, menuItem4;

            var menu = new Menu
            {
                Items =
                {
                    (menuItem1 = new MenuItem
                    {
                        GroupName = null, IsChecked = true, ToggleType = MenuItemToggleType.Radio
                    }),
                    (menuItem2 = new MenuItem
                    {
                        GroupName = null,
                        IsChecked = false,
                        ToggleType = MenuItemToggleType.Radio,
                        Items =
                        {
                            (menuItem3 = new MenuItem
                            {
                                GroupName = null,
                                IsChecked = false,
                                ToggleType = MenuItemToggleType.Radio
                            }),
                            (menuItem4 = new MenuItem
                            {
                                GroupName = null,
                                IsChecked = false,
                                ToggleType = MenuItemToggleType.Radio
                            }),
                        }
                    })
                }
            };

            var window = new Window { Content = menu };
            window.Show();

            Assert.True(menuItem1.IsChecked);
            Assert.False(menuItem2.IsChecked);
            Assert.False(menuItem3.IsChecked);
            Assert.False(menuItem4.IsChecked);

            menuItem3.IsChecked = true;

            Assert.True(menuItem1.IsChecked);
            Assert.False(menuItem2.IsChecked);
            Assert.True(menuItem3.IsChecked);
            Assert.False(menuItem4.IsChecked);
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

        private record MenuViewModel(string Header)
        {
            public IList<MenuViewModel> Children { get; set;}
        }
    }
}
