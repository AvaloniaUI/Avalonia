// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// A menu item control.
    /// </summary>
    public class MenuItem : HeaderedSelectingItemsControl, IMenuItem, ISelectable
    {
        /// <summary>
        /// Defines the <see cref="Command"/> property.
        /// </summary>
        public static readonly DirectProperty<MenuItem, ICommand> CommandProperty =
            Button.CommandProperty.AddOwner<MenuItem>(
                menuItem => menuItem.Command, 
                (menuItem, command) => menuItem.Command = command, 
                enableDataValidation: true);

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
        /// Defines the <see cref="PointerEnterItem"/> event.
        /// </summary>
        public static readonly RoutedEvent<PointerEventArgs> PointerEnterItemEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>(nameof(PointerEnterItem), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PointerLeaveItem"/> event.
        /// </summary>
        public static readonly RoutedEvent<PointerEventArgs> PointerLeaveItemEvent =
            RoutedEvent.Register<InputElement, PointerEventArgs>(nameof(PointerLeaveItem), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="SubmenuOpened"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> SubmenuOpenedEvent =
            RoutedEvent.Register<MenuItem, RoutedEventArgs>(nameof(SubmenuOpened), RoutingStrategies.Bubble);

        /// <summary>
        /// The default value for the <see cref="ItemsControl.ItemsPanel"/> property.
        /// </summary>
        private static readonly ITemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new StackPanel());

        private ICommand _command;
        private bool _commandCanExecute = true;
        private Popup _popup;

        /// <summary>
        /// Initializes static members of the <see cref="MenuItem"/> class.
        /// </summary>
        static MenuItem()
        {
            SelectableMixin.Attach<MenuItem>(IsSelectedProperty);
            CommandProperty.Changed.Subscribe(CommandChanged);
            FocusableProperty.OverrideDefaultValue<MenuItem>(true);
            HeaderProperty.Changed.AddClassHandler<MenuItem>(x => x.HeaderChanged);
            IconProperty.Changed.AddClassHandler<MenuItem>(x => x.IconChanged);
            IsSelectedProperty.Changed.AddClassHandler<MenuItem>(x => x.IsSelectedChanged);
            ItemsPanelProperty.OverrideDefaultValue<MenuItem>(DefaultPanel);
            ClickEvent.AddClassHandler<MenuItem>(x => x.OnClick);
            SubmenuOpenedEvent.AddClassHandler<MenuItem>(x => x.OnSubmenuOpened);
            IsSubMenuOpenProperty.Changed.AddClassHandler<MenuItem>(x => x.SubMenuOpenChanged);
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
        /// Occurs when the pointer enters a menu item.
        /// </summary>
        /// <remarks>
        /// A bubbling version of the <see cref="InputElement.PointerEnter"/> event for menu items.
        /// </remarks>
        public event EventHandler<PointerEventArgs> PointerEnterItem
        {
            add { AddHandler(PointerEnterItemEvent, value); }
            remove { RemoveHandler(PointerEnterItemEvent, value); }
        }

        /// <summary>
        /// Raised when the pointer leaves a menu item.
        /// </summary>
        /// <remarks>
        /// A bubbling version of the <see cref="InputElement.PointerLeave"/> event for menu items.
        /// </remarks>
        public event EventHandler<PointerEventArgs> PointerLeaveItem
        {
            add { AddHandler(PointerLeaveItemEvent, value); }
            remove { RemoveHandler(PointerLeaveItemEvent, value); }
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
            get { return _command; }
            set { SetAndRaise(CommandProperty, ref _command, value); }
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
        /// Gets a value that indicates whether the <see cref="MenuItem"/> is a top-level main menu item.
        /// </summary>
        public bool IsTopLevel => Parent is Menu;

        /// <inheritdoc/>
        bool IMenuItem.IsPointerOverSubMenu => _popup.PopupRoot?.IsPointerOver ?? false;

        /// <inheritdoc/>
        IMenuElement IMenuItem.Parent => Parent as IMenuElement;

        protected override bool IsEnabledCore => base.IsEnabledCore && _commandCanExecute;

        /// <inheritdoc/>
        bool IMenuElement.MoveSelection(NavigationDirection direction, bool wrap) => MoveSelection(direction, wrap);

        /// <inheritdoc/>
        IMenuItem IMenuElement.SelectedItem
        {
            get
            {
                var index = SelectedIndex;
                return (index != -1) ?
                    (IMenuItem)ItemContainerGenerator.ContainerFromIndex(index) :
                    null;
            }
            set
            {
                SelectedIndex = ItemContainerGenerator.IndexFromContainer(value);
            }
        }

        /// <inheritdoc/>
        IEnumerable<IMenuItem> IMenuElement.SubItems
        {
            get
            {
                return ItemContainerGenerator.Containers
                    .Select(x => x.ContainerControl)
                    .OfType<IMenuItem>();
            }
        }            

        /// <summary>
        /// Opens the submenu.
        /// </summary>
        /// <remarks>
        /// This has the same effect as setting <see cref="IsSubMenuOpen"/> to true.
        /// </remarks>
        public void Open() => IsSubMenuOpen = true;

        /// <summary>
        /// Closes the submenu.
        /// </summary>
        /// <remarks>
        /// This has the same effect as setting <see cref="IsSubMenuOpen"/> to false.
        /// </remarks>
        public void Close() => IsSubMenuOpen = false;

        /// <inheritdoc/>
        void IMenuItem.RaiseClick() => RaiseEvent(new RoutedEventArgs(ClickEvent));

        /// <inheritdoc/>
        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new MenuItemContainerGenerator(this);
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);

            if (Command != null)
            {
                Command.CanExecuteChanged += CanExecuteChanged;
            }
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);

            if (Command != null)
            {
                Command.CanExecuteChanged -= CanExecuteChanged;
            }
        }

        /// <summary>
        /// Called when the <see cref="MenuItem"/> is clicked.
        /// </summary>
        /// <param name="e">The click event args.</param>
        protected virtual void OnClick(RoutedEventArgs e)
        {
            if (!e.Handled && Command?.CanExecute(CommandParameter) == true)
            {
                Command.Execute(CommandParameter);
                e.Handled = true;
            }
        }

        /// <inheritdoc/>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            e.Handled = UpdateSelectionFromEventSource(e.Source, true);
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Don't handle here: let event bubble up to menu.
        }

        /// <inheritdoc/>
        protected override void OnPointerEnter(PointerEventArgs e)
        {
            base.OnPointerEnter(e);

            var point = e.GetPointerPoint(null);
            RaiseEvent(new PointerEventArgs(PointerEnterItemEvent, this, e.Pointer, this.VisualRoot, point.Position,
                e.Timestamp, point.Properties, e.InputModifiers));
        }

        /// <inheritdoc/>
        protected override void OnPointerLeave(PointerEventArgs e)
        {
            base.OnPointerLeave(e);

            var point = e.GetPointerPoint(null);
            RaiseEvent(new PointerEventArgs(PointerLeaveItemEvent, this, e.Pointer, this.VisualRoot, point.Position,
                e.Timestamp, point.Properties, e.InputModifiers));
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
                foreach (var child in ((IMenuItem)this).SubItems)
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

            if (_popup != null)
            {
                _popup.Opened -= PopupOpened;
                _popup.Closed -= PopupClosed;
                _popup.DependencyResolver = null;
            }

            _popup = e.NameScope.Find<Popup>("PART_Popup");

            if (_popup != null)
            {
                _popup.DependencyResolver = DependencyResolver.Instance;
                _popup.Opened += PopupOpened;
                _popup.Closed += PopupClosed;
            }
        }

        protected override void UpdateDataValidation(AvaloniaProperty property, BindingNotification status)
        {
            base.UpdateDataValidation(property, status);
            if (property == CommandProperty)
            {
                if (status?.ErrorType == BindingErrorType.Error)
                {
                    if (_commandCanExecute)
                    {
                        _commandCanExecute = false;
                        UpdateIsEffectivelyEnabled();
                    }
                }
            }
        }

        /// <summary>
        /// Closes all submenus of the menu item.
        /// </summary>
        private void CloseSubmenus()
        {
            foreach (var child in ((IMenuItem)this).SubItems)
            {
                child.IsSubMenuOpen = false;
            }
        }

        /// <summary>
        /// Called when the <see cref="Command"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void CommandChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is MenuItem menuItem)
            {
                if (((ILogical)menuItem).IsAttachedToLogicalTree)
                {
                    if (e.OldValue is ICommand oldCommand)
                    {
                        oldCommand.CanExecuteChanged -= menuItem.CanExecuteChanged;
                    }

                    if (e.NewValue is ICommand newCommand)
                    {
                        newCommand.CanExecuteChanged += menuItem.CanExecuteChanged;
                    }
                }

                menuItem.CanExecuteChanged(menuItem, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Called when the <see cref="ICommand.CanExecuteChanged"/> event fires.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void CanExecuteChanged(object sender, EventArgs e)
        {
            var canExecute = Command == null || Command.CanExecute(CommandParameter);

            if (canExecute != _commandCanExecute)
            {
                _commandCanExecute = canExecute;
                UpdateIsEffectivelyEnabled();
            }
        }

        /// <summary>
        /// Called when the <see cref="HeaderedSelectingItemsControl.Header"/> property changes.
        /// </summary>
        /// <param name="e">The property change event.</param>
        private void HeaderChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue is string newValue && newValue == "-")
            {
                PseudoClasses.Add(":separator");
                Focusable = false;
            }
            else if (e.OldValue is string oldValue && oldValue == "-")
            {
                PseudoClasses.Remove(":separator");
                Focusable = true;
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
        /// Called when the <see cref="IsSelected"/> property changes.
        /// </summary>
        /// <param name="e">The property change event.</param>
        private void IsSelectedChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                Focus();
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
