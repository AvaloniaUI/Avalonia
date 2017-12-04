// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Rendering;

namespace Avalonia.Controls
{
    /// <summary>
    /// A top-level menu control.
    /// </summary>
    public class Menu : SelectingItemsControl, IFocusScope, IMainMenu
    {
        /// <summary>
        /// Defines the default items panel used by a <see cref="Menu"/>.
        /// </summary>
        private static readonly ITemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new StackPanel { Orientation = Orientation.Horizontal });

        /// <summary>
        /// Defines the <see cref="IsOpen"/> property.
        /// </summary>
        public static readonly DirectProperty<Menu, bool> IsOpenProperty =
            AvaloniaProperty.RegisterDirect<Menu, bool>(
                nameof(IsOpen),
                o => o.IsOpen);

        private bool _isOpen;

        /// <summary>
        /// Tracks event handlers added to the root of the visual tree.
        /// </summary>
        private IDisposable _subscription;

        /// <summary>
        /// Initializes static members of the <see cref="Menu"/> class.
        /// </summary>
        static Menu()
        {
            ItemsPanelProperty.OverrideDefaultValue(typeof(Menu), DefaultPanel);
            MenuItem.ClickEvent.AddClassHandler<Menu>(x => x.OnMenuClick, handledEventsToo: true);
            MenuItem.SubmenuOpenedEvent.AddClassHandler<Menu>(x => x.OnSubmenuOpened);
        }

        /// <summary>
        /// Gets a value indicating whether the menu is open.
        /// </summary>
        public bool IsOpen
        {
            get { return _isOpen; }
            private set { SetAndRaise(IsOpenProperty, ref _isOpen, value); }
        }

        /// <summary>
        /// Gets the selected <see cref="MenuItem"/> container.
        /// </summary>
        private MenuItem SelectedMenuItem
        {
            get
            {
                var index = SelectedIndex;
                return (index != -1) ?
                    (MenuItem)ItemContainerGenerator.ContainerFromIndex(index) :
                    null;
            }
        }

        /// <summary>
        /// Closes the menu.
        /// </summary>
        public void Close()
        {
            foreach (MenuItem i in this.GetLogicalChildren())
            {
                i.IsSubMenuOpen = false;
            }

            IsOpen = false;
            SelectedIndex = -1;
        }

        /// <summary>
        /// Opens the menu in response to the Alt/F10 key.
        /// </summary>
        public void Open()
        {
            SelectedIndex = 0;
            SelectedMenuItem.Focus();
            IsOpen = true;
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var topLevel = (TopLevel)e.Root;
            var window = e.Root as Window;

            if (window != null)
                window.Deactivated += Deactivated;

            var pointerPress = topLevel.AddHandler(
                PointerPressedEvent,
                TopLevelPreviewPointerPress,
                RoutingStrategies.Tunnel);

            _subscription = new CompositeDisposable(
                pointerPress,
                Disposable.Create(() =>
                {
                    if (window != null)
                        window.Deactivated -= Deactivated;
                }),
                InputManager.Instance.Process.Subscribe(ListenForNonClientClick));

            var inputRoot = e.Root as IInputRoot;

            if (inputRoot?.AccessKeyHandler != null)
            {
                inputRoot.AccessKeyHandler.MainMenu = this;
            }
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _subscription.Dispose();
        }

        /// <inheritdoc/>
        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator<MenuItem>(this, MenuItem.HeaderProperty, null);
        }

        /// <summary>
        /// Called when a key is pressed within the menu.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            bool menuWasOpen = SelectedMenuItem?.IsSubMenuOpen ?? false;

            base.OnKeyDown(e);

            if (menuWasOpen)
            {
                // If a menu item was open and we navigate to a new one with the arrow keys, open
                // that menu and select the first item.
                var selection = SelectedMenuItem;

                if (selection != null && !selection.IsSubMenuOpen)
                {
                    selection.IsSubMenuOpen = true;
                    selection.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// Called when the menu loses focus.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            SelectedItem = null;
        }

        /// <summary>
        /// Called when a submenu opens somewhere in the menu.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnSubmenuOpened(RoutedEventArgs e)
        {
            var menuItem = e.Source as MenuItem;

            if (menuItem != null && menuItem.Parent == this)
            {
                foreach (var child in this.GetLogicalChildren().OfType<MenuItem>())
                {
                    if (child != menuItem && child.IsSubMenuOpen)
                    {
                        child.IsSubMenuOpen = false;
                    }
                }
            }

            IsOpen = true;
        }

        /// <summary>
        /// Called when the top-level window is deactivated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void Deactivated(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Listens for non-client clicks and closes the menu when one is detected.
        /// </summary>
        /// <param name="e">The raw event.</param>
        private void ListenForNonClientClick(RawInputEventArgs e)
        {
            var mouse = e as RawMouseEventArgs;

            if (mouse?.Type == RawMouseEventType.NonClientLeftButtonDown)
            {
                Close();
            }
        }

        /// <summary>
        /// Called when a submenu is clicked somewhere in the menu.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void OnMenuClick(RoutedEventArgs e)
        {
            Close();
            FocusManager.Instance.Focus(null);
            e.Handled = true;
        }

        /// <summary>
        /// Called when the pointer is pressed anywhere on the window.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void TopLevelPreviewPointerPress(object sender, PointerPressedEventArgs e)
        {
            if (IsOpen)
            {
                var control = e.Source as ILogical;

                if (!this.IsLogicalParentOf(control))
                {
                    Close();
                }
            }
        }
    }
}
