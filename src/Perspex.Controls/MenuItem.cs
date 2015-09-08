// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Input;
using Perspex.Controls.Mixins;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Input;
using Perspex.Interactivity;
using Perspex.Threading;
using Perspex.VisualTree;
using Splat;

namespace Perspex.Controls
{
    /// <summary>
    /// A menu item control.
    /// </summary>
    public class MenuItem : SelectingItemsControl, ISelectable
    {
        /// <summary>
        /// Defines the <see cref="Command"/> property.
        /// </summary>
        public static readonly PerspexProperty<ICommand> CommandProperty =
            Button.CommandProperty.AddOwner<MenuItem>();

        /// <summary>
        /// Defines the <see cref="CommandParameter"/> property.
        /// </summary>
        public static readonly PerspexProperty<object> CommandParameterProperty =
            Button.CommandParameterProperty.AddOwner<MenuItem>();

        /// <summary>
        /// Defines the <see cref="Header"/> property.
        /// </summary>
        public static readonly PerspexProperty<object> HeaderProperty =
            HeaderedItemsControl.HeaderProperty.AddOwner<MenuItem>();

        /// <summary>
        /// Defines the <see cref="Icon"/> property.
        /// </summary>
        public static readonly PerspexProperty<object> IconProperty =
            PerspexProperty.Register<MenuItem, object>(nameof(Icon));

        /// <summary>
        /// Defines the <see cref="IsSelected"/> property.
        /// </summary>
        public static readonly PerspexProperty<bool> IsSelectedProperty =
            ListBoxItem.IsSelectedProperty.AddOwner<MenuItem>();

        /// <summary>
        /// Defines the <see cref="IsSubMenuOpen"/> property.
        /// </summary>
        public static readonly PerspexProperty<bool> IsSubMenuOpenProperty =
            PerspexProperty.Register<MenuItem, bool>(nameof(IsSubMenuOpen));

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
        private static readonly ITemplate<IPanel> s_defaultPanel =
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
            ItemsPanelProperty.OverrideDefaultValue<MenuItem>(s_defaultPanel);
            ClickEvent.AddClassHandler<MenuItem>(x => x.OnClick);
            SubmenuOpenedEvent.AddClassHandler<MenuItem>(x => x.OnSubmenuOpened);
            IsSubMenuOpenProperty.Changed.AddClassHandler<MenuItem>(x => x.SubMenuOpenChanged);
            AccessKeyHandler.AccessKeyPressedEvent.AddClassHandler<MenuItem>(x => x.AccessKeyPressed);
        }

        /// <summary>
        /// Occurs when a <see cref="MenuItem"/> without a submenu is clicked.
        /// </summary>
        public event EventHandler<RoutedEventArgs> Click
        {
            add { this.AddHandler(ClickEvent, value); }
            remove { this.RemoveHandler(ClickEvent, value); }
        }

        /// <summary>
        /// Occurs when a <see cref="MenuItem"/>'s submenu is opened.
        /// </summary>
        public event EventHandler<RoutedEventArgs> SubmenuOpened
        {
            add { this.AddHandler(SubmenuOpenedEvent, value); }
            remove { this.RemoveHandler(SubmenuOpenedEvent, value); }
        }

        /// <summary>
        /// Gets or sets the command associated with the menu item.
        /// </summary>
        public ICommand Command
        {
            get { return this.GetValue(CommandProperty); }
            set { this.SetValue(CommandProperty, value); }
        }

        /// <summary>
        /// Gets or sets the parameter to pass to the <see cref="Command"/> property of a
        /// <see cref="MenuItem"/>.
        /// </summary>
        public object CommandParameter
        {
            get { return this.GetValue(CommandParameterProperty); }
            set { this.SetValue(CommandParameterProperty, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="MenuItem"/>'s header.
        /// </summary>
        public object Header
        {
            get { return this.GetValue(HeaderProperty); }
            set { this.SetValue(HeaderProperty, value); }
        }

        /// <summary>
        /// Gets or sets the icon that appears in a <see cref="MenuItem"/>.
        /// </summary>
        public object Icon
        {
            get { return this.GetValue(IconProperty); }
            set { this.SetValue(IconProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="MenuItem"/> is currently selected.
        /// </summary>
        public bool IsSelected
        {
            get { return this.GetValue(IsSelectedProperty); }
            set { this.SetValue(IsSelectedProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the submenu of the <see cref="MenuItem"/> is
        /// open.
        /// </summary>
        public bool IsSubMenuOpen
        {
            get { return this.GetValue(IsSubMenuOpenProperty); }
            set { this.SetValue(IsSubMenuOpenProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the <see cref="MenuItem"/> has a submenu.
        /// </summary>
        public bool HasSubMenu
        {
            get { return !this.Classes.Contains(":empty"); }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="MenuItem"/> is a top-level menu item.
        /// </summary>
        public bool IsTopLevel
        {
            get { return this.Parent is Menu; }
        }

        /// <summary>
        /// Called when the <see cref="MenuItem"/> is clicked.
        /// </summary>
        /// <param name="e">The click event args.</param>
        protected virtual void OnClick(RoutedEventArgs e)
        {
            if (this.Command != null)
            {
                this.Command.Execute(this.CommandParameter);
            }
        }

        /// <summary>
        /// Called when the <see cref="MenuItem"/> recieves focus.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            this.IsSelected = true;
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
                    if (!this.IsTopLevel && this.IsSubMenuOpen)
                    {
                        this.IsSubMenuOpen = false;
                        e.Handled = true;
                    }

                    passStraightToParent = this.IsTopLevel || !this.IsSubMenuOpen;
                    break;

                case Key.Right:
                    if (!this.IsTopLevel && this.HasSubMenu && !this.IsSubMenuOpen)
                    {
                        this.SelectedIndex = 0;
                        this.IsSubMenuOpen = true;
                        e.Handled = true;
                    }

                    passStraightToParent = this.IsTopLevel || !this.IsSubMenuOpen;
                    break;

                case Key.Enter:
                    if (this.HasSubMenu)
                    {
                        goto case Key.Right;
                    }
                    else
                    {
                        this.RaiseEvent(new RoutedEventArgs(ClickEvent));
                        e.Handled = true;
                    }

                    break;

                case Key.Escape:
                    if (this.IsSubMenuOpen)
                    {
                        this.IsSubMenuOpen = false;
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

            var menu = this.Parent as Menu;

            if (menu != null)
            {
                if (menu.IsOpen)
                {
                    this.IsSubMenuOpen = true;
                }
            }
            else if (this.HasSubMenu && !this.IsSubMenuOpen)
            {
                _submenuTimer = DispatcherTimer.Run(
                    () => this.IsSubMenuOpen = true,
                    TimeSpan.FromMilliseconds(400));
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
        protected override void OnPointerPressed(PointerPressEventArgs e)
        {
            base.OnPointerPressed(e);

            if (!this.HasSubMenu)
            {
                this.RaiseEvent(new RoutedEventArgs(ClickEvent));
            }
            else if (this.IsTopLevel)
            {
                this.IsSubMenuOpen = !this.IsSubMenuOpen;
            }
            else
            {
                this.IsSubMenuOpen = true;
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
                foreach (var child in this.Items.OfType<MenuItem>())
                {
                    if (child != menuItem && child.IsSubMenuOpen)
                    {
                        child.IsSubMenuOpen = false;
                    }
                }
            }
        }

        /// <summary>
        /// Called when the MenuItem's template has been applied.
        /// </summary>
        protected override void OnTemplateApplied()
        {
            base.OnTemplateApplied();

            _popup = this.GetTemplateChild<Popup>("popup");
            _popup.DependencyResolver = DependencyResolver.Instance;
            _popup.PopupRootCreated += this.PopupRootCreated;
            _popup.Opened += this.PopupOpened;
            _popup.Closed += this.PopupClosed;
        }

        /// <summary>
        /// Called when the menu item's access key is pressed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void AccessKeyPressed(RoutedEventArgs e)
        {
            if (this.HasSubMenu)
            {
                this.SelectedIndex = 0;
                this.IsSubMenuOpen = true;
            }
            else
            {
                this.RaiseEvent(new RoutedEventArgs(ClickEvent));
            }

            e.Handled = true;
        }

        /// <summary>
        /// Closes all submenus of the menu item.
        /// </summary>
        private void CloseSubmenus()
        {
            foreach (var child in this.Items.OfType<MenuItem>())
            {
                child.IsSubMenuOpen = false;
            }
        }

        /// <summary>
        /// Called when the <see cref="IsSubMenuOpen"/> property changes.
        /// </summary>
        /// <param name="e">The property change event.</param>
        private void SubMenuOpenChanged(PerspexPropertyChangedEventArgs e)
        {
            var value = (bool)e.NewValue;

            if (value)
            {
                this.RaiseEvent(new RoutedEventArgs(SubmenuOpenedEvent));
                this.IsSelected = true;
            }
            else
            {
                this.CloseSubmenus();
                this.SelectedIndex = -1;
            }
        }

        /// <summary>
        /// Called when the submenu's <see cref="PopupRoot"/> is created.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void PopupRootCreated(object sender, EventArgs e)
        {
            var popup = (Popup)sender;
            ItemsPresenter presenter = null;

            // Our ItemsPresenter is in a Popup which means that it's only created when the
            // Popup is opened, therefore it wasn't found by ItemsControl.OnTemplateApplied.
            // Now the Popup has been opened for the first time it should exist, so make sure
            // the PopupRoot's template is applied and look for the ItemsPresenter.
            foreach (var c in popup.PopupRoot.GetSelfAndVisualDescendents().OfType<Control>())
            {
                if (c.Name == "itemsPresenter" && c is ItemsPresenter)
                {
                    presenter = c as ItemsPresenter;
                    break;
                }

                c.ApplyTemplate();
            }

            if (presenter != null)
            {
                // The presenter was found. Set its TemplatedParent so it thinks that it had a
                // normal birth; may it never know its own perveristy.
                presenter.TemplatedParent = this;
                this.Presenter = presenter;
            }
        }

        /// <summary>
        /// Called when the submenu's <see cref="Popup"/> is opened.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void PopupOpened(object sender, EventArgs e)
        {
            var selected = this.SelectedIndex;

            if (selected != -1)
            {
                var container = this.ItemContainerGenerator.ContainerFromIndex(selected);

                if (container != null)
                {
                    container.Focus();
                }
            }
        }

        /// <summary>
        /// Called when the submenu's <see cref="Popup"/> is closed.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void PopupClosed(object sender, EventArgs e)
        {
            this.SelectedItem = null;
        }

        /// <summary>
        /// A dependency resolver which returns a <see cref="MenuItemAccessKeyHandler"/>.
        /// </summary>
        private class DependencyResolver : IDependencyResolver
        {
            /// <summary>
            /// Gets the default instance of <see cref="DependencyResolver"/>.
            /// </summary>
            public static readonly DependencyResolver Instance = new DependencyResolver();

            /// <summary>
            /// Disposes of all managed resources.
            /// </summary>
            public void Dispose()
            {
            }

            /// <summary>
            /// Gets a service of the specified type.
            /// </summary>
            /// <param name="serviceType">The service type.</param>
            /// <param name="contract">An optional contract.</param>
            /// <returns>A service of the requested type.</returns>
            public object GetService(Type serviceType, string contract = null)
            {
                if (serviceType == typeof(IAccessKeyHandler))
                {
                    return new MenuItemAccessKeyHandler();
                }
                else
                {
                    return Locator.Current.GetService(serviceType, contract);
                }
            }

            /// <summary>
            /// Gets collection of services of the specified type.
            /// </summary>
            /// <param name="serviceType">The service type.</param>
            /// <param name="contract">An optional contract.</param>
            /// <returns>A collection of services of the requested type.</returns>
            public IEnumerable<object> GetServices(Type serviceType, string contract = null)
            {
                return Locator.Current.GetServices(serviceType, contract);
            }
        }
    }
}
