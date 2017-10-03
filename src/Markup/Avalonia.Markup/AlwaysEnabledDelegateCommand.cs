using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Avalonia.Markup
{
    class AlwaysEnabledDelegateCommand : ICommand
    {
        private readonly Delegate action;

        public AlwaysEnabledDelegateCommand(Delegate action)
        {
            this.action = action;
        }

#pragma warning disable 0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore 0067

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            if (action.Method.GetParameters().Length == 0)
            {
                action.DynamicInvoke();
            }
            else
            {
                action.DynamicInvoke(parameter); 
            }
        }
    }
}
