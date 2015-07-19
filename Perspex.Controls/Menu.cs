// -----------------------------------------------------------------------
// <copyright file="Menu.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Linq;
    using System.Reactive.Disposables;
    using Perspex.Input;
    using Perspex.LogicalTree;
    using Perspex.Rendering;
    using Perspex.Interactivity;
    using Perspex.Controls.Primitives;

    /// <summary>
    /// A top-level menu control.
    /// </summary>
    public class Menu : SelectingItemsControl, IFocusScope, IMainMenu
    {
        /// <summary>
        /// Defines the default items panel used by a <see cref="Menu"/>.
        /// </summary>
        private static readonly ItemsPanelTemplate DefaultPanel =
            new ItemsPanelTemplate(() => new StackPanel { Orientation = Orientation.Horizontal });

        /// <summary>
        /// Defines the <see cref="IsOpen"/> property.
        /// </summary>
        public static readonly PerspexProperty<bool> IsOpenProperty =
            PerspexProperty.Register<Menu, bool>(nameof(IsOpen));

        /// <summary>
        /// Tracks event handlers added to the root of the visual tree.
        /// </summary>
        private IDisposable subscription;

        /// <summary>
        /// Initializes static members of the <see cref="Menu"/> class.
        /// </summary>
        static Menu()
        {
            ItemsPanelProperty.OverrideDefaultValue(typeof(Menu), DefaultPanel);
            MenuItem.ClickEvent.AddClassHandler<Menu>(x => x.OnMenuClick);
            MenuItem.SubmenuOpenedEvent.AddClassHandler<Menu>(x => x.OnSubmenuOpened);
        }

        /// <summary>
        /// Gets a value indicating whether the menu is open.
        /// </summary>
        public bool IsOpen
        {
            get { return this.GetValue(IsOpenProperty); }
            private set { this.SetValue(IsOpenProperty, value); }
        }

        /// <summary>
        /// Gets the selected <see cref="MenuItem"/> container.
        /// </summary>
        private MenuItem SelectedMenuItem
        {
            get
            {
                return (this.SelectedItem != null) ?
                    (MenuItem)this.ItemContainerGenerator.GetContainerForItem(this.SelectedItem) :
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

            this.IsOpen = false;
            this.SelectedIndex = -1;
        }

        /// <summary>
        /// Opens the menu in response to the Alt/F10 key.
        /// </summary>
        public void Open()
        {
            this.SelectedIndex = 0;
            this.SelectedMenuItem.Focus();
            this.IsOpen = true;
        }

        /// <summary>
        /// Called when the <see cref="MenuItem"/> is attached to the visual tree.
        /// </summary>
        /// <param name="root">The root of the visual tree.</param>
        protected override void OnAttachedToVisualTree(IRenderRoot root)
        {
            base.OnAttachedToVisualTree(root);

            var topLevel = root as TopLevel;

            topLevel.Deactivated += this.Deactivated;

            var pointerPress = topLevel.AddHandler(
                InputElement.PointerPressedEvent,
                this.TopLevelPreviewPointerPress,
                RoutingStrategies.Tunnel);

            this.subscription = new CompositeDisposable(
                pointerPress,
                Disposable.Create(() => topLevel.Deactivated -= this.Deactivated));

            var inputRoot = root as IInputRoot;

            if (inputRoot != null && inputRoot.AccessKeyHandler != null)
            {
                inputRoot.AccessKeyHandler.MainMenu = this;
            }
        }

        /// <summary>
        /// Called when the <see cref="Menu"/> is detached from the visual tree.
        /// </summary>
        /// <param name="oldRoot">The root of the visual tree being detached from.</param>
        protected override void OnDetachedFromVisualTree(IRenderRoot oldRoot)
        {
            base.OnDetachedFromVisualTree(oldRoot);
            this.subscription.Dispose();
        }

        /// <summary>
        /// Called when a key is pressed within the menu.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            bool menuWasOpen = this.SelectedMenuItem?.IsSubMenuOpen ?? false;

            base.OnKeyDown(e);

            if (menuWasOpen)
            {
                // If a menu item was open and we navigate to a new one with the arrow keys, open
                // that menu and select the first item.
                var selection = this.SelectedMenuItem;

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
            this.SelectedItem = null;
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

            this.IsOpen = true;
        }

        /// <summary>
        /// Called when the top-level window is deactivated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void Deactivated(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Called when a submenu is clicked somewhere in the menu.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void OnMenuClick(RoutedEventArgs e)
        {
            this.Close();
            FocusManager.Instance.Focus(null);
            e.Handled = true;
        }

        /// <summary>
        /// Called when the pointer is pressed anywhere on the window.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void TopLevelPreviewPointerPress(object sender, PointerPressEventArgs e)
        {
            if (this.IsOpen)
            {
                var control = e.Source as ILogical;

                if (!this.IsLogicalParentOf(control))
                {
                    this.Close();
                }
            }
        }
    }
}
