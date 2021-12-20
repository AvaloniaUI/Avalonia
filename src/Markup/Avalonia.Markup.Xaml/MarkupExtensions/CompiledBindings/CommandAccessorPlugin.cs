using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Windows.Input;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;
using Avalonia.Utilities;

namespace Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings
{
    internal class CommandAccessorPlugin : IPropertyAccessorPlugin
    {
        private readonly Func<WeakReference<object>, IPropertyAccessor> _commandAccessorFactory;

        public CommandAccessorPlugin(Func<WeakReference<object>, IPropertyAccessor> commandAccessorFactory)
        {
            _commandAccessorFactory = commandAccessorFactory;
        }

        public bool Match(object obj, string propertyName)
        {
            throw new InvalidOperationException("The CommandAccessorPlugin does not support dynamic matching");
        }

        public IPropertyAccessor Start(WeakReference<object> reference, string propertyName)
        {
            return _commandAccessorFactory(reference);
        }

        internal abstract class CommandAccessorBase : PropertyAccessorBase
        {
            private readonly WeakReference<object> _reference;
            private readonly ISet<string> _dependsOnProperties;

            public CommandAccessorBase(WeakReference<object> reference, ISet<string> dependsOnProperties)
            {
                Contract.Requires<ArgumentNullException>(reference != null);

                _reference = reference;
                _dependsOnProperties = dependsOnProperties;
            }

            public override Type PropertyType => typeof(ICommand);

            protected abstract void RaiseCanExecuteChanged();

            public override bool SetValue(object value, BindingPriority priority)
            {
                return false;
            }

            void OnNotifyPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (string.IsNullOrEmpty(e.PropertyName) || _dependsOnProperties.Contains(e.PropertyName))
                {
                    SendCurrentValue();
                }
            }

            protected override void SubscribeCore()
            {
                SendCurrentValue();
                SubscribeToChanges();
            }

            protected override void UnsubscribeCore()
            {
                if (_dependsOnProperties is { Count: > 0 } && _reference.TryGetTarget(out var o) && o is INotifyPropertyChanged inpc)
                {
                    WeakEventHandlerManager.Unsubscribe<PropertyChangedEventArgs, InpcPropertyAccessor>(
                        inpc,
                        nameof(INotifyPropertyChanged.PropertyChanged),
                        OnNotifyPropertyChanged);
                }
            }

            private void SendCurrentValue()
            {
                try
                {
                    var value = Value;
                    PublishValue(value);
                }
                catch { }
            }

            private void SubscribeToChanges()
            {
                if (_dependsOnProperties is { Count:>0 } && _reference.TryGetTarget(out var o) && o is INotifyPropertyChanged inpc)
                {
                    WeakEventHandlerManager.Subscribe<INotifyPropertyChanged, PropertyChangedEventArgs, InpcPropertyAccessor>(
                        inpc,
                        nameof(INotifyPropertyChanged.PropertyChanged),
                        OnNotifyPropertyChanged);
                }
            }
        }

        internal sealed class CommandWithParameterAccessor<T> : CommandAccessorBase
        {
            private Command _command;

            public CommandWithParameterAccessor(WeakReference<object> target, ISet<string> dependsOnProperties, Action<T> execute, Func<object, bool> canExecute)
                : base(target, dependsOnProperties)
            {
                _command = new Command(execute, canExecute);
            }

            public override object Value => _command;

            protected override void RaiseCanExecuteChanged()
            {
                _command.RaiseCanExecuteChanged();
            }

            private sealed class Command : ICommand
            {
                private readonly Action<T> _execute;
                private readonly Func<object, bool> _canExecute;

                public event EventHandler CanExecuteChanged;

                public Command(Action<T> execute, Func<object, bool> canExecute)
                {
                    _execute = execute;
                    _canExecute = canExecute;
                }

                public void RaiseCanExecuteChanged()
                {
                    Threading.Dispatcher.UIThread.Post(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty)
                       , Threading.DispatcherPriority.Input);
                }

                public bool CanExecute(object parameter)
                {
                    return _canExecute(parameter);
                }

                public void Execute(object parameter)
                {
                    _execute((T)parameter);
                }
            }
        }

        internal sealed class CommandWithoutParameterAccessor : CommandAccessorBase
        {
            private Command _command;

            public CommandWithoutParameterAccessor(WeakReference<object> target, ISet<string> dependsOnProperties, Action execute, Func<object, bool> canExecute)
                : base(target, dependsOnProperties)
            {
                _command = new Command(execute, canExecute);
            }

            public override object Value => _command;

            protected override void RaiseCanExecuteChanged()
            {
                _command.RaiseCanExecuteChanged();
            }

            private sealed class Command : ICommand
            {
                private readonly Action _execute;
                private readonly Func<object, bool> _canExecute;

                public event EventHandler CanExecuteChanged;

                public Command(Action execute, Func<object, bool> canExecute)
                {
                    _execute = execute;
                    _canExecute = canExecute;
                }

                public void RaiseCanExecuteChanged()
                {
                    Threading.Dispatcher.UIThread.Post(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty)
                       , Threading.DispatcherPriority.Input);
                }

                public bool CanExecute(object parameter)
                {
                    return _canExecute(parameter);
                }

                public void Execute(object parameter)
                {
                    _execute();
                }
            }
        }
    }
}
