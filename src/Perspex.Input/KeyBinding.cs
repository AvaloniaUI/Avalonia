using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Perspex.Input
{
    public class KeyBinding : PerspexObject
    {
        public static PerspexProperty<ICommand> CommandProperty =
            PerspexProperty.Register<KeyBinding, ICommand>("Command");

        public ICommand Command
        {
            get { return GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static PerspexProperty<KeyGesture> GestureProperty =
            PerspexProperty.Register<KeyBinding, KeyGesture>("Gesture");

        public KeyGesture Gesture
        {
            get { return GetValue(GestureProperty); }
            set { SetValue(GestureProperty, value); }
        }

        public void TryHandle(KeyEventArgs args)
        {
            if (Gesture?.Matches(args) == true)
            {
                args.Handled = true;
                if (Command?.CanExecute(null) == true)
                    Command.Execute(null);
            }
        }
    }
}
