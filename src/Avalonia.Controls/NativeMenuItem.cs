using System;
using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    public class NativeMenuItem : NativeMenuItemBase, INativeMenuItemExporterEventsImplBridge
    {
        private readonly CanExecuteChangedSubscriber _canExecuteChangedSubscriber;

        class CanExecuteChangedSubscriber : IWeakEventSubscriber<EventArgs>
        {
            private readonly NativeMenuItem _parent;

            public CanExecuteChangedSubscriber(NativeMenuItem parent)
            {
                _parent = parent;
            }

            public void OnEvent(object? sender, WeakEvent ev, EventArgs e)
            {
                _parent.CanExecuteChanged();
            }
        }


        public NativeMenuItem()
        {
            _canExecuteChangedSubscriber = new CanExecuteChangedSubscriber(this);
        }

        public NativeMenuItem(string header) : this()
        {
            Header = header;
        }

        public static readonly StyledProperty<NativeMenu?> MenuProperty =
            AvaloniaProperty.Register<NativeMenuItem, NativeMenu?>(nameof(Menu), coerce: CoerceMenu);

        [Content]
        public NativeMenu? Menu
        {
            get => GetValue(MenuProperty);
            set => SetValue(MenuProperty, value);
        }

        private static NativeMenu? CoerceMenu(AvaloniaObject sender, NativeMenu? value)
        {
            if (value != null && value.Parent != null && value.Parent != sender)
                throw new InvalidOperationException("NativeMenu already has a parent");
            return value;
        }

        /// <inheritdoc cref="MenuItem.IconProperty"/>
        public static readonly StyledProperty<Bitmap?> IconProperty =
            AvaloniaProperty.Register<NativeMenuItem, Bitmap?>(nameof(Icon));

        /// <inheritdoc cref="MenuItem.Icon"/>
        public Bitmap? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <inheritdoc cref="MenuItem.HeaderProperty"/>
        public static readonly StyledProperty<string?> HeaderProperty =
            AvaloniaProperty.Register<NativeMenuItem, string?>(nameof(Header));

        /// <inheritdoc cref="MenuItem.Header"/>
        public string? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Defines the <see cref="ToolTip"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> ToolTipProperty =
            AvaloniaProperty.Register<NativeMenuItem, string?>(nameof(ToolTip));

        /// <summary>
        /// Gets or sets the tooltip associated with the menu item.
        /// This may not be supported by the native menu provider, but
        /// will be passed on to the non-native fallback menu item if used.
        /// </summary>
        public string? ToolTip
        {
            get => GetValue(ToolTipProperty);
            set => SetValue(ToolTipProperty, value);
        }

        /// <inheritdoc cref="MenuItem.InputGestureProperty"/>
        public static readonly StyledProperty<KeyGesture?> GestureProperty =
            AvaloniaProperty.Register<NativeMenuItem, KeyGesture?>(nameof(Gesture));

        /// <inheritdoc cref="MenuItem.InputGesture"/>
        public KeyGesture? Gesture
        {
            get => GetValue(GestureProperty);
            set => SetValue(GestureProperty, value);
        }

        /// <inheritdoc cref="MenuItem.IsCheckedProperty"/>
        public static readonly StyledProperty<bool> IsCheckedProperty =
            MenuItem.IsCheckedProperty.AddOwner<NativeMenuItem>();

        /// <inheritdoc cref="MenuItem.IsChecked"/>
        public bool IsChecked
        {
            get => GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }
        
        /// <inheritdoc cref="MenuItem.ToggleTypeProperty"/>
        public static readonly StyledProperty<NativeMenuItemToggleType> ToggleTypeProperty =
            AvaloniaProperty.Register<NativeMenuItem, NativeMenuItemToggleType>(nameof(ToggleType));

        /// <inheritdoc cref="MenuItem.ToggleType"/>
        public NativeMenuItemToggleType ToggleType
        {
            get => GetValue(ToggleTypeProperty);
            set => SetValue(ToggleTypeProperty, value);
        }

        /// <inheritdoc cref="MenuItem.CommandProperty"/>
        public static readonly StyledProperty<ICommand?> CommandProperty =
            MenuItem.CommandProperty.AddOwner<NativeMenuItem>(new(enableDataValidation: true));

        /// <inheritdoc cref="MenuItem.CommandParameterProperty"/>
        public static readonly StyledProperty<object?> CommandParameterProperty =
            MenuItem.CommandParameterProperty.AddOwner<NativeMenuItem>();

        public static readonly StyledProperty<bool> IsEnabledProperty =
           AvaloniaProperty.Register<NativeMenuItem, bool>(nameof(IsEnabled), true);

        /// <inheritdoc cref="MenuItem.IsEnabled"/>
        public bool IsEnabled
        {
            get => GetValue(IsEnabledProperty);
            set => SetValue(IsEnabledProperty, value);
        }

        /// <summary>
        /// Defines the <see cref="IsVisible"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsVisibleProperty =
           Visual.IsVisibleProperty.AddOwner<NativeMenuItem>();

        /// <summary>
        /// Gets or sets a value indicating whether this menu item is visible.
        /// </summary>
        public bool IsVisible
        {
            get => GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }

        void CanExecuteChanged()
        {
            SetCurrentValue(IsEnabledProperty, Command?.CanExecute(CommandParameter) ?? true);
        }

        public bool HasClickHandlers => Click != null;

        /// <inheritdoc cref="MenuItem.Command"/>
        public ICommand? Command
        {
            get => GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <inheritdoc cref="MenuItem.CommandParameter"/>
        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        /// <summary>
        /// Occurs when a <see cref="NativeMenuItem"/> is clicked.
        /// </summary>
        public event EventHandler? Click;

        void INativeMenuItemExporterEventsImplBridge.RaiseClicked()
        {
            Click?.Invoke(this, new EventArgs());

            if (Command?.CanExecute(CommandParameter) == true)
            {
                Command.Execute(CommandParameter);
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == MenuProperty && change.NewValue is NativeMenu newMenu)
            {
                if (newMenu.Parent != null && newMenu.Parent != this)
                    throw new InvalidOperationException("NativeMenu already has a parent");
                newMenu.Parent = this;
            }
            else if (change.Property == CommandProperty)
            {
                if (change.OldValue is ICommand oldCommand)
                    WeakEvents.CommandCanExecuteChanged.Unsubscribe(oldCommand, _canExecuteChangedSubscriber);
                if (change.NewValue is ICommand newCommand)
                    WeakEvents.CommandCanExecuteChanged.Subscribe(newCommand, _canExecuteChangedSubscriber);
                CanExecuteChanged();
            }
        }
    }

    // TODO12: remove this enum and use MenuItemToggleType only 
    public enum NativeMenuItemToggleType
    {
        None = MenuItemToggleType.None,
        CheckBox = MenuItemToggleType.CheckBox,
        Radio = MenuItemToggleType.Radio
    }
}
