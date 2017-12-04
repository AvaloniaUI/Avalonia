// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    /// <summary>
    /// A menu item control.
    /// </summary>
    public class MenuItem : HeaderedSelectingItemsControl, ISelectable
    {
        /// <summary>
        /// Defines the <see cref="Command"/> property.
        /// </summary>
        public static readonly StyledProperty<ICommand> CommandProperty =
            AvaloniaProperty.Register<MenuItem, ICommand>(nameof(Command));

        /// <summary>
        /// Defines the <see cref="HotKey"/> property.
        /// </summary>
        public static readonly StyledProperty<KeyGesture> HotKeyProperty =
            HotKeyManager.HotKeyProperty.AddOwner<MenuItem>();

        /// <summary>
        /// Defines the <see cref="CommandParameter"/> property.
        /// </summary>
        public static readonly StyledProperty<object> CommandParameterProperty =
            Button.CommandParameterProperty.AddOwner<MenuItem>();

        /// <summary>
        /// Defines the <see cref="Icon"/> property.
        /// </summary>
        public static readonly StyledProperty<object> IconProperty =
            AvaloniaProperty.Register<MenuItem, object>(nameof(Icon));

        /// <summary>
        /// Defines the <see cref="IsSelected"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSelectedProperty =
            ListBoxItem.IsSelectedProperty.AddOwner<MenuItem>();

        /// <summary>
        /// Defines the <see cref="IsSubMenuOpen"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSubMenuOpenProperty =
            AvaloniaProperty.Register<MenuItem, bool>(nameof(IsSubMenuOpen));

        /// <summary>
        /// Defines the <see cref="Click"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
            RoutedEvent.Register<MenuItem, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="SubmenuOpened"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> SubmenuOpenedEvent =
            RoutedEvent.Register<MenuItem, RoutedEventArgs>(nameof(SubmenuOpened), RoutingStrategies.Bubble);

        /// <summary>
        /// The default value for the <see cref="ItemsControl.ItemsPanel"/> property.
        /// </summary>
        private static readonly ITemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new StackPanel
            {
                [KeyboardNavigation.DirectionalNavigationProperty] = KeyboardNavigationMode.Cycle,
            });

        /// <summary>
        /// The timer used to display submenus.
        /// </summary>
        private IDisposable _submenuTimer;

        /// <summary>
        /// The submenu popup.
        /// </summary>
        private Popup _popup;

        /// <summary>
        /// Initializes static members of the <see cref="MenuItem"/> class.
        /// </summary>
        static MenuItem()
        {
            SelectableMixin.Attach<MenuItem>(IsSelectedProperty);
            FocusableProperty.OverrideDefaultValue<MenuItem>(true);
            IconProperty.Changed.AddClassHandler<MenuItem>(x => x.IconChanged);
            ItemsPanelProperty.OverrideDefaultValue<MenuItem>(DefaultPanel);
            ClickEvent.AddClassHandler<MenuItem>(x => x.OnClick);
            SubmenuOpenedEvent.AddClassHandler<MenuItem>(x => x.OnSubmenuOpened);
            IsSubMenuOpenProperty.Changed.AddClassHandler<MenuItem>(x => x.SubMenuOpenChanged);
            AccessKeyHandler.AccessKeyPressedEvent.AddClassHandler<MenuItem>(x => x.AccessKeyPressed);
        }

        public MenuItem()
        {

        }

        /// <summary>
        /// Occurs when a <see cref="MenuItem"/> without a submenu is clicked.
        /// </summary>
        public event EventHandler<RoutedEventArgs> Click
        {
            add { AddHandler(ClickEvent, value); }
            remove { RemoveHandler(ClickEvent, value); }
        }

        /// <summary>
        /// Occurs when a <see cref="MenuItem"/>'s submenu is opened.
        /// </summary>
        public event EventHandler<RoutedEventArgs> SubmenuOpened
        {
            add { AddHandler(SubmenuOpenedEvent, value); }
            remove { RemoveHandler(SubmenuOpenedEvent, value); }
        }

        /// <summary>
        /// Gets or sets the command associated with the menu item.
        /// </summary>
        public ICommand Command
        {
            get { return GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        /// <summary>
        /// Gets or sets an <see cref="KeyGesture"/> associated with this control
        /// </summary>
        public KeyGesture HotKey
        {
            get { return GetValue(HotKeyProperty); }
            set { SetValue(HotKeyProperty, value); }
        }

        /// <summary>
        /// Gets or sets the parameter to pass to the <see cref="Command"/> property of a
        /// <see cref="MenuItem"/>.
        /// </summary>
        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        /// <summary>
        /// Gets or sets the icon that appears in a <see cref="MenuItem"/>.
        /// </summary>
        public object Icon
        {
            get { return GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="MenuItem"/> is currently selected.
        /// </summary>
        public bool IsSelected
        {
            get { return GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the submenu of the <see cref="MenuItem"/> is
        /// open.
        /// </summary>
        public bool IsSubMenuOpen
        {
            get { return GetValue(IsSubMenuOpenProperty); }
            set { SetValue(IsSubMenuOpenProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the <see cref="MenuItem"/> has a submenu.
        /// </summary>
        public bool HasSubMenu => !Classes.Contains(":empty");

        /// <summary>
        /// Gets a value that indicates whether the <see cref="MenuItem"/> is a top-level menu item.
        /// </summary>
        public bool IsTopLevel => Parent is Menu;

        /// <summary>
        /// Called when the <see cref="MenuItem"/> is clicked.
        /// </summary>
        /// <param name="e">The click event args.</param>
        protected virtual void OnClick(RoutedEventArgs e)
        {
            if (Command != null)
            {
                Command.Execute(CommandParameter);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Called when the <see cref="MenuItem"/> recieves focus.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            IsSelected = true;
        }

        /// <inheritdoc/>
        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new MenuItemContainerGenerator(this);
        }

        /// <summary>
        /// Called when a key is pressed in the <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Some keypresses we want to pass straight to the parent MenuItem/Menu without giving
            // this MenuItem the chance to handle them. This is usually e.g. when the submenu is
            // closed so passing them to the base would try to move the selection in a hidden
            // submenu.
            var passStraightToParent = true;

            switch (e.Key)
            {
                case Key.Left:
                    if (!IsTopLevel && IsSubMenuOpen)
                    {
                        IsSubMenuOpen = false;
                        e.Handled = true;
                    }

                    passStraightToParent = IsTopLevel || !IsSubMenuOpen;
                    break;

                case Key.Right:
                    if (!IsTopLevel && HasSubMenu && !IsSubMenuOpen)
                    {
                        SelectedIndex = 0;
                        IsSubMenuOpen = true;
                        e.Handled = true;
                    }

                    passStraightToParent = IsTopLevel || !IsSubMenuOpen;
                    break;

                case Key.Enter:
                    if (HasSubMenu)
                    {
                        goto case Key.Right;
                    }
                    else
                    {
                        RaiseEvent(new RoutedEventArgs(ClickEvent));
                        e.Handled = true;
                    }

                    break;

                case Key.Escape:
                    if (IsSubMenuOpen)
                    {
                        IsSubMenuOpen = false;
                        e.Handled = true;
                    }

                    break;
            }

            if (!passStraightToParent)
            {
                base.OnKeyDown(e);
            }
        }

        /// <summary>
        /// Called when the pointer enters the <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnPointerEnter(PointerEventArgs e)
        {
            base.OnPointerEnter(e);

            var menu = Parent as Menu;

            if (menu != null)
            {
                if (menu.IsOpen)
                {
                    IsSubMenuOpen = true;
                }
            }
            else if (HasSubMenu && !IsSubMenuOpen)
            {
                _submenuTimer = DispatcherTimer.Run(
                    () => IsSubMenuOpen = true,
                    TimeSpan.FromMilliseconds(400));
            }
            else
            {
                var parentItem = Parent as MenuItem;
                if (parentItem != null)
                {
                    foreach (var sibling in parentItem.Items
                        .OfType<MenuItem>()
                        .Where(x => x != this && x.IsSubMenuOpen))
                    {
                        sibling.CloseSubmenus();
                        sibling.IsSubMenuOpen = false;
                        sibling.IsSelected = false;
                    }
                }
            }
        }

        /// <summary>
        /// Called when the pointer leaves the <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnPointerLeave(PointerEventArgs e)
        {
            base.OnPointerLeave(e);

            if (_submenuTimer != null)
            {
                _submenuTimer.Dispose();
                _submenuTimer = null;
            }
        }

        /// <summary>
        /// Called when the pointer is pressed over the <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (!HasSubMenu)
            {
                RaiseEvent(new RoutedEventArgs(ClickEvent));
            }
            else if (IsTopLevel)
            {
                IsSubMenuOpen = !IsSubMenuOpen;
            }
            else
            {
                IsSubMenuOpen = true;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Called when a submenu is opened on this MenuItem or a child MenuItem.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnSubmenuOpened(RoutedEventArgs e)
        {
            var menuItem = e.Source as MenuItem;

            if (menuItem != null && menuItem.Parent == this)
            {
                foreach (var child in Items.OfType<MenuItem>())
                {
                    if (child != menuItem && child.IsSubMenuOpen)
                    {
                        child.IsSubMenuOpen = false;
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            _popup = e.NameScope.Get<Popup>("PART_Popup");
            _popup.DependencyResolver = DependencyResolver.Instance;
            _popup.Opened += PopupOpened;
            _popup.Closed += PopupClosed;
        }

        /// <summary>
        /// Called when the menu item's access key is pressed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void AccessKeyPressed(RoutedEventArgs e)
        {
            if (HasSubMenu)
            {
                SelectedIndex = 0;
                IsSubMenuOpen = true;
            }
            else
            {
                RaiseEvent(new RoutedEventArgs(ClickEvent));
            }

            e.Handled = true;
        }

        /// <summary>
        /// Closes all submenus of the menu item.
        /// </summary>
        private void CloseSubmenus()
        {
            foreach (var child in Items.OfType<MenuItem>())
            {
                child.IsSubMenuOpen = false;
            }
        }

        /// <summary>
        /// Called when the <see cref="Icon"/> property changes.
        /// </summary>
        /// <param name="e">The property change event.</param>
        private void IconChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldValue = e.OldValue as ILogical;
            var newValue = e.NewValue as ILogical;

            if (oldValue != null)
            {
                LogicalChildren.Remove(oldValue);
            }

            if (newValue != null)
            {
                LogicalChildren.Add(newValue);
            }
        }

        /// <summary>
        /// Called when the <see cref="IsSubMenuOpen"/> property changes.
        /// </summary>
        /// <param name="e">The property change event.</param>
        private void SubMenuOpenChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var value = (bool)e.NewValue;

            if (value)
            {
                RaiseEvent(new RoutedEventArgs(SubmenuOpenedEvent));
                IsSelected = true;
            }
            else
            {
                CloseSubmenus();
                SelectedIndex = -1;
            }
        }

        /// <summary>
        /// Called when the submenu's <see cref="Popup"/> is opened.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void PopupOpened(object sender, EventArgs e)
        {
            var selected = SelectedIndex;

            if (selected != -1)
            {
                var container = ItemContainerGenerator.ContainerFromIndex(selected);
                container?.Focus();
            }
        }

        /// <summary>
        /// Called when the submenu's <see cref="Popup"/> is closed.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void PopupClosed(object sender, EventArgs e)
        {
            SelectedItem = null;
        }

        /// <summary>
        /// A dependency resolver which returns a <see cref="MenuItemAccessKeyHandler"/>.
        /// </summary>
        private class DependencyResolver : IAvaloniaDependencyResolver
        {
            /// <summary>
            /// Gets the default instance of <see cref="DependencyResolver"/>.
            /// </summary>
            public static readonly DependencyResolver Instance = new DependencyResolver();

            /// <summary>
            /// Gets a service of the specified type.
            /// </summary>
            /// <param name="serviceType">The service type.</param>
            /// <returns>A service of the requested type.</returns>
            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(IAccessKeyHandler))
                {
                    return new MenuItemAccessKeyHandler();
                }
                else
                {
                    return AvaloniaLocator.Current.GetService(serviceType);
                }
            }
        }
    }
}
