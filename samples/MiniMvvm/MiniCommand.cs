using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MiniMvvm
{
    public sealed class MiniCommand<T> : MiniCommand, ICommand
    {
        private readonly Action<T> _cb;
        private bool _busy;
        private Func<T, Task> _acb;
        
        public MiniCommand(Action<T> cb)
        {
            _cb = cb;
        }

        public MiniCommand(Func<T, Task> cb)
        {
            _acb = cb;
        }

        private bool Busy
        {
            get => _busy;
            set
            {
                _busy = value;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        
        public override event EventHandler CanExecuteChanged;
        public override bool CanExecute(object parameter) => !_busy;

        public override async void Execute(object parameter)
        {
            if(Busy)
                return;
            try
            {
                Busy = true;
                if (_cb != null)
                    _cb((T)parameter);
                else
                    await _acb((T)parameter);
            }
            finally
            {
                Busy = false;
            }
        }
    }
    
    public abstract class MiniCommand : ICommand
    {
        public static MiniCommand Create(Action cb) => new MiniCommand<object>(_ => cb());
        public static MiniCommand Create<TArg>(Action<TArg> cb) => new MiniCommand<TArg>(cb);
        public static MiniCommand CreateFromTask(Func<Task> cb) => new MiniCommand<object>(_ => cb());
        
        public abstract bool CanExecute(object parameter);
        public abstract void Execute(object parameter);
        public abstract event EventHandler CanExecuteChanged;
    }
}
