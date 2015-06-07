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

    /// <summary>
    /// A top-level menu control.
    /// </summary>
    public class Menu : ItemsControl
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
        /// Called when the <see cref="MenuItem"/> is attached to the visual tree.
        /// </summary>
        /// <param name="root">The root of the visual tree.</param>
        protected override void OnAttachedToVisualTree(IRenderRoot root)
        {
            base.OnAttachedToVisualTree(root);

            var topLevel = root as TopLevel;

            topLevel.Deactivated += this.Deactivated;

            this.subscription = new CompositeDisposable(
                topLevel.AddHandler(
                    InputElement.PointerPressedEvent,
                    this.TopLevelPointerPress,
                    Interactivity.RoutingStrategies.Tunnel),
                Disposable.Create(() => topLevel.Deactivated -= this.Deactivated));
        }

        /// <summary>
        /// Called when the <see cref="MenuItem"/> is detached from the visual tree.
        /// </summary>
        /// <param name="oldRoot">The root of the visual tree being detached from.</param>
        protected override void OnDetachedFromVisualTree(IRenderRoot oldRoot)
        {
            base.OnDetachedFromVisualTree(oldRoot);
            this.subscription.Dispose();
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
                foreach (var child in this.Items.OfType<MenuItem>())
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
        /// Closes the menu.
        /// </summary>
        private void CloseMenu()
        {
            foreach (MenuItem i in this.GetLogicalChildren())
            {
                i.IsSubMenuOpen = false;
            }

            this.IsOpen = false;
        }

        /// <summary>
        /// Called when the top-level window is deactivated.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void Deactivated(object sender, EventArgs e)
        {
            this.CloseMenu();
        }

        /// <summary>
        /// Called when a submenu is clicked somewhere in the menu.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void OnMenuClick(RoutedEventArgs e)
        {
            this.CloseMenu();
        }

        /// <summary>
        /// Called when the pointer is pressed anywhere on the window.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void TopLevelPointerPress(object sender, PointerPressEventArgs e)
        {
            if (this.IsOpen)
            {
                var control = e.Source as ILogical;

                if (!this.IsLogicalParentOf(control))
                {
                    this.CloseMenu();
                }
            }
        }
    }
}
