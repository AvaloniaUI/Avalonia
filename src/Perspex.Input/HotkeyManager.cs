using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Perspex.Input
{
    public class HotkeyManager
    {
        public static PerspexProperty<KeyGesture> HotKey
            = PerspexProperty.RegisterAttached<Visual, KeyGesture>("HotKey", typeof (HotkeyManager));

        class HotkeyCommandWrapper : ICommand
        {
            public Visual Visual;
            public PerspexProperty Property;

            ICommand GetCommand() => (ICommand) Visual.GetValue(Property);

            public bool CanExecute(object parameter) => GetCommand()?.CanExecute(parameter) ?? false;

            public void Execute(object parameter) => GetCommand()?.Execute(parameter);

            //Implementation isn't needed in this case
            public event EventHandler CanExecuteChanged;
        }


        static HotkeyManager()
        {
            HotKey.Changed.Subscribe(args =>
            {
                if (args.OldValue != null)
                {
                    
                }


            });
        }

    }
}
