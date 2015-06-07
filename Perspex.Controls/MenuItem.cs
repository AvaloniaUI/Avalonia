// -----------------------------------------------------------------------
// <copyright file="MenuItem.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Linq;
    using System.Windows.Input;
    using Perspex.Controls.Primitives;
    using Perspex.Input;
    using Perspex.Interactivity;
    using Perspex.Rendering;
    using Perspex.Controls.Templates;
    using Perspex.Controls.Presenters;

    /// <summary>
    /// A menu item control.
    /// </summary>
    public class MenuItem : HeaderedItemsControl
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
        /// Defines the <see cref="Icon"/> property.
        /// </summary>
        public static readonly PerspexProperty<object> IconProperty =
            PerspexProperty.Register<MenuItem, object>(nameof(Icon));

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
        /// The timer used to display submenus.
        /// </summary>
        private IDisposable submenuTimer;

        /// <summary>
        /// Initializes static members of the <see cref="MenuItem"/> class.
        /// </summary>
        static MenuItem()
        {
            ClickEvent.AddClassHandler<MenuItem>(x => x.OnClick);
            SubmenuOpenedEvent.AddClassHandler<MenuItem>(x => x.OnSubmenuOpened);
            IsSubMenuOpenProperty.Changed.Subscribe(SubMenuOpenChanged);
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
        /// Gets or sets the icon that appears in a <see cref="MenuItem"/>.
        /// </summary>
        public object Icon
        {
            get { return this.GetValue(IconProperty); }
            set { this.SetValue(IconProperty, value); }
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
            get;
            private set;
        }

        /// <summary>
        /// Called when the <see cref="MenuItem"/> is attached to the visual tree.
        /// </summary>
        /// <param name="root">The root of the visual tree.</param>
        protected override void OnAttachedToVisualTree(IRenderRoot root)
        {
            base.OnAttachedToVisualTree(root);
            this.IsTopLevel = this.Parent is Menu;
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
        /// Called when the pointer enters the <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnPointerEnter(PointerEventArgs e)
        {
            base.OnPointerEnter(e);

            var menu = this.Parent as Menu;

            if (menu != null && menu.IsOpen)
            {
                this.IsSubMenuOpen = true;
            }
        }

        /// <summary>
        /// Called when the pointer leaves the <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnPointerLeave(PointerEventArgs e)
        {
            base.OnPointerLeave(e);

            if (this.submenuTimer != null)
            {
                this.submenuTimer.Dispose();
                this.submenuTimer = null;
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

            var popup = this.FindTemplateChild<Popup>("popup");

            if (popup != null)
            {
                popup.Opened += this.PopupFirstOpened;
            }
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
        private static void SubMenuOpenChanged(PerspexPropertyChangedEventArgs e)
        {
            var sender = e.Sender as MenuItem;
            var value = (bool)e.NewValue;

            if (sender != null)
            {
                if (value)
                {
                    sender.RaiseEvent(new RoutedEventArgs(SubmenuOpenedEvent));
                }
                else
                {
                    sender.CloseSubmenus();
                }
            }
        }

        /// <summary>
        /// Called the first time the MenuItem's popup is opened.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void PopupFirstOpened(object sender, EventArgs e)
        {
            var popup = (Popup)sender;

            // Our ItemsPresenter is in a Popup which means that it's only created when the
            // Popup is opened, therefore it wasn't found by ItemsControl.OnTemplateApplied.
            // Now the Popup has been opened for the first time it should exist, so make sure
            // the PopupRoot's template is applied and look for the ItemsPresenter.
            popup.PopupRoot.ApplyTemplate();
            var presenter = popup.PopupRoot.FindControl<ItemsPresenter>("itemsPresenter");

            if (presenter != null)
            {
                // The presenter was found. First make its Panel's ChildLogicalParent point to
                // this so that the child MenuItems will be logically parented by the parent
                // MenuItem and then assign it to our Presenter property.
                presenter.ApplyTemplate();
                ((IItemsPanel)presenter.Panel).ChildLogicalParent = this;
                this.Presenter = presenter;
            }

            // Don't call this event handler again.
            popup.Opened -= this.PopupFirstOpened;
        }
    }
}
