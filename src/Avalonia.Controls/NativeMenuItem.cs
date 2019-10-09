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
            AvaloniaProperty.RegisterDirect<NativeMenuItem, NativeMenu>(nameof(Menu), o => o._menu,
                (o, v) =>
                {
                    if (v.Parent != null && v.Parent != o)
                        throw new InvalidOperationException("NativeMenu already has a parent");
                    o._menu = v;
                });

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
            AvaloniaProperty.RegisterDirect<NativeMenuItem, string>(nameof(Header), o => o._header, (o, v) => o._header = v);

        public string Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DirectProperty<NativeMenuItem, KeyGesture> GestureProperty =
            AvaloniaProperty.RegisterDirect<NativeMenuItem, KeyGesture>(nameof(Gesture), o => o._gesture, (o, v) => o._gesture = v);

        public KeyGesture Gesture
        {
            get => GetValue(GestureProperty);
            set => SetValue(GestureProperty, value);
        }

        private ICommand _command;

        public static readonly DirectProperty<NativeMenuItem, ICommand> CommandProperty =
           AvaloniaProperty.RegisterDirect<NativeMenuItem, ICommand>(nameof(Command),
               o => o._command, (o, v) =>
               {
                   if (o._command != null)
                       WeakSubscriptionManager.Unsubscribe(o._command,
                           nameof(ICommand.CanExecuteChanged), o._canExecuteChangedSubscriber);
                   o._command = v;
                   if (o._command != null)
                       WeakSubscriptionManager.Subscribe(o._command,
                           nameof(ICommand.CanExecuteChanged), o._canExecuteChangedSubscriber);
                   o.CanExecuteChanged();
               });

        /// <summary>
        /// Defines the <see cref="CommandParameter"/> property.
        /// </summary>
        public static readonly StyledProperty<object> CommandParameterProperty =
            Button.CommandParameterProperty.AddOwner<MenuItem>();

        public static readonly DirectProperty<NativeMenuItem, bool> EnabledProperty =
           AvaloniaProperty.RegisterDirect<NativeMenuItem, bool>(nameof(Enabled), o => o._enabled,
               (o, v) => o._enabled = v, true);

        public bool Enabled
        {
            get => GetValue(EnabledProperty);
            set => SetValue(EnabledProperty, value);
        }

        void CanExecuteChanged()
        {
            Enabled = _command?.CanExecute(null) ?? true;
        }

        public bool HasClickHandlers => Clicked != null;

        public ICommand Command
        {
            get => GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
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
