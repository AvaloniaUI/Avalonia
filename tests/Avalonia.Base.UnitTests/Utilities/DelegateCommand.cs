using System;
using System.Windows.Input;

namespace Avalonia.Base.UnitTests.Utilities;

internal class DelegateCommand : ICommand
{
    private readonly Action _action;
    private readonly Func<object, bool> _canExecute;
    public DelegateCommand(Action action, Func<object, bool> canExecute = default)
    {
        _action = action;
        _canExecute = canExecute ?? new(_ => true);
    }

    public event EventHandler CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object parameter) => _canExecute(parameter);
    public void Execute(object parameter) => _action();
}
