using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia.Collections;
using Avalonia.Metadata;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    public class NativeMenuItem : AvaloniaObject
    {
        private string _header;
        private ICollection<NativeMenuItem> _items;
        private bool _enabled = true;

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

            _items = new AvaloniaList<NativeMenuItem>();
        }

        public static readonly DirectProperty<NativeMenuItem, string> HeaderProperty =
            AvaloniaProperty.RegisterDirect<NativeMenuItem, string>(nameof(Header), o => o._header, (o, v) => o._header = v);

        public string Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DirectProperty<NativeMenuItem, ICollection<NativeMenuItem>> ItemsProperty =
           AvaloniaProperty.RegisterDirect<NativeMenuItem, ICollection<NativeMenuItem>>(
               nameof(Items), o => o._items, (o, v) => o._items = v);

        [Content]
        public ICollection<NativeMenuItem> Items
        {
            get => GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
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
