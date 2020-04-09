using System;
using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    public class NativeMenuItem : NativeMenuItemBase
    {
        private string _header;
        private KeyGesture _gesture;
        private bool _enabled = true;
        private ICommand _command;

        private NativeMenu _menu;

        static NativeMenuItem()
        {
            MenuProperty.Changed.Subscribe(args =>
            {
                var item = (NativeMenuItem)args.Sender;
                var value = (NativeMenu)args.NewValue;
                if (value.Parent != null && value.Parent != item)
                    throw new InvalidOperationException("NativeMenu already has a parent");
                value.Parent = item;
            });
        }


        class CanExecuteChangedSubscriber : IWeakSubscriber<EventArgs>
        {
            private readonly NativeMenuItem _parent;

            public CanExecuteChangedSubscriber(NativeMenuItem parent)
            {
                _parent = parent;
            }

            public void OnEvent(object sender, EventArgs e)
            {
                _parent.CanExecuteChanged();
            }
        }

        private readonly CanExecuteChangedSubscriber _canExecuteChangedSubscriber;


        public NativeMenuItem()
        {
            _canExecuteChangedSubscriber = new CanExecuteChangedSubscriber(this);
        }

        public NativeMenuItem(string header) : this()
        {
            Header = header;
        }

        public static readonly DirectProperty<NativeMenuItem, NativeMenu> MenuProperty =
            AvaloniaProperty.RegisterDirect<NativeMenuItem, NativeMenu>(nameof(Menu), o => o.Menu, (o, v) => o.Menu = v);

        public NativeMenu Menu
        {
            get => _menu;
            set
            {
                if (value.Parent != null && value.Parent != this)
                    throw new InvalidOperationException("NativeMenu already has a parent");
                SetAndRaise(MenuProperty, ref _menu, value);
            }
        }

        public static readonly DirectProperty<NativeMenuItem, string> HeaderProperty =
            AvaloniaProperty.RegisterDirect<NativeMenuItem, string>(nameof(Header), o => o.Header, (o, v) => o.Header = v);

        public string Header
        {
            get => _header;
            set => SetAndRaise(HeaderProperty, ref _header, value);
        }

        public static readonly DirectProperty<NativeMenuItem, KeyGesture> GestureProperty =
            AvaloniaProperty.RegisterDirect<NativeMenuItem, KeyGesture>(nameof(Gesture), o => o.Gesture, (o, v) => o.Gesture = v);

        public KeyGesture Gesture
        {
            get => _gesture;
            set => SetAndRaise(GestureProperty, ref _gesture, value);
        }        

        public static readonly DirectProperty<NativeMenuItem, ICommand> CommandProperty =
           AvaloniaProperty.RegisterDirect<NativeMenuItem, ICommand>(nameof(Command),
               o => o.Command, (o, v) => o.Command = v);

        /// <summary>
        /// Defines the <see cref="CommandParameter"/> property.
        /// </summary>
        public static readonly StyledProperty<object> CommandParameterProperty =
            Button.CommandParameterProperty.AddOwner<MenuItem>();

        public static readonly DirectProperty<NativeMenuItem, bool> EnabledProperty =
           AvaloniaProperty.RegisterDirect<NativeMenuItem, bool>(nameof(Enabled), o => o.Enabled, (o, v) => o.Enabled = v, true);

        public bool Enabled
        {
            get => _enabled;
            set => SetAndRaise(EnabledProperty, ref _enabled, value);
        }

        void CanExecuteChanged()
        {
            Enabled = _command?.CanExecute(null) ?? true;
        }

        public bool HasClickHandlers => Clicked != null;

        public ICommand Command
        {
            get => _command;
            set
            {
                if (_command != null)
                    WeakSubscriptionManager.Unsubscribe(_command,
                        nameof(ICommand.CanExecuteChanged), _canExecuteChangedSubscriber);

                SetAndRaise(CommandProperty, ref _command, value);
                
                if (_command != null)
                    WeakSubscriptionManager.Subscribe(_command,
                        nameof(ICommand.CanExecuteChanged), _canExecuteChangedSubscriber);
                
                CanExecuteChanged();                
            }
        }

        /// <summary>
        /// Gets or sets the parameter to pass to the <see cref="Command"/> property of a
        /// <see cref="NativeMenuItem"/>.
        /// </summary>
        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public event EventHandler Clicked;

        public void RaiseClick()
        {
            Clicked?.Invoke(this, new EventArgs());

            if (Command?.CanExecute(CommandParameter) == true)
            {
                Command.Execute(CommandParameter);
            }
        }
    }
}
