using System;
using System.Windows.Input;

namespace Avalonia.Controls.UnitTests.Utils;

internal class TestCommand : ICommand
{
    private readonly Func<object, bool> _canExecute;
    private readonly Action<object> _execute;
    private EventHandler _canExecuteChanged;
    private bool _enabled = true;

    public TestCommand(bool enabled = true)
    {
        _enabled = enabled;
        _canExecute = _ => _enabled;
        _execute = _ => { };
    }

    public TestCommand(Func<object, bool> canExecute, Action<object> execute = null)
    {
        _canExecute = canExecute;
        _execute = execute ?? (_ => { });
    }

    public bool IsEnabled
    {
        get { return _enabled; }
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                _canExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public int SubscriptionCount { get; private set; }

    public event EventHandler CanExecuteChanged
    {
        add { _canExecuteChanged += value; ++SubscriptionCount; }
        remove { _canExecuteChanged -= value; --SubscriptionCount; }
    }

    public bool CanExecute(object parameter) => _canExecute(parameter);

    public void Execute(object parameter) => _execute(parameter);

    public void RaiseCanExecuteChanged() => _canExecuteChanged?.Invoke(this, EventArgs.Empty);
}
