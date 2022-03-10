using System;
using System.Windows.Input;
using Avalonia.Controls.Utils;
using Avalonia.Input;

namespace Avalonia.Controls
{
    public class HotKeyManager
    {
        public static readonly AttachedProperty<KeyGesture?> HotKeyProperty
            = AvaloniaProperty.RegisterAttached<Control, KeyGesture?>("HotKey", typeof(HotKeyManager));

        class HotkeyCommandWrapper : ICommand
        {
            public HotkeyCommandWrapper(ICommandSource? control)
            {
                CommandSource = control;
            }

            public readonly ICommandSource? CommandSource;

            private ICommand? GetCommand() => CommandSource?.Command;

            public bool CanExecute(object? parameter) =>
                CommandSource?.Command?.CanExecute(CommandSource.CommandParameter) == true
                && CommandSource.IsEffectivelyEnabled;

            public void Execute(object? parameter) =>
                GetCommand()?.Execute(CommandSource?.CommandParameter);

#pragma warning disable 67 // Event not used
            public event EventHandler? CanExecuteChanged;
#pragma warning restore 67
        }


        class Manager
        {
            private readonly IControl _control;
            private TopLevel? _root;
            private IDisposable? _parentSub;
            private IDisposable? _hotkeySub;
            private KeyGesture? _hotkey;
            private readonly HotkeyCommandWrapper _wrapper;
            private KeyBinding? _binding;

            public Manager(IControl control)
            {
                _control = control;
                _wrapper = new HotkeyCommandWrapper(_control as ICommandSource);
            }

            public void Init()
            {
                _hotkeySub = _control.GetObservable(HotKeyProperty).Subscribe(OnHotkeyChanged);
                _parentSub = AncestorFinder.Create<TopLevel>(_control).Subscribe(OnParentChanged);
            }

            private void OnParentChanged(TopLevel? control)
            {
                Unregister();
                _root = control;
                Register();
            }

            private void OnHotkeyChanged(KeyGesture? hotkey)
            {
                if (hotkey == null)
                    //Subscription will be recreated by static property watcher
                    Stop();
                else
                {
                    Unregister();
                    _hotkey = hotkey;
                    Register();
                }
            }

            void Unregister()
            {
                if (_root != null && _binding != null)
                    _root.KeyBindings.Remove(_binding);
                _binding = null;
            }

            void Register()
            {
                if (_root != null && _hotkey != null)
                {
                    _binding = new KeyBinding() { Gesture = _hotkey, Command = _wrapper };
                    _root.KeyBindings.Add(_binding);
                }
            }

            void Stop()
            {
                Unregister();
                _parentSub?.Dispose();
                _hotkeySub?.Dispose();
            }
        }

        static HotKeyManager()
        {
            HotKeyProperty.Changed.Subscribe(args =>
            {
                if (args.NewValue.Value is null) return;

                var control = args.Sender as IControl;
                if (control is not ICommandSource)
                {
                    Logging.Logger.TryGet(Logging.LogEventLevel.Warning, Logging.LogArea.Control)?.Log(control,
                        $"The element {args.Sender.GetType().Name} does not implement ICommandSource and does not support binding a HotKey ({args.NewValue}).");
                    return;
                }

                new Manager(control).Init();
            });
        }
        public static void SetHotKey(AvaloniaObject target, KeyGesture value) => target.SetValue(HotKeyProperty, value);
        public static KeyGesture? GetHotKey(AvaloniaObject target) => target.GetValue(HotKeyProperty);
    }
}
