using System;
using System.Windows.Input;
using Avalonia.Collections;
using Avalonia.Metadata;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    public class NativeMenuItem : AvaloniaObject
    {
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

            _subItems = new AvaloniaList<NativeMenuItem>();
        }

        private string _text;
        public static readonly DirectProperty<NativeMenuItem, string> TextProperty =
            AvaloniaProperty.RegisterDirect<NativeMenuItem, string>(nameof(Text), o => o._text, (o, v) => o._text = v);

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }


        private AvaloniaList<NativeMenuItem> _subItems;

        public static readonly DirectProperty<NativeMenuItem, AvaloniaList<NativeMenuItem>> SubItemsProperty =
           AvaloniaProperty.RegisterDirect<NativeMenuItem, AvaloniaList<NativeMenuItem>>(
               nameof(SubItems), o => o._subItems, (o, v) => o._subItems = v);

        [Content]
        public AvaloniaList<NativeMenuItem> SubItems
        {
            get => GetValue(SubItemsProperty);
            set => SetValue(SubItemsProperty, value);
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


        private bool _enabled = true;

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

        public event EventHandler Clicked;

        public void RaiseClick()
        {
            Clicked?.Invoke(this, new EventArgs());
            if (Command?.CanExecute(null) == true)
                Command.Execute(null);
        }
    }
}
