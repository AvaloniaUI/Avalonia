using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Reactive;
using System.Windows.Input;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <summary>
    /// A menu item control.
    /// </summary>
    [TemplatePart("PART_Popup", typeof(Popup))]
    [PseudoClasses(":separator", ":radio", ":toggle", ":checked", ":icon", ":open", ":pressed", ":selected")]
    public class MenuItem : HeaderedSelectingItemsControl, IMenuItem, ISelectable, ICommandSource, IClickableControl, IRadioButton
    {
        /// <summary>
        /// Defines the <see cref="Command"/> property.
        /// </summary>
        public static readonly StyledProperty<ICommand?> CommandProperty =
            Button.CommandProperty.AddOwner<MenuItem>(new(enableDataValidation: true));

        /// <summary>
        /// Defines the <see cref="HotKey"/> property.
        /// </summary>
        public static readonly StyledProperty<KeyGesture?> HotKeyProperty =
            HotKeyManager.HotKeyProperty.AddOwner<MenuItem>();

        /// <summary>
        /// Defines the <see cref="CommandParameter"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> CommandParameterProperty =
            Button.CommandParameterProperty.AddOwner<MenuItem>();

        /// <summary>
        /// Defines the <see cref="Icon"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> IconProperty =
            AvaloniaProperty.Register<MenuItem, object?>(nameof(Icon));

        /// <summary>
        /// Defines the <see cref="InputGesture"/> property.
        /// </summary>
        public static readonly StyledProperty<KeyGesture?> InputGestureProperty =
            AvaloniaProperty.Register<MenuItem, KeyGesture?>(nameof(InputGesture));

        /// <summary>
        /// Defines the <see cref="IsSubMenuOpen"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSubMenuOpenProperty =
            AvaloniaProperty.Register<MenuItem, bool>(nameof(IsSubMenuOpen));

        /// <summary>
        /// Defines the <see cref="StaysOpenOnClick"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> StaysOpenOnClickProperty =
            AvaloniaProperty.Register<MenuItem, bool>(nameof(StaysOpenOnClick));

        /// <summary>
        /// Defines the <see cref="ToggleType"/> property.
        /// </summary>
        public static readonly StyledProperty<MenuItemToggleType> ToggleTypeProperty =
            AvaloniaProperty.Register<MenuItem, MenuItemToggleType>(nameof(ToggleType));

        /// <summary>
        /// Defines the <see cref="IsChecked"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsCheckedProperty =
            AvaloniaProperty.Register<MenuItem, bool>(nameof(IsChecked));

        /// <summary>
        /// Defines the <see cref="GroupName"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> GroupNameProperty =
            RadioButton.GroupNameProperty.AddOwner<MenuItem>();
        
        /// <summary>
        /// Defines the <see cref="Click"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
            RoutedEvent.Register<MenuItem, RoutedEventArgs>(
                nameof(Click),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PointerEnteredItem"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> PointerEnteredItemEvent =
            RoutedEvent.Register<MenuItem, RoutedEventArgs>(
                nameof(PointerEnteredItem),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PointerExitedItem"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> PointerExitedItemEvent =
            RoutedEvent.Register<MenuItem, RoutedEventArgs>(
                nameof(PointerExitedItem),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="SubmenuOpened"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> SubmenuOpenedEvent =
            RoutedEvent.Register<MenuItem, RoutedEventArgs>(
                nameof(SubmenuOpened),
                RoutingStrategies.Bubble);

        /// <summary>
        /// The default value for the <see cref="ItemsControl.ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<Panel?> DefaultPanel =
            new(() => new StackPanel());

        private bool _commandCanExecute = true;
        private bool _commandBindingError;
        private Popup? _popup;
        private KeyGesture? _hotkey;
        private bool _isEmbeddedInMenu;

        /// <summary>
        /// Initializes static members of the <see cref="MenuItem"/> class.
        /// </summary>
        static MenuItem()
        {
            SelectableMixin.Attach<MenuItem>(IsSelectedProperty);
            PressedMixin.Attach<MenuItem>();
            FocusableProperty.OverrideDefaultValue<MenuItem>(true);
            ItemsPanelProperty.OverrideDefaultValue<MenuItem>(DefaultPanel);
            ClickEvent.AddClassHandler<MenuItem>((x, e) => x.OnClick(e));
            SubmenuOpenedEvent.AddClassHandler<MenuItem>((x, e) => x.OnSubmenuOpened(e));
        }

        public MenuItem()
        {
            // HACK: This nasty but it's all WPF's fault. Grid uses an inherited attached
            // property to store SharedSizeGroup state, except property inheritance is done
            // down the logical tree. In this case, the control which is setting
            // Grid.IsSharedSizeScope="True" is not in the logical tree. Instead of fixing
            // the way Grid stores shared size state, the developers of WPF just created a
            // binding of the internal state of the visual parent to the menu item. We don't
            // have much choice but to do the same for now unless we want to refactor Grid,
            // which I honestly am not brave enough to do right now. Here's the same hack in
            // the WPF codebase:
            //
            // https://github.com/dotnet/wpf/blob/89537909bdf36bc918e88b37751add46a8980bb0/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/MenuItem.cs#L2126-L2141
            //
            // In addition to the hack from WPF, we also make sure to return null when we have
            // no parent. If we don't do this, inheritance falls back to the logical tree,
            // causing the shared size scope in the parent MenuItem to be used, breaking
            // menu layout.

            var parentSharedSizeScope = this.GetObservable(VisualParentProperty)
                .Select(x =>
                {
                    var parent = x as Control;
                    return parent?.GetObservable(DefinitionBase.PrivateSharedSizeScopeProperty) ??
                           Observable.Return<DefinitionBase.SharedSizeScope?>(null);
                })
                .Switch();

            this.Bind(DefinitionBase.PrivateSharedSizeScopeProperty, parentSharedSizeScope);
        }

        /// <summary>
        /// Occurs when a <see cref="MenuItem"/> without a submenu is clicked.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        /// <summary>
        /// Occurs when the pointer enters a menu item.
        /// </summary>
        /// <remarks>
        /// A bubbling version of the <see cref="InputElement.PointerEntered"/> event for menu items.
        /// </remarks>
        public event EventHandler<RoutedEventArgs>? PointerEnteredItem
        {
            add => AddHandler(PointerEnteredItemEvent, value);
            remove => RemoveHandler(PointerEnteredItemEvent, value);
        }

        /// <summary>
        /// Raised when the pointer leaves a menu item.
        /// </summary>
        /// <remarks>
        /// A bubbling version of the <see cref="InputElement.PointerExited"/> event for menu items.
        /// </remarks>
        public event EventHandler<RoutedEventArgs>? PointerExitedItem
        {
            add => AddHandler(PointerExitedItemEvent, value);
            remove => RemoveHandler(PointerExitedItemEvent, value);
        }

        /// <summary>
        /// Occurs when a <see cref="MenuItem"/>'s submenu is opened.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? SubmenuOpened
        {
            add => AddHandler(SubmenuOpenedEvent, value);
            remove => RemoveHandler(SubmenuOpenedEvent, value);
        }

        /// <summary>
        /// Gets or sets the command associated with the menu item.
        /// </summary>
        public ICommand? Command
        {
            get => GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// Gets or sets an <see cref="KeyGesture"/> associated with this control
        /// </summary>
        public KeyGesture? HotKey
        {
            get => GetValue(HotKeyProperty);
            set => SetValue(HotKeyProperty, value);
        }

        /// <summary>
        /// Gets or sets the parameter to pass to the <see cref="Command"/> property of a
        /// <see cref="MenuItem"/>.
        /// </summary>
        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        /// <summary>
        /// Gets or sets the icon that appears in a <see cref="MenuItem"/>.
        /// </summary>
        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Gets or sets the input gesture that will be displayed in the menu item.
        /// </summary>
        /// <remarks>
        /// Setting this property does not cause the input gesture to be handled by the menu item,
        /// it simply displays the gesture text in the menu.
        /// </remarks>
        public KeyGesture? InputGesture
        {
            get => GetValue(InputGestureProperty);
            set => SetValue(InputGestureProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="MenuItem"/> is currently selected.
        /// </summary>
        public bool IsSelected
        {
            get => GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the submenu of the <see cref="MenuItem"/> is
        /// open.
        /// </summary>
        public bool IsSubMenuOpen
        {
            get => GetValue(IsSubMenuOpenProperty);
            set => SetValue(IsSubMenuOpenProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates the submenu that this <see cref="MenuItem"/> is
        /// within should not close when this item is clicked.
        /// </summary>
        public bool StaysOpenOnClick
        {
            get => GetValue(StaysOpenOnClickProperty);
            set => SetValue(StaysOpenOnClickProperty, value);
        }
        
        /// <inheritdoc cref="IMenuItem.ToggleType" />
        public MenuItemToggleType ToggleType
        {
            get => GetValue(ToggleTypeProperty);
            set => SetValue(ToggleTypeProperty, value);
        }

        /// <inheritdoc cref="IMenuItem.IsChecked"/>
        public bool IsChecked
        {
            get => GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }
        
        bool IRadioButton.IsChecked
        {
            get => IsChecked;
            set => SetCurrentValue(IsCheckedProperty, value);
        }

        /// <inheritdoc cref="IMenuItem.GroupName"/>
        public string? GroupName
        {
            get => GetValue(GroupNameProperty);
            set => SetValue(GroupNameProperty, value);
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
        bool IMenuItem.IsPointerOverSubMenu => _popup?.IsPointerOverPopup ?? false;

        /// <inheritdoc/>
        IMenuElement? IMenuItem.Parent => Parent as IMenuElement;

        protected override bool IsEnabledCore => base.IsEnabledCore && _commandCanExecute;

        /// <inheritdoc/>
        bool IMenuElement.MoveSelection(NavigationDirection direction, bool wrap) => MoveSelection(direction, wrap);

        /// <inheritdoc/>
        IMenuItem? IMenuElement.SelectedItem
        {
            get
            {
                var index = SelectedIndex;
                return (index != -1) ?
                    (IMenuItem?)ContainerFromIndex(index) :
                    null;
            }
            set => SelectedIndex = value is Control c ? IndexFromContainer(c) : -1;
        }

        /// <inheritdoc/>
        IEnumerable<IMenuItem> IMenuElement.SubItems => LogicalChildren.OfType<IMenuItem>();

        private IMenuInteractionHandler? MenuInteractionHandler => this.FindLogicalAncestorOfType<MenuBase>()?.InteractionHandler;

        /// <summary>
        /// Opens the submenu.
        /// </summary>
        /// <remarks>
        /// This has the same effect as setting <see cref="IsSubMenuOpen"/> to true.
        /// </remarks>
        public void Open() => SetCurrentValue(IsSubMenuOpenProperty, true);

        /// <summary>
        /// Closes the submenu.
        /// </summary>
        /// <remarks>
        /// This has the same effect as setting <see cref="IsSubMenuOpen"/> to false.
        /// </remarks>
        public void Close() => SetCurrentValue(IsSubMenuOpenProperty, false);

        /// <inheritdoc/>
        void IMenuItem.RaiseClick() => RaiseEvent(new RoutedEventArgs(ClickEvent));

        protected internal override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            return new MenuItem();
        }

        protected internal override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            if (item is MenuItem or Separator)
            {
                recycleKey = null;
                return false;
            }

            recycleKey = DefaultRecycleKey;
            return true;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (!_isEmbeddedInMenu)
            {
                //Normally the Menu's IMenuInteractionHandler is sending the click events for us
                //However when the item is not embedded into a menu we need to send them ourselves.
                RaiseEvent(new RoutedEventArgs(ClickEvent));
            }
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            if (_hotkey != null) // Control attached again, set Hotkey to create a hotkey manager for this control
            {
                SetCurrentValue(HotKeyProperty, _hotkey);
            }
            
            base.OnAttachedToLogicalTree(e);

            if (Command != null)
            {
                Command.CanExecuteChanged += CanExecuteChanged;
            }
            
            TryUpdateCanExecute();

            var parent = Parent;

            while (parent is MenuItem)
            {
                parent = parent.Parent;
            }

            _isEmbeddedInMenu = parent?.FindLogicalAncestorOfType<IMenu>(true) != null;
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            
            TryUpdateCanExecute();
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            // This will cause the hotkey manager to dispose the observer and the reference to this control
            if (HotKey != null)
            {
                _hotkey = HotKey;
                SetCurrentValue(HotKeyProperty, null);
            }

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
        protected override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);
            RaiseEvent(new RoutedEventArgs(PointerEnteredItemEvent));
        }

        /// <inheritdoc/>
        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            RaiseEvent(new RoutedEventArgs(PointerExitedItemEvent));
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
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
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

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new MenuItemAutomationPeer(this);
        }

        protected override void UpdateDataValidation(
            AvaloniaProperty property,
            BindingValueType state,
            Exception? error)
        {
            base.UpdateDataValidation(property, state, error);
            if (property == CommandProperty)
            {
                _commandBindingError = state == BindingValueType.BindingError;
                if (_commandBindingError && _commandCanExecute)
                {
                    _commandCanExecute = false;
                    UpdateIsEffectivelyEnabled();
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
            if (e.Sender is MenuItem menuItem &&
                ((ILogical)menuItem).IsAttachedToLogicalTree)
            {
                if (e.OldValue is ICommand oldCommand)
                {
                    oldCommand.CanExecuteChanged -= menuItem.CanExecuteChanged;
                }

                if (e.NewValue is ICommand newCommand)
                {
                    newCommand.CanExecuteChanged += menuItem.CanExecuteChanged;
                }

                menuItem.TryUpdateCanExecute();
            }
        }

        /// <summary>
        /// Called when the <see cref="CommandParameter"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void CommandParameterChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is MenuItem menuItem)
            {
                menuItem.TryUpdateCanExecute();
            }
        }

        /// <summary>
        /// Called when the <see cref="ICommand.CanExecuteChanged"/> event fires.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void CanExecuteChanged(object? sender, EventArgs e)
        {
            TryUpdateCanExecute();
        }

        /// <summary>
        /// Tries to evaluate CanExecute value of a Command if menu is opened
        /// </summary>
        private void TryUpdateCanExecute()
        {
            if (Command == null)
            {
                _commandCanExecute = !_commandBindingError;
                UpdateIsEffectivelyEnabled();
                return;
            }
            
            //Perf optimization - only raise CanExecute event if the menu is open
            if (!((ILogical)this).IsAttachedToLogicalTree ||
                Parent is MenuItem { IsSubMenuOpen: false })
            {
                return;
            }
            
            var canExecute = Command.CanExecute(CommandParameter);
            if (canExecute != _commandCanExecute)
            {
                _commandCanExecute = canExecute;
                UpdateIsEffectivelyEnabled();
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == HeaderProperty)
            {
                HeaderChanged(change);
            }
            else if (change.Property == IconProperty)
            {
                IconChanged(change);
            }
            else if (change.Property == IsSelectedProperty)
            {
                IsSelectedChanged(change);
            }
            else if (change.Property == IsSubMenuOpenProperty)
            {
                SubMenuOpenChanged(change);
            }
            else if (change.Property == CommandProperty)
            {
                CommandChanged(change);
            }
            else if (change.Property == CommandParameterProperty)
            {
                CommandParameterChanged(change);
            }
            else if (change.Property == IsCheckedProperty)
            {
                IsCheckedChanged(change);
            }
            else if (change.Property == ToggleTypeProperty)
            {
                ToggleTypeChanged(change);
            }
            else if (change.Property == GroupNameProperty)
            {
                GroupNameChanged(change);
            }
        }
        /// <summary>
        /// Called when the <see cref="GroupName"/> property changes.
        /// </summary>
        /// <param name="e">The property change event.</param>
        private void GroupNameChanged(AvaloniaPropertyChangedEventArgs e)
        {
            (MenuInteractionHandler as DefaultMenuInteractionHandler)?.OnGroupOrTypeChanged(this, e.GetOldValue<string>());
        }

        /// <summary>
        /// Called when the <see cref="ToggleType"/> property changes.
        /// </summary>
        /// <param name="e">The property change event.</param>
        private void ToggleTypeChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var newValue = e.GetNewValue<MenuItemToggleType>();
            PseudoClasses.Set(":radio", newValue == MenuItemToggleType.Radio);
            PseudoClasses.Set(":toggle", newValue == MenuItemToggleType.CheckBox);

            (MenuInteractionHandler as DefaultMenuInteractionHandler)?.OnGroupOrTypeChanged(this, GroupName);
        }

        /// <summary>
        /// Called when the <see cref="IsChecked"/> property changes.
        /// </summary>
        /// <param name="e">The property change event.</param>
        private void IsCheckedChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var newValue = e.GetNewValue<bool>();
            PseudoClasses.Set(":checked", newValue);

            if (newValue)
            {
                (MenuInteractionHandler as DefaultMenuInteractionHandler)?.OnCheckedChanged(this);
            }
        }
        
        /// <summary>
        /// Called when the <see cref="HeaderedSelectingItemsControl.Header"/> property changes.
        /// </summary>
        /// <param name="e">The property change event.</param>
        private void HeaderChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var (oldValue, newValue) = e.GetOldAndNewValue<object?>();
            if (Equals(newValue, "-"))
            {
                PseudoClasses.Add(":separator");
                Focusable = false;
            }
            else if (Equals(oldValue, "-"))
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
            var (oldValue, newValue) = e.GetOldAndNewValue<object?>();

            if (oldValue is ILogical oldLogical)
            {
                LogicalChildren.Remove(oldLogical);
                PseudoClasses.Remove(":icon");
            }

            if (newValue is ILogical newLogical)
            {
                LogicalChildren.Add(newLogical);
                PseudoClasses.Add(":icon");
            }
        }

        /// <summary>
        /// Called when the <see cref="IsSelected"/> property changes.
        /// </summary>
        /// <param name="e">The property change event.</param>
        private void IsSelectedChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var parentMenu = Parent as Menu;

            if ((bool)e.NewValue! && (parentMenu is null || parentMenu.IsOpen))
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
            var value = (bool)e.NewValue!;

            if (value)
            {
                foreach (var item in ItemsView.OfType<MenuItem>())
                {
                    item.TryUpdateCanExecute();
                }

                RaiseEvent(new RoutedEventArgs(SubmenuOpenedEvent));
                SetCurrentValue(IsSelectedProperty, true);
                PseudoClasses.Add(":open");
            }
            else
            {
                CloseSubmenus();
                SelectedIndex = -1;
                PseudoClasses.Remove(":open");
            }
        }

        /// <summary>
        /// Called when the submenu's <see cref="Popup"/> is opened.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void PopupOpened(object? sender, EventArgs e)
        {
            // If we're using overlay popups, there's a chance we need to do a layout pass before
            // the child items are added to the visual tree. If we don't do this here, then
            // selection breaks.
            if (Presenter?.IsAttachedToVisualTree == false)
                UpdateLayout();

            var selected = SelectedIndex;

            if (selected != -1)
            {
                var container = ContainerFromIndex(selected);
                container?.Focus();
            }
        }

        /// <summary>
        /// Called when the submenu's <see cref="Popup"/> is closed.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void PopupClosed(object? sender, EventArgs e)
        {
            SelectedItem = null;
        }

        void ICommandSource.CanExecuteChanged(object sender, EventArgs e) => this.CanExecuteChanged(sender, e);

        void IClickableControl.RaiseClick()
        {
            if (IsEffectivelyEnabled)
            {
                RaiseEvent(new RoutedEventArgs(ClickEvent));
            }
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
            public object? GetService(Type serviceType)
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
