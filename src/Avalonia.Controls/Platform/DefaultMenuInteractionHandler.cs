using System;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// Provides the default keyboard and pointer interaction for menus.
    /// </summary>
    public class DefaultMenuInteractionHandler : IMenuInteractionHandler
    {
        private readonly bool _isContextMenu;
        private IDisposable _inputManagerSubscription;
        private IRenderRoot _root;

        public DefaultMenuInteractionHandler(bool isContextMenu)
            : this(isContextMenu, Input.InputManager.Instance, DefaultDelayRun)
        {
        }

        public DefaultMenuInteractionHandler(
            bool isContextMenu,
            IInputManager inputManager,
            Action<Action, TimeSpan> delayRun)
        {
            _isContextMenu = isContextMenu;
            InputManager = inputManager;
            DelayRun = delayRun;
        }

        public virtual void Attach(IMenu menu)
        {
            if (Menu != null)
            {
                throw new NotSupportedException("DefaultMenuInteractionHandler is already attached.");
            }

            Menu = menu;
            Menu.GotFocus += GotFocus;
            Menu.LostFocus += LostFocus;
            Menu.KeyDown += KeyDown;
            Menu.PointerPressed += PointerPressed;
            Menu.PointerReleased += PointerReleased;
            Menu.AddHandler(AccessKeyHandler.AccessKeyPressedEvent, AccessKeyPressed);
            Menu.AddHandler(Avalonia.Controls.Menu.MenuOpenedEvent, this.MenuOpened);
            Menu.AddHandler(MenuItem.PointerEnterItemEvent, PointerEnter);
            Menu.AddHandler(MenuItem.PointerLeaveItemEvent, PointerLeave);

            _root = Menu.VisualRoot;

            if (_root is InputElement inputRoot)
            {
                inputRoot.AddHandler(InputElement.PointerPressedEvent, RootPointerPressed, RoutingStrategies.Tunnel);
            }

            if (_root is WindowBase window)
            {
                window.Deactivated += WindowDeactivated;
            }

            _inputManagerSubscription = InputManager?.Process.Subscribe(RawInput);
        }

        public virtual void Detach(IMenu menu)
        {
            if (Menu != menu)
            {
                throw new NotSupportedException("DefaultMenuInteractionHandler is not attached to the menu.");
            }

            Menu.GotFocus -= GotFocus;
            Menu.LostFocus -= LostFocus;
            Menu.KeyDown -= KeyDown;
            Menu.PointerPressed -= PointerPressed;
            Menu.PointerReleased -= PointerReleased;
            Menu.RemoveHandler(AccessKeyHandler.AccessKeyPressedEvent, AccessKeyPressed);
            Menu.RemoveHandler(Avalonia.Controls.Menu.MenuOpenedEvent, this.MenuOpened);
            Menu.RemoveHandler(MenuItem.PointerEnterItemEvent, PointerEnter);
            Menu.RemoveHandler(MenuItem.PointerLeaveItemEvent, PointerLeave);

            if (_root is InputElement inputRoot)
            {
                inputRoot.RemoveHandler(InputElement.PointerPressedEvent, RootPointerPressed);
            }

            if (_root is WindowBase root)
            {
                root.Deactivated -= WindowDeactivated;
            }

            _inputManagerSubscription.Dispose();

            Menu = null;
            _root = null;
        }

        protected Action<Action, TimeSpan> DelayRun { get; }

        protected IInputManager InputManager { get; }

        protected IMenu Menu { get; private set; }

        protected static TimeSpan MenuShowDelay { get; } = TimeSpan.FromMilliseconds(400);

        protected internal virtual void GotFocus(object sender, GotFocusEventArgs e)
        {
            var item = GetMenuItem(e.Source as IControl);

            if (item?.Parent != null)
            {
                item.SelectedItem = item;
            }
        }

        protected internal virtual void LostFocus(object sender, RoutedEventArgs e)
        {
            var item = GetMenuItem(e.Source as IControl);

            if (item != null)
            {
                item.SelectedItem = null;
            }
        }

        protected internal virtual void KeyDown(object sender, KeyEventArgs e)
        {
            KeyDown(GetMenuItem(e.Source as IControl), e);
        }

        protected internal virtual void KeyDown(IMenuItem item, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                case Key.Down:
                    if (item?.IsTopLevel == true)
                    {
                        if (item.HasSubMenu && !item.IsSubMenuOpen)
                        {
                            Open(item, true);
                            e.Handled = true;
                        }
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case Key.Left:
                    if (item?.Parent is IMenuItem parent && !parent.IsTopLevel && parent.IsSubMenuOpen)
                    {
                        parent.Close();
                        parent.Focus();
                        e.Handled = true;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case Key.Right:
                    if (item != null && !item.IsTopLevel && item.HasSubMenu)
                    {
                        Open(item, true);
                        e.Handled = true;
                    }
                    else
                    {
                        goto default;
                    }
                    break;

                case Key.Enter:
                    if (item != null)
                    {
                        if (!item.HasSubMenu)
                        {
                            Click(item);
                        }
                        else
                        {
                            Open(item, true);
                        }

                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    if (item?.Parent != null)
                    {
                        item.Parent.Close();
                        item.Parent.Focus();
                    }
                    else
                    {
                        Menu.Close();
                    }

                    e.Handled = true;
                    break;

                default:
                    var direction = e.Key.ToNavigationDirection();

                    if (direction.HasValue)
                    {
                        if (item == null && _isContextMenu)
                        {
                            if (Menu.MoveSelection(direction.Value, true) == true)
                            {
                                e.Handled = true;
                            }
                        }
                        else if (item.Parent?.MoveSelection(direction.Value, true) == true)
                        {
                            // If the the parent is an IMenu which successfully moved its selection,
                            // and the current menu is open then close the current menu and open the
                            // new menu.
                            if (item.IsSubMenuOpen && item.Parent is IMenu)
                            {
                                item.Close();
                                Open(item.Parent.SelectedItem, true);
                            }
                            e.Handled = true;
                        }
                    }

                    break;
            }

            if (!e.Handled && item?.Parent is IMenuItem parentItem)
            {
                KeyDown(parentItem, e);
            }
        }

        protected internal virtual void AccessKeyPressed(object sender, RoutedEventArgs e)
        {
            var item = GetMenuItem(e.Source as IControl);

            if (item == null)
            {
                return;
            }

            if (item.HasSubMenu)
            {
                Open(item, true);
            }
            else
            {
                Click(item);
            }

            e.Handled = true;
        }

        protected internal virtual void PointerEnter(object sender, PointerEventArgs e)
        {
            var item = GetMenuItem(e.Source as IControl);

            if (item?.Parent == null)
            {
                return;
            }

            if (item.IsTopLevel)
            {
                if (item.Parent.SelectedItem?.IsSubMenuOpen == true)
                {
                    item.Parent.SelectedItem.Close();
                    SelectItemAndAncestors(item);
                    Open(item, false);
                }
                else
                {
                    SelectItemAndAncestors(item);
                }
            }
            else
            {
                SelectItemAndAncestors(item);

                if (item.HasSubMenu)
                {
                    OpenWithDelay(item);
                }
                else if (item.Parent != null)
                {
                    foreach (var sibling in item.Parent.SubItems)
                    {
                        if (sibling.IsSubMenuOpen)
                        {
                            CloseWithDelay(sibling);
                        }
                    }
                }
            }
        }

        protected internal virtual void PointerLeave(object sender, PointerEventArgs e)
        {
            var item = GetMenuItem(e.Source as IControl);

            if (item?.Parent == null)
            {
                return;
            }

            if (item.Parent.SelectedItem == item)
            {
                if (item.IsTopLevel)
                {
                    if (!((IMenu)item.Parent).IsOpen)
                    {
                        item.Parent.SelectedItem = null;
                    }
                }
                else if (!item.HasSubMenu)
                {
                    item.Parent.SelectedItem = null;
                }
            }
        }

        protected internal virtual void PointerPressed(object sender, PointerPressedEventArgs e)
        {
            var item = GetMenuItem(e.Source as IControl);

            if (e.MouseButton == MouseButton.Left && item?.HasSubMenu == true)
            {
                if (item.IsSubMenuOpen)
                {
                    if (item.IsTopLevel)
                    {
                        CloseMenu(item);
                    }
                }
                else
                {
                    Open(item, false);
                }

                e.Handled = true;
            }
        }

        protected internal virtual void PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            var item = GetMenuItem(e.Source as IControl);

            if (e.MouseButton == MouseButton.Left && item?.HasSubMenu == false)
            {
                Click(item);
                e.Handled = true;
            }
        }

        protected internal virtual void MenuOpened(object sender, RoutedEventArgs e)
        {
            if (e.Source == Menu)
            {
                Menu.MoveSelection(NavigationDirection.First, true);
            }
        }

        protected internal virtual void RawInput(RawInputEventArgs e)
        {
            var mouse = e as RawPointerEventArgs;

            if (mouse?.Type == RawPointerEventType.NonClientLeftButtonDown)
            {
                Menu.Close();
            }
        }

        protected internal virtual void RootPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (Menu?.IsOpen == true)
            {
                var control = e.Source as ILogical;

                if (!Menu.IsLogicalParentOf(control))
                {
                    Menu.Close();
                }
            }
        }

        protected internal virtual void WindowDeactivated(object sender, EventArgs e)
        {
            Menu.Close();
        }

        protected void Click(IMenuItem item)
        {
            item.RaiseClick();
            CloseMenu(item);
        }

        protected void CloseMenu(IMenuItem item)
        {
            var current = (IMenuElement)item;

            while (current != null && !(current is IMenu))
            {
                current = (current as IMenuItem)?.Parent;
            }

            current?.Close();
        }

        protected void CloseWithDelay(IMenuItem item)
        {
            void Execute()
            {
                if (item.Parent?.SelectedItem != item)
                {
                    item.Close();
                }
            }

            DelayRun(Execute, MenuShowDelay);
        }

        protected void Open(IMenuItem item, bool selectFirst)
        {
            item.Open();

            if (selectFirst)
            {
                item.MoveSelection(NavigationDirection.First, true);
            }
        }

        protected void OpenWithDelay(IMenuItem item)
        {
            void Execute()
            {
                if (item.Parent?.SelectedItem == item)
                {
                    Open(item, false);
                }
            }

            DelayRun(Execute, MenuShowDelay);
        }

        protected void SelectItemAndAncestors(IMenuItem item)
        {
            var current = item;

            while (current?.Parent != null)
            {
                current.Parent.SelectedItem = current;
                current = current.Parent as IMenuItem;
            }
        }

        protected static IMenuItem GetMenuItem(IControl item)
        {
            while (true)
            {
                if (item == null)
                    return null;
                if (item is IMenuItem menuItem)
                    return menuItem;
                item = item.Parent;
            }
        }

        private static void DefaultDelayRun(Action action, TimeSpan timeSpan)
        {
            DispatcherTimer.RunOnce(action, timeSpan);
        }
    }
}
