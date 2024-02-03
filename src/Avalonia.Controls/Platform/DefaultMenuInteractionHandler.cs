using System;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Reactive;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// Provides the default keyboard and pointer interaction for menus.
    /// </summary>
    [Unstable]
    public class DefaultMenuInteractionHandler : IMenuInteractionHandler
    {
        private readonly bool _isContextMenu;
        private IDisposable? _inputManagerSubscription;
        private IRenderRoot? _root;
        private RadioButtonGroupManager? _groupManager;

        public DefaultMenuInteractionHandler(bool isContextMenu)
            : this(isContextMenu, Input.InputManager.Instance, DefaultDelayRun)
        {
        }

        public DefaultMenuInteractionHandler(
            bool isContextMenu,
            IInputManager? inputManager,
            Action<Action, TimeSpan> delayRun)
        {
            delayRun = delayRun ?? throw new ArgumentNullException(nameof(delayRun));

            _isContextMenu = isContextMenu;
            InputManager = inputManager;
            DelayRun = delayRun;
        }

        public void Attach(MenuBase menu) => AttachCore(menu);
        public void Detach(MenuBase menu) => DetachCore(menu);
        
        protected Action<Action, TimeSpan> DelayRun { get; }

        protected IInputManager? InputManager { get; }

        internal IMenu? Menu { get; private set; }

        public static TimeSpan MenuShowDelay { get; set;} = TimeSpan.FromMilliseconds(400);

        protected internal virtual void GotFocus(object? sender, GotFocusEventArgs e)
        {
            var item = GetMenuItemCore(e.Source as Control);

            if (item?.Parent != null)
            {
                item.SelectedItem = item;
            }
        }

        protected internal virtual void LostFocus(object? sender, RoutedEventArgs e)
        {
            var item = GetMenuItemCore(e.Source as Control);

            if (item != null)
            {
                item.SelectedItem = null;
            }
        }

        protected internal virtual void KeyDown(object? sender, KeyEventArgs e)
        {
            KeyDown(GetMenuItemCore(e.Source as Control), e);
        }

        protected internal virtual void AccessKeyPressed(object? sender, RoutedEventArgs e)
        {
            var item = GetMenuItemCore(e.Source as Control);

            if (item == null)
            {
                return;
            }

            if (item.HasSubMenu && item.IsEffectivelyEnabled)
            {
                Open(item, true);
            }
            else
            {
                Click(item);
            }

            e.Handled = true;
        }

        protected internal virtual void PointerEntered(object? sender, RoutedEventArgs e)
        {
            var item = GetMenuItemCore(e.Source as Control);

            if (item?.Parent == null)
            {
                return;
            }

            if (item.IsTopLevel)
            {
                if (item != item.Parent.SelectedItem &&
                    item.Parent.SelectedItem?.IsSubMenuOpen == true)
                {
                    item.Parent.SelectedItem.Close();
                    SelectItemAndAncestors(item);
                    if (item.HasSubMenu)
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

        protected internal virtual void PointerMoved(object? sender, PointerEventArgs e)
        {
            // HACK: #8179 needs to be addressed to correctly implement it in the PointerPressed method.
            var item = GetMenuItemCore(e.Source as Control) as MenuItem;

            if (item == null)
                return;

            var transformedBounds = item.GetTransformedBounds();
            if (transformedBounds == null)
                return;

            var point = e.GetCurrentPoint(null);

            if (point.Properties.IsLeftButtonPressed
                   && transformedBounds.Value.Contains(point.Position) == false)
            {
                e.Pointer.Capture(null);
            }
        }

        protected internal virtual void PointerExited(object? sender, RoutedEventArgs e)
        {
            var item = GetMenuItemCore(e.Source as Control);

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
                else if (!item.IsPointerOverSubMenu)
                {
                    DelayRun(() =>
                    {
                        if (!item.IsPointerOverSubMenu)
                        {
                            item.IsSubMenuOpen = false;
                        }
                    }, MenuShowDelay);
                }
            }
        }

        protected internal virtual void PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var item = GetMenuItemCore(e.Source as Control);

            if (sender is Visual visual &&
                e.GetCurrentPoint(visual).Properties.IsLeftButtonPressed && item?.HasSubMenu == true)
            {
                if (item.IsSubMenuOpen)
                {
                    // PointerPressed events may bubble from disabled items in sub-menus. In this case,
                    // keep the sub-menu open.
                    var popup = (e.Source as ILogical)?.FindLogicalAncestorOfType<Popup>();
                    if (item.IsTopLevel && popup == null)
                    {
                        CloseMenu(item);
                    }
                }
                else
                {
                    if (item.IsTopLevel && item.Parent is IMainMenu mainMenu)
                    {
                        mainMenu.Open();
                    }

                    Open(item, false);
                }

                e.Handled = true;
            }
        }

        protected internal virtual void PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            var item = GetMenuItemCore(e.Source as Control);

            if (e.InitialPressMouseButton == MouseButton.Left && item?.HasSubMenu == false)
            {
                Click(item);
                e.Handled = true;
            }
        }

        protected internal virtual void MenuOpened(object? sender, RoutedEventArgs e)
        {
            if (e.Source is Menu)
            {
                Menu?.MoveSelection(NavigationDirection.First, true);
            }
        }

        protected internal virtual void RawInput(RawInputEventArgs e)
        {
            var mouse = e as RawPointerEventArgs;

            if (mouse?.Type == RawPointerEventType.NonClientLeftButtonDown)
            {
                Menu?.Close();
            }
        }

        protected internal virtual void RootPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (Menu?.IsOpen == true)
            {
                if (e.Source is ILogical control && !Menu.IsLogicalAncestorOf(control))
                {
                    Menu.Close();
                }
            }
        }

        protected internal virtual void WindowDeactivated(object? sender, EventArgs e)
        {
            Menu?.Close();
        }

        internal static MenuItem? GetMenuItem(StyledElement? item) => (MenuItem?)GetMenuItemCore(item);

        internal void AttachCore(IMenu menu)
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
            Menu.AddHandler(MenuBase.OpenedEvent, MenuOpened);
            Menu.AddHandler(MenuItem.PointerEnteredItemEvent, PointerEntered);
            Menu.AddHandler(MenuItem.PointerExitedItemEvent, PointerExited);
            Menu.AddHandler(InputElement.PointerMovedEvent, PointerMoved);

            _root = Menu.VisualRoot;

            if (_root is not null)
            {
                _groupManager = RadioButtonGroupManager.GetOrCreateForRoot(_root);
                AddMenuItemToRadioGroup(_groupManager, menu);
            }

            if (_root is InputElement inputRoot)
            {
                inputRoot.AddHandler(InputElement.PointerPressedEvent, RootPointerPressed, RoutingStrategies.Tunnel);
            }

            if (_root is WindowBase window)
            {
                window.Deactivated += WindowDeactivated;
            }

            if (_root is TopLevel tl && tl.PlatformImpl is ITopLevelImpl pimpl)
                pimpl.LostFocus += TopLevelLostPlatformFocus;

            _inputManagerSubscription = InputManager?.Process.Subscribe(RawInput);
        }

        internal void DetachCore(IMenu menu)
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
            Menu.RemoveHandler(MenuBase.OpenedEvent, MenuOpened);
            Menu.RemoveHandler(MenuItem.PointerEnteredItemEvent, PointerEntered);
            Menu.RemoveHandler(MenuItem.PointerExitedItemEvent, PointerExited);
            Menu.RemoveHandler(InputElement.PointerMovedEvent, PointerMoved);

            if (_root is not null && _groupManager is { } oldManager)
            {
                _groupManager = null;
                RemoveMenuItemFromRadioGroup(oldManager, menu);
            }

            if (_root is InputElement inputRoot)
            {
                inputRoot.RemoveHandler(InputElement.PointerPressedEvent, RootPointerPressed);
            }

            if (_root is WindowBase root)
            {
                root.Deactivated -= WindowDeactivated;
            }

            if (_root is TopLevel tl && tl.PlatformImpl != null)
                tl.PlatformImpl.LostFocus -= TopLevelLostPlatformFocus;

            _inputManagerSubscription?.Dispose();

            Menu = null;
            _root = null;
        }

        internal void Click(IMenuItem item)
        {
            if (!item.HasSubMenu)
            {
                if (item.ToggleType == MenuItemToggleType.CheckBox)
                {
                    var newValue = !item.IsChecked;
                    item.IsChecked = newValue;   
                }
                else if (item.ToggleType == MenuItemToggleType.Radio && !item.IsChecked)
                {
                    item.IsChecked = true;
                }
            }

            item.RaiseClick();

            if (!item.StaysOpenOnClick)
            {
                CloseMenu(item);
            }
        }

        internal void CloseMenu(IMenuItem item)
        {
            var current = (IMenuElement?)item;

            while (current != null && !(current is IMenu))
            {
                current = (current as IMenuItem)?.Parent;
            }

            current?.Close();
        }

        internal void CloseWithDelay(IMenuItem item)
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

        internal void KeyDown(IMenuItem? item, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                case Key.Down:
                    {
                        if (item?.IsTopLevel == true && item.HasSubMenu)
                        {
                            if (!item.IsSubMenuOpen)
                            {
                                Open(item, true);
                            }
                            else
                            {
                                item.MoveSelection(NavigationDirection.First, true);
                            }

                            e.Handled = true;
                        }
                        else
                        {
                            goto default;
                        }
                        break;
                    }

                case Key.Left:
                    {
                        if (item is { IsSubMenuOpen: true, SelectedItem: null })
                        {
                            item.Close();
                        }
                        else if (item?.Parent is IMenuItem { IsTopLevel: false, IsSubMenuOpen: true } parent)
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
                    }

                case Key.Right:
                    {
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
                    }

                case Key.Enter:
                    {
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
                    }

                case Key.Escape:
                    {
                        if (item?.Parent is IMenuElement parent)
                        {
                            parent.Close();
                            parent.Focus();
                        }
                        else
                        {
                            Menu!.Close();
                        }

                        e.Handled = true;
                        break;
                    }

                default:
                    {
                        var direction = e.Key.ToNavigationDirection();

                        if (direction?.IsDirectional() == true)
                        {
                            if (item == null && _isContextMenu)
                            {
                                if (Menu!.MoveSelection(direction.Value, true) == true)
                                {
                                    e.Handled = true;
                                }
                            }
                            else if (item?.Parent?.MoveSelection(direction.Value, true) == true)
                            {
                                // If the the parent is an IMenu which successfully moved its selection,
                                // and the current menu is open then close the current menu and open the
                                // new menu.
                                if (item.IsSubMenuOpen &&
                                    item.Parent is IMenu &&
                                    item.Parent.SelectedItem is object &&
                                    item.Parent.SelectedItem != item)
                                {
                                    item.Close();
                                    Open(item.Parent.SelectedItem, true);
                                }
                                e.Handled = true;
                            }
                        }

                        break;
                    }
            }

            if (!e.Handled && item?.Parent is IMenuItem parentItem)
            {
                KeyDown(parentItem, e);
            }
        }

        internal void Open(IMenuItem item, bool selectFirst)
        {
            item.Open();

            if (selectFirst)
            {
                item.MoveSelection(NavigationDirection.First, true);
            }
        }

        internal void OpenWithDelay(IMenuItem item)
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

        internal void SelectItemAndAncestors(IMenuItem item)
        {
            var current = (IMenuItem?)item;

            while (current?.Parent != null)
            {
                current.Parent.SelectedItem = current;
                current = current.Parent as IMenuItem;
            }
        }

        internal void OnCheckedChanged(IMenuItem item)
        {
            if (item is IRadioButton radioButton)
            {
                _groupManager?.OnCheckedChanged(radioButton);
            }
        }
        
        internal void OnGroupOrTypeChanged(IRadioButton button, string? oldGroupName)
        {
            if (!string.IsNullOrEmpty(oldGroupName))
            {
                _groupManager?.Remove(button, oldGroupName);
            }
            if (!string.IsNullOrEmpty(button.GroupName))
            {
                _groupManager?.Add(button);
            }
        }

        internal static IMenuItem? GetMenuItemCore(StyledElement? item)
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

        private void TopLevelLostPlatformFocus()
        {
            Menu?.Close();
        }

        private static void DefaultDelayRun(Action action, TimeSpan timeSpan)
        {
            DispatcherTimer.RunOnce(action, timeSpan);
        }

        private static void AddMenuItemToRadioGroup(RadioButtonGroupManager manager, IMenuElement element)
        {
            // Instead add menu item to the group on attached/detached + ensure checked stated on attached.
            if (element is IRadioButton button)
            {
                manager.Add(button);
            }

            foreach (var subItem in element.SubItems)
            {
                AddMenuItemToRadioGroup(manager, subItem);
            }
        }

        private static void RemoveMenuItemFromRadioGroup(RadioButtonGroupManager manager, IMenuElement element)
        {
            if (element is IRadioButton button)
            {
                manager.Remove(button, button.GroupName);
            }

            foreach (var subItem in element.SubItems)
            {
                RemoveMenuItemFromRadioGroup(manager, subItem);
            }
        }
    }
}
