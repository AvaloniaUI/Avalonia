using System;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests.Platform
{
    public class DefaultMenuInteractionHandlerTests
    {
        static PointerPressedEventArgs CreatePressed(object source) => new PointerPressedEventArgs(source,
            new FakePointer(), (Visual)source, default,0, new PointerPointProperties (RawInputModifiers.None, PointerUpdateKind.LeftButtonPressed),
            default);
        
        static PointerReleasedEventArgs CreateReleased(object source) => new PointerReleasedEventArgs(source,
            new FakePointer(), (Visual)source, default,0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
            default, MouseButton.Left);

        public class TopLevel
        {
            [Fact]
            public void Up_Opens_MenuItem_With_SubMenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var item = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true);
                var e = new KeyEventArgs { Key = Key.Up, Source = item.Object };

                target.KeyDown(item, e);

                item.Verify(x => x.Open());
                item.Verify(x => x.MoveSelection(NavigationDirection.First, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Down_Opens_MenuItem_With_SubMenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var item = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true);
                var e = new KeyEventArgs { Key = Key.Down, Source = item.Object };

                target.KeyDown(item.Object, e);

                item.Verify(x => x.Open());
                item.Verify(x => x.MoveSelection(NavigationDirection.First, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Down_Selects_First_Item_Of_Already_Opened_Submenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var item = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, isSubMenuOpen: true);
                var e = new KeyEventArgs { Key = Key.Down, Source = item.Object };

                target.KeyDown(item, e);

                item.Verify(x => x.MoveSelection(NavigationDirection.First, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Right_Selects_Next_MenuItem()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>(x => x.MoveSelection(NavigationDirection.Right, true) == true);
                var item = CreateMockMenuItem(isTopLevel: true, parent: menu);
                var e = new KeyEventArgs { Key = Key.Right, Source = item.Object };

                target.KeyDown(item, e);

                Mock.Get(menu).Verify(x => x.MoveSelection(NavigationDirection.Right, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Left_Selects_Previous_MenuItem()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>(x => x.MoveSelection(NavigationDirection.Left, true) == true);
                var item = CreateMockMenuItem(isTopLevel: true, parent: menu);
                var e = new KeyEventArgs { Key = Key.Left, Source = item.Object };

                target.KeyDown(item, e);

                Mock.Get(menu).Verify(x => x.MoveSelection(NavigationDirection.Left, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Enter_On_Item_With_No_SubMenu_Causes_Click()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var item = CreateMockMenuItem(isTopLevel: true, parent: menu);
                var e = new KeyEventArgs { Key = Key.Enter, Source = item.Object };

                target.KeyDown(item, e);

                item.Verify(x => x.RaiseClick());
                Mock.Get(menu).Verify(x => x.Close());
                Assert.True(e.Handled);
            }

            [Fact]
            public void Enter_On_Item_With_SubMenu_Opens_SubMenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var item = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, parent: menu);
                var e = new KeyEventArgs { Key = Key.Enter, Source = item.Object };

                target.KeyDown(item, e);

                item.Verify(x => x.Open());
                item.Verify(x => x.MoveSelection(NavigationDirection.First, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Escape_Closes_Parent_Menu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var item = CreateMockMenuItem(isTopLevel: true, parent: menu);
                var e = new KeyEventArgs { Key = Key.Escape, Source = item.Object };

                target.KeyDown(item, e);

                Mock.Get(menu).Verify(x => x.Close());
                Assert.True(e.Handled);
            }

            [Fact]
            public void Click_On_TopLevel_Calls_MainMenu_Open()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = CreateMockMainMenu();
                var item = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, parent: (IMenuElement)menu.Object);

                var e = CreatePressed(item.Object);

                target.PointerPressed(item.Object, e);
                menu.Verify(x => x.Open());
            }

            [Fact]
            public void Click_On_Open_TopLevel_Menu_Closes_Menu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var item = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, isSubMenuOpen: true, parent: menu);

                var e = CreatePressed(item.Object);

                target.PointerPressed(item.Object, e);
                Mock.Get(menu).Verify(x => x.Close());
            }

            [Fact]
            public void PointerEntered_Opens_Item_When_Old_Item_Is_Open()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = new Mock<IMenu>();
                var item = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, isSubMenuOpen: true, parent: menu.Object);
                var nextItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, parent: menu.Object);
                var e = new RoutedEventArgs(MenuItem.PointerEnteredItemEvent, nextItem.Object);

                menu.SetupGet(x => x.SelectedItem).Returns(item.Object);

                target.PointerEntered(nextItem, e);

                item.Verify(x => x.Close());
                menu.VerifySet(x => x.SelectedItem = nextItem.Object);
                nextItem.Verify(x => x.Open());
                nextItem.Verify(x => x.MoveSelection(NavigationDirection.First, true), Times.Never);
                Assert.False(e.Handled);

            }

            [Fact]
            public void PointerExited_Deselects_Item_When_Menu_Not_Open()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = new Mock<IMenu>();
                var item = CreateMockMenuItem(isTopLevel: true, parent: menu.Object);
                var e = new RoutedEventArgs(MenuItem.PointerExitedItemEvent, item.Object);

                menu.SetupGet(x => x.SelectedItem).Returns(item.Object);
                target.PointerExited(item, e);

                menu.VerifySet(x => x.SelectedItem = null);
                Assert.False(e.Handled);
            }

            [Fact]
            public void PointerExited_Doesnt_Deselect_Item_When_Menu_Open()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = new Mock<IMenu>();
                var item = CreateMockMenuItem(isTopLevel: true, parent: menu.Object);
                var e = new RoutedEventArgs(MenuItem.PointerExitedItemEvent, item.Object);

                menu.SetupGet(x => x.IsOpen).Returns(true);
                menu.SetupGet(x => x.SelectedItem).Returns(item.Object);
                target.PointerExited(item, e);

                menu.VerifySet(x => x.SelectedItem = null, Times.Never);
                Assert.False(e.Handled);
            }

            [Fact]
            public void Doesnt_Throw_On_Menu_Keypress()
            {
                // Issue #3459
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var item = CreateMockMenuItem(isTopLevel: true, parent: menu);
                var e = new KeyEventArgs { Key = Key.Tab, Source = menu };

                target.KeyDown(menu, e);
            }
        }

        public class NonTopLevel
        {
            [Fact]
            public void Up_Selects_Previous_MenuItem()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var parentItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true);
                var item = CreateMockMenuItem(parent: parentItem.Object);
                var e = new KeyEventArgs { Key = Key.Up, Source = item.Object };

                target.KeyDown(item.Object, e);

                parentItem.Verify(x => x.MoveSelection(NavigationDirection.Up, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Down_Selects_Next_MenuItem()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var parentItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true);
                var item = CreateMockMenuItem(parent: parentItem.Object);
                var e = new KeyEventArgs { Key = Key.Down, Source = item.Object };

                target.KeyDown(item.Object, e);

                parentItem.Verify(x => x.MoveSelection(NavigationDirection.Down, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Left_Closes_Parent_SubMenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var parentItem = CreateMockMenuItem(hasSubMenu: true, isSubMenuOpen: true);
                var item = CreateMockMenuItem(parent: parentItem.Object);
                var e = new KeyEventArgs { Key = Key.Left, Source = item.Object };

                target.KeyDown(item.Object, e);

                parentItem.Verify(x => x.Close());
                parentItem.Verify(x => x.Focus(It.IsAny<NavigationMethod>(), It.IsAny<KeyModifiers>()));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Right_With_SubMenu_Items_Opens_SubMenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var parentItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true);
                var item = CreateMockMenuItem(hasSubMenu: true, parent: parentItem.Object);
                var e = new KeyEventArgs { Key = Key.Right, Source = item.Object };

                target.KeyDown(item.Object, e);

                item.Verify(x => x.Open());
                item.Verify(x => x.MoveSelection(NavigationDirection.First, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Right_On_TopLevel_Child_Navigates_TopLevel_Selection()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = new Mock<IMenu>();
                var parentItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, isSubMenuOpen: true, parent: menu.Object);
                var nextItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, parent: menu.Object);
                var item = CreateMockMenuItem(parent: parentItem.Object);
                var e = new KeyEventArgs { Key = Key.Right, Source = item.Object };

                menu.Setup(x => x.MoveSelection(NavigationDirection.Right, true))
                    .Callback(() => menu.SetupGet(x => x.SelectedItem).Returns(nextItem.Object))
                    .Returns(true);

                target.KeyDown(item.Object, e);

                menu.Verify(x => x.MoveSelection(NavigationDirection.Right, true));
                parentItem.Verify(x => x.Close());
                nextItem.Verify(x => x.Open());
                nextItem.Verify(x => x.MoveSelection(NavigationDirection.First, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Enter_On_Item_With_No_SubMenu_Causes_Click()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var parentItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, parent: menu);
                var item = CreateMockMenuItem(parent: parentItem.Object);
                var e = new KeyEventArgs { Key = Key.Enter, Source = item.Object };

                target.KeyDown(item.Object, e);

                item.Verify(x => x.RaiseClick());
                Mock.Get(menu).Verify(x => x.Close());
                Assert.True(e.Handled);
            }

            [Fact]
            public void Enter_On_Item_With_SubMenu_Opens_SubMenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var parentItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true);
                var item = CreateMockMenuItem(hasSubMenu: true, parent: parentItem.Object);
                var e = new KeyEventArgs { Key = Key.Enter, Source = item.Object };

                target.KeyDown(item.Object, e);

                item.Verify(x => x.Open());
                item.Verify(x => x.MoveSelection(NavigationDirection.First, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Escape_Closes_Parent_MenuItem()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var parentItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true);
                var item = CreateMockMenuItem(parent: parentItem.Object);
                var e = new KeyEventArgs { Key = Key.Escape, Source = item.Object };

                target.KeyDown(item.Object, e);

                parentItem.Verify(x => x.Close());
                parentItem.Verify(x => x.Focus(It.IsAny<NavigationMethod>(), It.IsAny<KeyModifiers>()));
                Assert.True(e.Handled);
            }

            [Fact]
            public void PointerEntered_Selects_Item()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var parentItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, parent: menu);
                var item = CreateMockMenuItem(parent: parentItem.Object);
                var e = new RoutedEventArgs(MenuItem.PointerEnteredItemEvent, item.Object);

                target.PointerEntered(item.Object, e);

                parentItem.VerifySet(x => x.SelectedItem = item.Object);
                Assert.False(e.Handled);
            }

            [Fact]
            public void PointerEntered_Opens_Submenu_After_Delay()
            {
                var timer = new TestTimer();
                var target = new DefaultMenuInteractionHandler(false, null, timer.RunOnce);
                var menu = Mock.Of<IMenu>();
                var parentItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, parent: menu);
                var item = CreateMockMenuItem(hasSubMenu: true, parent: parentItem.Object);
                var e = new RoutedEventArgs(MenuItem.PointerEnteredItemEvent, item.Object);

                target.PointerEntered(item.Object, e);
                item.Verify(x => x.Open(), Times.Never);

                timer.Pulse();
                item.Verify(x => x.Open());

                Assert.False(e.Handled);
            }

            [Fact]
            public void PointerEntered_Closes_Sibling_Submenu_After_Delay()
            {
                var timer = new TestTimer();
                var target = new DefaultMenuInteractionHandler(false, null, timer.RunOnce);
                var menu = Mock.Of<IMenu>();
                var parentItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, parent: menu);
                var item = CreateMockMenuItem(parent: parentItem.Object);
                var sibling = CreateMockMenuItem(hasSubMenu: true, isSubMenuOpen: true, parent: parentItem.Object);
                var e = new RoutedEventArgs(MenuItem.PointerEnteredItemEvent, item.Object);

                parentItem.SetupGet(x => x.SubItems).Returns(new[] { item.Object, sibling.Object });

                target.PointerEntered(item, e);
                sibling.Verify(x => x.Close(), Times.Never);

                timer.Pulse();
                sibling.Verify(x => x.Close());

                Assert.False(e.Handled);
            }

            [Fact]
            public void PointerExited_Deselects_Item()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var parentItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, parent: menu);
                var item = CreateMockMenuItem(parent: parentItem.Object);
                var e = new RoutedEventArgs(MenuItem.PointerExitedItemEvent, item.Object);

                parentItem.SetupGet(x => x.SelectedItem).Returns(item.Object);
                target.PointerExited(item, e);

                parentItem.VerifySet(x => x.SelectedItem = null);
                Assert.False(e.Handled);
            }

            [Fact]
            public void PointerExited_Doesnt_Deselect_Sibling()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var parentItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, parent: menu);
                var item = CreateMockMenuItem(parent: parentItem.Object);
                var sibling = CreateMockMenuItem(parent: parentItem.Object);
                var e = new RoutedEventArgs(MenuItem.PointerExitedItemEvent, item.Object);

                parentItem.SetupGet(x => x.SelectedItem).Returns(sibling.Object);
                target.PointerExited(item, e);

                parentItem.VerifySet(x => x.SelectedItem = null, Times.Never);
                Assert.False(e.Handled);
            }

            [Fact]
            public void PointerExited_Doesnt_Deselect_Item_If_Pointer_Over_Submenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var parentItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, parent: menu);
                var item = CreateMockMenuItem(hasSubMenu: true, parent: parentItem.Object);
                var e = new RoutedEventArgs(MenuItem.PointerExitedItemEvent, item.Object);

                item.Setup(x => x.IsPointerOverSubMenu).Returns(true);
                target.PointerExited(item, e);

                parentItem.VerifySet(x => x.SelectedItem = null, Times.Never);
                Assert.False(e.Handled);
            }

            [Fact]
            public void PointerReleased_On_Item_With_No_SubMenu_Causes_Click()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var parentItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, parent: menu);
                var item = CreateMockMenuItem(parent: parentItem.Object);
                var e = CreateReleased(item.Object);

                target.PointerReleased(item, e);

                item.Verify(x => x.RaiseClick());
                Mock.Get(menu).Verify(x => x.Close());
                Assert.True(e.Handled);
            }

            [Fact]
            public void Selection_Is_Correct_When_Pointer_Temporarily_Exits_Item_To_Select_SubItem()
            {
                var timer = new TestTimer();
                var target = new DefaultMenuInteractionHandler(false, null, timer.RunOnce);
                var menu = Mock.Of<IMenu>();
                var parentItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, parent: menu);
                var item = CreateMockMenuItem(hasSubMenu: true, parent: parentItem.Object);
                var childItem = CreateMockMenuItem(parent: item.Object);
                var enter = new RoutedEventArgs(MenuItem.PointerEnteredItemEvent, item.Object);
                var leave = new RoutedEventArgs(MenuItem.PointerExitedItemEvent, item.Object);

                // Pointer enters item; item is selected.
                target.PointerEntered(item, enter);
                Assert.True(timer.ActionIsQueued);
                parentItem.VerifySet(x => x.SelectedItem = item.Object);
                parentItem.Invocations.Clear();

                // SubMenu shown after a delay.
                timer.Pulse();
                item.Verify(x => x.Open());
                item.SetupGet(x => x.IsSubMenuOpen).Returns(true);
                item.Invocations.Clear();

                // Pointer briefly exits item, but submenu remains open.
                target.PointerExited(item, leave);
                item.Verify(x => x.Close(), Times.Never);
                item.Invocations.Clear();

                // Pointer enters child item; is selected.
                enter.Source = childItem.Object;
                target.PointerEntered(childItem, enter);
                item.VerifySet(x => x.SelectedItem = childItem.Object);
                parentItem.VerifySet(x => x.SelectedItem = item.Object);
            }

            [Fact]
            public void PointerPressed_On_Item_With_SubMenu_Causes_Opens_Submenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var parentItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, parent: menu);
                var item = CreateMockMenuItem(hasSubMenu: true, parent: parentItem.Object);
                var e = CreatePressed(item.Object);

                target.PointerPressed(item.Object, e);

                item.Verify(x => x.Open());
                item.Verify(x => x.MoveSelection(NavigationDirection.First, true), Times.Never);
                Assert.True(e.Handled);
            }

            [Fact]
            public void PointerPressed_On_Disabled_Item_Doesnt_Close_SubMenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var parentItem = CreateMockMenuItem(isTopLevel: true, hasSubMenu: true, isSubMenuOpen: true, parent: menu);
                var popup = new Popup();
                var e = CreatePressed(popup);

                ((ISetLogicalParent)popup).SetParent(parentItem.Object);
                target.PointerPressed(parentItem.Object, e);

                parentItem.Verify(x => x.Close(), Times.Never);
                Assert.True(e.Handled);
            }
        }

        public class ContextMenu
        {
            [Fact]
            public void Down_Selects_Selects_First_MenuItem_When_No_Selection()
            {
                var target = new DefaultMenuInteractionHandler(true);
                var contextMenu = Mock.Of<IMenu>(x => x.MoveSelection(NavigationDirection.Down, true) == true);
                var e = new KeyEventArgs { Key = Key.Down, Source = contextMenu };

                target.AttachCore(contextMenu);
                target.KeyDown(contextMenu, e);

                Mock.Get(contextMenu).Verify(x => x.MoveSelection(NavigationDirection.Down, true));
                Assert.True(e.Handled);
            }
        }

        private static Mock<IMainMenu> CreateMockMainMenu()
        {
            var mock = new Mock<Control>();
            mock.As<IMenuElement>();
            return mock.As<IMainMenu>();
        }

        private static Mock<IMenuItem> CreateMockMenuItem(
            bool isTopLevel = false,
            bool hasSubMenu = false,
            bool isSubMenuOpen = false,
            IMenuElement parent = null)
        {
            var mock = new Mock<Control>();
            var item = mock.As<IMenuItem>();
            item.Setup(x => x.IsTopLevel).Returns(isTopLevel);
            item.Setup(x => x.HasSubMenu).Returns(hasSubMenu);
            item.Setup(x => x.IsSubMenuOpen).Returns(isSubMenuOpen);
            item.Setup(x => x.Parent).Returns(parent);
            item.SetupProperty(x => x.SelectedItem);
            return item;
        }

        private class TestTimer
        {
            private Action _action;

            public bool ActionIsQueued => _action != null;

            public void Pulse()
            {
                _action();
                _action = null;
            }

            public void RunOnce(Action action, TimeSpan timeSpan)
            {
                if (_action != null)
                {
                    throw new NotSupportedException("Action already set.");
                }

                _action = action;
            }
        }

        class FakePointer : IPointer
        {
            public int Id { get; } = Pointer.GetNextFreeId();

            public void Capture(IInputElement control)
            {
                Captured = control;
            }

            public IInputElement Captured { get; set; }
            public PointerType Type { get; }
            public bool IsPrimary { get; } = true;
        }
    }
}
