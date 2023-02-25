using System;
using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Utilities;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    public class NativeMenuItem : NativeMenuItemBase, INativeMenuItemExporterEventsImplBridge
    {
        private string? _header;
        private KeyGesture? _gesture;
        private bool _isEnabled = true;
        private ICommand? _command;
        private bool _isChecked = false;
        private NativeMenuItemToggleType _toggleType;
        private IBitmap? _icon;
        private readonly CanExecuteChangedSubscriber _canExecuteChangedSubscriber;

        private NativeMenu? _menu;

        static NativeMenuItem()
        {
            MenuProperty.Changed.Subscribe(args =>
            {
                var item = (NativeMenuItem)args.Sender;
                var value = args.NewValue.GetValueOrDefault()!;
                if (value.Parent != null && value.Parent != item)
                    throw new InvalidOperationException("NativeMenu already has a parent");
                value.Parent = item;
            });
        }


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

        public static readonly DirectProperty<NativeMenuItem, NativeMenu?> MenuProperty =
            AvaloniaProperty.RegisterDirect<NativeMenuItem, NativeMenu?>(nameof(Menu), o => o.Menu, (o, v) => o.Menu = v);

        [Content]
        public NativeMenu? Menu
        {
            get => _menu;
            set
            {
                if (value != null && value.Parent != null && value.Parent != this)
                    throw new InvalidOperationException("NativeMenu already has a parent");
                SetAndRaise(MenuProperty, ref _menu, value);
            }
        }

        public static readonly DirectProperty<NativeMenuItem, IBitmap?> IconProperty =
            AvaloniaProperty.RegisterDirect<NativeMenuItem, IBitmap?>(nameof(Icon), o => o.Icon, (o, v) => o.Icon = v);


        public IBitmap? Icon
        {
            get => _icon;
            set => SetAndRaise(IconProperty, ref _icon, value);
        }  

        public static readonly DirectProperty<NativeMenuItem, string?> HeaderProperty =
            AvaloniaProperty.RegisterDirect<NativeMenuItem, string?>(nameof(Header), o => o.Header, (o, v) => o.Header = v);

        public string? Header
        {
            get => _header;
            set => SetAndRaise(HeaderProperty, ref _header, value);
        }

        public static readonly DirectProperty<NativeMenuItem, KeyGesture?> GestureProperty =
            AvaloniaProperty.RegisterDirect<NativeMenuItem, KeyGesture?>(nameof(Gesture), o => o.Gesture, (o, v) => o.Gesture = v);

        public KeyGesture? Gesture
        {
            get => _gesture;
            set => SetAndRaise(GestureProperty, ref _gesture, value);
        }

        public static readonly DirectProperty<NativeMenuItem, bool> IsCheckedProperty =
            AvaloniaProperty.RegisterDirect<NativeMenuItem, bool>(
                nameof(IsChecked),
                o => o.IsChecked,
                (o, v) => o.IsChecked = v);

        public bool IsChecked
        {
            get => _isChecked;
            set => SetAndRaise(IsCheckedProperty, ref _isChecked, value);
        }
        
        public static readonly DirectProperty<NativeMenuItem, NativeMenuItemToggleType> ToggleTypeProperty =
            AvaloniaProperty.RegisterDirect<NativeMenuItem, NativeMenuItemToggleType>(
                nameof(ToggleType),
                o => o.ToggleType,
                (o, v) => o.ToggleType = v);

        public NativeMenuItemToggleType ToggleType
        {
            get => _toggleType;
            set => SetAndRaise(ToggleTypeProperty, ref _toggleType, value);
        }

        public static readonly DirectProperty<NativeMenuItem, ICommand?> CommandProperty =
            Button.CommandProperty.AddOwner<NativeMenuItem>(
                menuItem => menuItem.Command,
                (menuItem, command) => menuItem.Command = command,
                enableDataValidation: true);

        /// <summary>
        /// Defines the <see cref="CommandParameter"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> CommandParameterProperty =
            Button.CommandParameterProperty.AddOwner<NativeMenuItem>();

        public static readonly DirectProperty<NativeMenuItem, bool> IsEnabledProperty =
           AvaloniaProperty.RegisterDirect<NativeMenuItem, bool>(nameof(IsEnabled), o => o.IsEnabled, (o, v) => o.IsEnabled = v, true);

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetAndRaise(IsEnabledProperty, ref _isEnabled, value);
        }

        void CanExecuteChanged()
        {
            IsEnabled = _command?.CanExecute(CommandParameter) ?? true;
        }

        public bool HasClickHandlers => Click != null;

        public ICommand? Command
        {
            get => _command;
            set
            {
                if (_command != null)
                    WeakEvents.CommandCanExecuteChanged.Unsubscribe(_command, _canExecuteChangedSubscriber);

                SetAndRaise(CommandProperty, ref _command, value);

                if (_command != null)
                    WeakEvents.CommandCanExecuteChanged.Subscribe(_command, _canExecuteChangedSubscriber);

                CanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets or sets the parameter to pass to the <see cref="Command"/> property of a
        /// <see cref="NativeMenuItem"/>.
        /// </summary>
        public object? CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
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
    }
    
    public enum NativeMenuItemToggleType
    {
        None,
        CheckBox,
        Radio
    }
}
