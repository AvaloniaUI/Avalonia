using System;
using System.Windows.Input;
using Avalonia.Collections;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    public class SimpleMenuItem : AvaloniaObject
    {
        class CanExecuteChangedSubscriber : IWeakSubscriber<EventArgs>
        {
            private readonly SimpleMenuItem _parent;

            public CanExecuteChangedSubscriber(SimpleMenuItem parent)
            {
                _parent = parent;
            }
            
            public void OnEvent(object sender, EventArgs e)
            {
                _parent.CanExecuteChanged();
            }
        }

        private readonly CanExecuteChangedSubscriber _canExecuteChangedSubscriber;
        public SimpleMenuItem()
        {
            _canExecuteChangedSubscriber = new CanExecuteChangedSubscriber(this);
        }
        
        private string _text;
        public static readonly DirectProperty<SimpleMenuItem, string> TextProperty =
            AvaloniaProperty.RegisterDirect<SimpleMenuItem, string>("Text", o => o._text, (o, v) => o._text = v);
        
        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }


        private AvaloniaList<SimpleMenuItem> _subItems;

        public static readonly DirectProperty<SimpleMenuItem, AvaloniaList<SimpleMenuItem>> SubItemsProperty =
            AvaloniaProperty.RegisterDirect<SimpleMenuItem, AvaloniaList<SimpleMenuItem>>(
                "SubItems", o => o._subItems, (o, v) => o._subItems = v);

        public AvaloniaList<SimpleMenuItem> SubItems
        {
            get => GetValue(SubItemsProperty);
            set => SetValue(SubItemsProperty, value);
        }


        private ICommand _command;

        public static readonly DirectProperty<SimpleMenuItem, ICommand> CommandProperty =
            AvaloniaProperty.RegisterDirect<SimpleMenuItem, ICommand>("Command",
                o => o._command, (o, v) =>
                {
                    if (o._command != null)
                        WeakSubscriptionManager.Unsubscribe(o._command,
                            nameof(ICommand.CanExecuteChanged), o._canExecuteChangedSubscriber);
                    o._command = v;
                    if(o._command != null)
                        WeakSubscriptionManager.Subscribe(o._command,
                            nameof(ICommand.CanExecuteChanged), o._canExecuteChangedSubscriber);
                    o.CanExecuteChanged();
                });


        private bool _enabled = true;

        public static readonly DirectProperty<SimpleMenuItem, bool> EnabledProperty =
            AvaloniaProperty.RegisterDirect<SimpleMenuItem, bool>("Enabled", o => o._enabled,
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
