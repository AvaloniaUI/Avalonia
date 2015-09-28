using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Perspex.Controls;
using Perspex.Controls.Utils;
using Perspex.Input;

namespace Perspex.Controls
{
    public class HotKeyManager
    {
        public static PerspexProperty<KeyGesture> HotKeyProperty
            = PerspexProperty.RegisterAttached<Control, KeyGesture>("HotKey", typeof (HotKeyManager));

        class HotkeyCommandWrapper : ICommand
        {
            public HotkeyCommandWrapper(IControl control)
            {
                Control = control;
            }

            public IControl Control;

            private ICommand GetCommand() => Control.GetValue(Button.CommandProperty);

            public bool CanExecute(object parameter) => GetCommand()?.CanExecute(parameter) ?? false;

            public void Execute(object parameter) => GetCommand()?.Execute(parameter);

            //Implementation isn't needed in this case
            public event EventHandler CanExecuteChanged;
        }


        class Manager
        {
            private readonly IControl _control;
            private TopLevel _root;
            private IDisposable _parentSub;
            private IDisposable _hotkeySub;
            private KeyGesture _hotkey;
            private HotkeyCommandWrapper _wrapper;
            private KeyBinding _binding;

            public Manager(IControl control)
            {
                _control = control;
                _wrapper = new HotkeyCommandWrapper(_control);
            }

            public void Init()
            {
                _hotkeySub = _control.GetObservable(HotKeyProperty).Subscribe(OnHotkeyChanged);
                _parentSub = AncestorFinder.Create(_control, typeof (TopLevel)).Subscribe(OnParentChanged);
            }

            private void OnParentChanged(IControl control)
            {
                Unregister();
                _root = (TopLevel) control;
                Register();
            }

            private void OnHotkeyChanged(KeyGesture hotkey)
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
                    _binding = new KeyBinding() {Gesture = _hotkey, Command = _wrapper};
                    _root.KeyBindings.Add(_binding);
                }
            }

            void Stop()
            {
                Unregister();
                _parentSub.Dispose();
                _hotkeySub.Dispose();
            }
        }

        static HotKeyManager()
        {
            HotKeyProperty.Changed.Subscribe(args =>
            {
                var control = args.Sender as IControl;
                if (args.OldValue != null|| control == null)
                    return;
                new Manager(control).Init();
            });
        }
        public static void SetHotKey(PerspexObject target, KeyGesture value) => target.SetValue(HotKeyProperty, value);
        public static KeyGesture GetHotKey(PerspexObject target) => target.GetValue(HotKeyProperty);
    }
}
