using System;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests.Platform
{
    public class DefaultMenuInteractionHandlerTests
    {
        static PointerEventArgs CreateArgs(RoutedEvent ev, IInteractive source) 
            => new PointerEventArgs(ev, source, new FakePointer(), (IVisual)source, default, 0, new PointerPointProperties(), default);

        static PointerPressedEventArgs CreatePressed(IInteractive source) => new PointerPressedEventArgs(source,
            new FakePointer(), (IVisual)source, default,0, new PointerPointProperties {IsLeftButtonPressed = true},
            default);
        
        static PointerReleasedEventArgs CreateReleased(IInteractive source) => new PointerReleasedEventArgs(source,
            new FakePointer(), (IVisual)source, default,0, new PointerPointProperties(), default, MouseButton.Left);
        
        public class TopLevel
        {
            [Fact]
            public void Up_Opens_MenuItem_With_SubMenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var item = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true);
                var e = new KeyEventArgs { Key = Key.Up, Source = item };

                target.KeyDown(item, e);

                Mock.Get(item).Verify(x => x.Open());
                Mock.Get(item).Verify(x => x.MoveSelection(NavigationDirection.First, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Down_Opens_MenuItem_With_SubMenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var item = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true);
                var e = new KeyEventArgs { Key = Key.Down, Source = item };

                target.KeyDown(item, e);

                Mock.Get(item).Verify(x => x.Open());
                Mock.Get(item).Verify(x => x.MoveSelection(NavigationDirection.First, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Right_Selects_Next_MenuItem()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>(x => x.MoveSelection(NavigationDirection.Right, true) == true);
                var item = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.Parent == menu);
                var e = new KeyEventArgs { Key = Key.Right, Source = item };

                target.KeyDown(item, e);

                Mock.Get(menu).Verify(x => x.MoveSelection(NavigationDirection.Right, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Left_Selects_Previous_MenuItem()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>(x => x.MoveSelection(NavigationDirection.Left, true) == true);
                var item = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.Parent == menu);
                var e = new KeyEventArgs { Key = Key.Left, Source = item };

                target.KeyDown(item, e);

                Mock.Get(menu).Verify(x => x.MoveSelection(NavigationDirection.Left, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Enter_On_Item_With_No_SubMenu_Causes_Click()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var item = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.Parent == menu);
                var e = new KeyEventArgs { Key = Key.Enter, Source = item };

                target.KeyDown(item, e);

                Mock.Get(item).Verify(x => x.RaiseClick());
                Mock.Get(menu).Verify(x => x.Close());
                Assert.True(e.Handled);
            }

            [Fact]
            public void Enter_On_Item_With_SubMenu_Opens_SubMenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var item = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true && x.Parent == menu);
                var e = new KeyEventArgs { Key = Key.Enter, Source = item };

                target.KeyDown(item, e);

                Mock.Get(item).Verify(x => x.Open());
                Mock.Get(item).Verify(x => x.MoveSelection(NavigationDirection.First, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Escape_Closes_Parent_Menu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var item = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.Parent == menu);
                var e = new KeyEventArgs { Key = Key.Escape, Source = item };

                target.KeyDown(item, e);

                Mock.Get(menu).Verify(x => x.Close());
                Assert.True(e.Handled);
            }

            [Fact]
            public void Click_On_Open_TopLevel_Menu_Closes_Menu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var item = Mock.Of<IMenuItem>(x =>
                    x.IsSubMenuOpen == true &&
                    x.IsTopLevel == true && 
                    x.HasSubMenu == true &&
                    x.Parent == menu);

                var e = CreatePressed(item);

                target.PointerPressed(item, e);
                Mock.Get(menu).Verify(x => x.Close());
            }

            [Fact]
            public void PointerEnter_Opens_Item_When_Old_Item_Is_Open()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = new Mock<IMenu>();
                var item = Mock.Of<IMenuItem>(x =>
                    x.IsSubMenuOpen == true &&
                    x.IsTopLevel == true &&
                    x.HasSubMenu == true &&
                    x.Parent == menu.Object);
                var nextItem = Mock.Of<IMenuItem>(x =>
                    x.IsTopLevel == true &&
                    x.HasSubMenu == true &&
                    x.Parent == menu.Object);
                var e = CreateArgs(MenuItem.PointerEnterItemEvent, nextItem);

                menu.SetupGet(x => x.SelectedItem).Returns(item);

                target.PointerEnter(nextItem, e);

                Mock.Get(item).Verify(x => x.Close());
                menu.VerifySet(x => x.SelectedItem = nextItem);
                Mock.Get(nextItem).Verify(x => x.Open());
                Mock.Get(nextItem).Verify(x => x.MoveSelection(NavigationDirection.First, true), Times.Never);
                Assert.False(e.Handled);

            }

            [Fact]
            public void PointerLeave_Deselects_Item_When_Menu_Not_Open()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = new Mock<IMenu>();
                var item = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.Parent == menu.Object);
                var e = CreateArgs(MenuItem.PointerLeaveItemEvent, item);

                menu.SetupGet(x => x.SelectedItem).Returns(item);
                target.PointerLeave(item, e);

                menu.VerifySet(x => x.SelectedItem = null);
                Assert.False(e.Handled);
            }

            [Fact]
            public void PointerLeave_Doesnt_Deselect_Item_When_Menu_Open()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = new Mock<IMenu>();
                var item = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.Parent == menu.Object);
                var e = CreateArgs(MenuItem.PointerLeaveItemEvent, item);

                menu.SetupGet(x => x.IsOpen).Returns(true);
                menu.SetupGet(x => x.SelectedItem).Returns(item);
                target.PointerLeave(item, e);

                menu.VerifySet(x => x.SelectedItem = null, Times.Never);
                Assert.False(e.Handled);
            }
        }

        public class NonTopLevel
        {
            [Fact]
            public void Up_Selects_Previous_MenuItem()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var parentItem = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true);
                var item = Mock.Of<IMenuItem>(x => x.Parent == parentItem);
                var e = new KeyEventArgs { Key = Key.Up, Source = item };

                target.KeyDown(item, e);

                Mock.Get(parentItem).Verify(x => x.MoveSelection(NavigationDirection.Up, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Down_Selects_Next_MenuItem()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var parentItem = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true);
                var item = Mock.Of<IMenuItem>(x => x.Parent == parentItem);
                var e = new KeyEventArgs { Key = Key.Down, Source = item };

                target.KeyDown(item, e);

                Mock.Get(parentItem).Verify(x => x.MoveSelection(NavigationDirection.Down, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Left_Closes_Parent_SubMenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var parentItem = Mock.Of<IMenuItem>(x => x.HasSubMenu == true && x.IsSubMenuOpen == true);
                var item = Mock.Of<IMenuItem>(x => x.Parent == parentItem);
                var e = new KeyEventArgs { Key = Key.Left, Source = item };

                target.KeyDown(item, e);
                
                Mock.Get(parentItem).Verify(x => x.Close());
                Mock.Get(parentItem).Verify(x => x.Focus());
                Assert.True(e.Handled);
            }

            [Fact]
            public void Right_With_SubMenu_Items_Opens_SubMenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var parentItem = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true);
                var item = Mock.Of<IMenuItem>(x => x.Parent == parentItem && x.HasSubMenu == true);
                var e = new KeyEventArgs { Key = Key.Right, Source = item };

                target.KeyDown(item, e);

                Mock.Get(item).Verify(x => x.Open());
                Mock.Get(item).Verify(x => x.MoveSelection(NavigationDirection.First, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Right_On_TopLevel_Child_Navigates_TopLevel_Selection()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = new Mock<IMenu>();
                var parentItem = Mock.Of<IMenuItem>(x => 
                    x.IsSubMenuOpen == true &&
                    x.IsTopLevel == true && 
                    x.HasSubMenu == true && 
                    x.Parent == menu.Object);
                var nextItem = Mock.Of<IMenuItem>(x =>
                    x.IsTopLevel == true &&
                    x.HasSubMenu == true &&
                    x.Parent == menu.Object);
                var item = Mock.Of<IMenuItem>(x => x.Parent == parentItem);
                var e = new KeyEventArgs { Key = Key.Right, Source = item };

                menu.Setup(x => x.MoveSelection(NavigationDirection.Right, true))
                    .Callback(() => menu.SetupGet(x => x.SelectedItem).Returns(nextItem))
                    .Returns(true);

                target.KeyDown(item, e);

                menu.Verify(x => x.MoveSelection(NavigationDirection.Right, true));
                Mock.Get(parentItem).Verify(x => x.Close());
                Mock.Get(nextItem).Verify(x => x.Open());
                Mock.Get(nextItem).Verify(x => x.MoveSelection(NavigationDirection.First, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Enter_On_Item_With_No_SubMenu_Causes_Click()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var parentItem = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true && x.Parent == menu);
                var item = Mock.Of<IMenuItem>(x => x.Parent == parentItem);
                var e = new KeyEventArgs { Key = Key.Enter, Source = item };

                target.KeyDown(item, e);

                Mock.Get(item).Verify(x => x.RaiseClick());
                Mock.Get(menu).Verify(x => x.Close());
                Assert.True(e.Handled);
            }

            [Fact]
            public void Enter_On_Item_With_SubMenu_Opens_SubMenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var parentItem = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true);
                var item = Mock.Of<IMenuItem>(x => x.Parent == parentItem && x.HasSubMenu == true);
                var e = new KeyEventArgs { Key = Key.Enter, Source = item };

                target.KeyDown(item, e);

                Mock.Get(item).Verify(x => x.Open());
                Mock.Get(item).Verify(x => x.MoveSelection(NavigationDirection.First, true));
                Assert.True(e.Handled);
            }

            [Fact]
            public void Escape_Closes_Parent_MenuItem()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var parentItem = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true);
                var item = Mock.Of<IMenuItem>(x => x.Parent == parentItem);
                var e = new KeyEventArgs { Key = Key.Escape, Source = item };

                target.KeyDown(item, e);

                Mock.Get(parentItem).Verify(x => x.Close());
                Mock.Get(parentItem).Verify(x => x.Focus());
                Assert.True(e.Handled);
            }

            [Fact]
            public void PointerEnter_Selects_Item()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var parentItem = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true && x.Parent == menu);
                var item = Mock.Of<IMenuItem>(x => x.Parent == parentItem);
                var e = CreateArgs(MenuItem.PointerEnterItemEvent, item);

                target.PointerEnter(item, e);

                Mock.Get(parentItem).VerifySet(x => x.SelectedItem = item);
                Assert.False(e.Handled);
            }

            [Fact]
            public void PointerEnter_Opens_Submenu_After_Delay()
            {
                var timer = new TestTimer();
                var target = new DefaultMenuInteractionHandler(false, null, timer.RunOnce);
                var menu = Mock.Of<IMenu>();
                var parentItem = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true && x.Parent == menu);
                var item = Mock.Of<IMenuItem>(x => x.Parent == parentItem && x.HasSubMenu == true);
                var e = CreateArgs(MenuItem.PointerEnterItemEvent, item);

                target.PointerEnter(item, e);
                Mock.Get(item).Verify(x => x.Open(), Times.Never);

                timer.Pulse();
                Mock.Get(item).Verify(x => x.Open());

                Assert.False(e.Handled);
            }

            [Fact]
            public void PointerEnter_Closes_Sibling_Submenu_After_Delay()
            {
                var timer = new TestTimer();
                var target = new DefaultMenuInteractionHandler(false, null, timer.RunOnce);
                var menu = Mock.Of<IMenu>();
                var parentItem = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true && x.Parent == menu);
                var item = Mock.Of<IMenuItem>(x => x.Parent == parentItem);
                var sibling = Mock.Of<IMenuItem>(x => x.Parent == parentItem && x.HasSubMenu == true && x.IsSubMenuOpen == true);
                var e = CreateArgs(MenuItem.PointerEnterItemEvent, item);

                Mock.Get(parentItem).SetupGet(x => x.SubItems).Returns(new[] { item, sibling });

                target.PointerEnter(item, e);
                Mock.Get(sibling).Verify(x => x.Close(), Times.Never);

                timer.Pulse();
                Mock.Get(sibling).Verify(x => x.Close());

                Assert.False(e.Handled);
            }

            [Fact]
            public void PointerLeave_Deselects_Item()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var parentItem = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true && x.Parent == menu);
                var item = Mock.Of<IMenuItem>(x => x.Parent == parentItem);
                var e = CreateArgs(MenuItem.PointerLeaveItemEvent, item);

                Mock.Get(parentItem).SetupGet(x => x.SelectedItem).Returns(item);
                target.PointerLeave(item, e);

                Mock.Get(parentItem).VerifySet(x => x.SelectedItem = null);
                Assert.False(e.Handled);
            }

            [Fact]
            public void PointerLeave_Doesnt_Deselect_Sibling()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var parentItem = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true && x.Parent == menu);
                var item = Mock.Of<IMenuItem>(x => x.Parent == parentItem);
                var sibling = Mock.Of<IMenuItem>(x => x.Parent == parentItem);
                var e = CreateArgs(MenuItem.PointerLeaveItemEvent, item);

                Mock.Get(parentItem).SetupGet(x => x.SelectedItem).Returns(sibling);
                target.PointerLeave(item, e);

                Mock.Get(parentItem).VerifySet(x => x.SelectedItem = null, Times.Never);
                Assert.False(e.Handled);
            }

            [Fact]
            public void PointerLeave_Doesnt_Deselect_Item_If_Pointer_Over_Submenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var parentItem = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true && x.Parent == menu);
                var item = Mock.Of<IMenuItem>(x => x.Parent == parentItem && x.HasSubMenu == true && x.IsPointerOverSubMenu == true);
                var e = CreateArgs(MenuItem.PointerLeaveItemEvent, item);

                target.PointerLeave(item, e);

                Mock.Get(parentItem).VerifySet(x => x.SelectedItem = null, Times.Never);
                Assert.False(e.Handled);
            }

            [Fact]
            public void PointerReleased_On_Item_With_No_SubMenu_Causes_Click()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var parentItem = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true && x.Parent == menu);
                var item = Mock.Of<IMenuItem>(x => x.Parent == parentItem);
                var e = CreateReleased(item);

                target.PointerReleased(item, e);

                Mock.Get(item).Verify(x => x.RaiseClick());
                Mock.Get(menu).Verify(x => x.Close());
                Assert.True(e.Handled);
            }

            [Fact]
            public void Selection_Is_Correct_When_Pointer_Temporarily_Exits_Item_To_Select_SubItem()
            {
                var timer = new TestTimer();
                var target = new DefaultMenuInteractionHandler(false, null, timer.RunOnce);
                var menu = Mock.Of<IMenu>();
                var parentItem = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true && x.Parent == menu);
                var item = Mock.Of<IMenuItem>(x => x.Parent == parentItem && x.HasSubMenu == true);
                var childItem = Mock.Of<IMenuItem>(x => x.Parent == item);
                var enter = CreateArgs(MenuItem.PointerEnterItemEvent, item);
                var leave = CreateArgs(MenuItem.PointerLeaveItemEvent, item);

                // Pointer enters item; item is selected.
                target.PointerEnter(item, enter);
                Assert.True(timer.ActionIsQueued);
                Mock.Get(parentItem).VerifySet(x => x.SelectedItem = item);
                Mock.Get(parentItem).ResetCalls();

                // SubMenu shown after a delay.
                timer.Pulse();
                Mock.Get(item).Verify(x => x.Open());
                Mock.Get(item).SetupGet(x => x.IsSubMenuOpen).Returns(true);
                Mock.Get(item).ResetCalls();

                // Pointer briefly exits item, but submenu remains open.
                target.PointerLeave(item, leave);
                Mock.Get(item).Verify(x => x.Close(), Times.Never);
                Mock.Get(item).ResetCalls();

                // Pointer enters child item; is selected.
                enter.Source = childItem;
                target.PointerEnter(childItem, enter);
                Mock.Get(item).VerifySet(x => x.SelectedItem = childItem);
                Mock.Get(parentItem).VerifySet(x => x.SelectedItem = item);
                Mock.Get(item).ResetCalls();
                Mock.Get(parentItem).ResetCalls();
            }

            [Fact]
            public void PointerPressed_On_Item_With_SubMenu_Causes_Opens_Submenu()
            {
                var target = new DefaultMenuInteractionHandler(false);
                var menu = Mock.Of<IMenu>();
                var parentItem = Mock.Of<IMenuItem>(x => x.IsTopLevel == true && x.HasSubMenu == true && x.Parent == menu);
                var item = Mock.Of<IMenuItem>(x => x.Parent == parentItem && x.HasSubMenu == true);
                var e = CreatePressed(item);

                target.PointerPressed(item, e);

                Mock.Get(item).Verify(x => x.Open());
                Mock.Get(item).Verify(x => x.MoveSelection(NavigationDirection.First, true), Times.Never);
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

                target.Attach(contextMenu);
                target.KeyDown(contextMenu, e);

                Mock.Get(contextMenu).Verify(x => x.MoveSelection(NavigationDirection.Down, true));
                Assert.True(e.Handled);
            }
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
