using System;
using System.Windows.Input;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    public class HotKeyManager
    {
        public static readonly AttachedProperty<KeyGesture?> HotKeyProperty
            = AvaloniaProperty.RegisterAttached<Control, KeyGesture?>("HotKey", typeof(HotKeyManager));

        class HotkeyCommandWrapper : ICommand
        {
            readonly WeakReference reference;

            public HotkeyCommandWrapper(Control control)
            {
                reference = new WeakReference(control);
            }

            public ICommand? GetCommand()
            {
                if (reference.Target is { } target)
                {
                    if (target is ICommandSource commandSource && commandSource.Command is { } command)
                    {
                        return command;
                    }
                    else if (target is IClickableControl { })
                    {
                        return this;
                    }
                }
                return null;
            }

            public bool CanExecute(object? parameter)
            {
                if (reference.Target is { } target)
                {
                    if (target is ICommandSource commandSource && commandSource.Command is { } command)
                    {
                        return commandSource.IsEffectivelyEnabled
                            && command.CanExecute(commandSource.CommandParameter) == true;
                    }
                    else if (target is IClickableControl clickable)
                    {
                        return clickable.IsEffectivelyEnabled;
                    }
                }
                return false;
            }

            public void Execute(object? parameter)
            {
                if (reference.Target is { } target)
                {
                    if (target is ICommandSource commandSource && commandSource.Command is { } command)
                    {
                        command.Execute(commandSource.CommandParameter);
                    }
                    else if (target is IClickableControl { IsEffectivelyEnabled: true } clickable)
                    {
                        clickable.RaiseClick();
                    }
                }
            }


#pragma warning disable 67 // Event not used
            public event EventHandler? CanExecuteChanged;
#pragma warning restore 67
        }


        class Manager
        {
            private readonly Control _control;
            private TopLevel? _root;
            private IDisposable? _parentSub;
            private IDisposable? _hotkeySub;
            private KeyGesture? _hotkey;
            private readonly HotkeyCommandWrapper _wrapper;
            private KeyBinding? _binding;

            public Manager(Control control)
            {
                _control = control;
                _wrapper = new HotkeyCommandWrapper(_control);
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
                if (args.NewValue.Value is null)
                    return;

                var control = args.Sender as Control;
                if (control is not IClickableControl and not ICommandSource)
                {
                    Logging.Logger.TryGet(Logging.LogEventLevel.Warning, Logging.LogArea.Control)?.Log(control,
                        $"The element {args.Sender.GetType().Name} does not implement IClickableControl nor ICommandSource and does not support binding a HotKey ({args.NewValue}).");
                    return;
                }

                new Manager(control).Init();
            });
        }
        public static void SetHotKey(AvaloniaObject target, KeyGesture value) => target.SetValue(HotKeyProperty, value);
        public static KeyGesture? GetHotKey(AvaloniaObject target) => target.GetValue(HotKeyProperty);
    }
}
