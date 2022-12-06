using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
        private readonly Action<object, object> _execute;
        private readonly Func<object, object, bool> _canExecute;
        private readonly ISet<string> _dependsOnProperties;

        public CommandAccessorPlugin(Action<object, object> execute, Func<object, object, bool> canExecute, ISet<string> dependsOnProperties)
        {
            _execute = execute;
            _canExecute = canExecute;
            _dependsOnProperties = dependsOnProperties;
        }

        [RequiresUnreferencedCode(TrimmingMessages.PropertyAccessorsRequiresUnreferencedCodeMessage)]
        public bool Match(object obj, string propertyName)
        {
            throw new InvalidOperationException("The CommandAccessorPlugin does not support dynamic matching");
        }

        [RequiresUnreferencedCode(TrimmingMessages.PropertyAccessorsRequiresUnreferencedCodeMessage)]
        public IPropertyAccessor Start(WeakReference<object> reference, string propertyName)
        {
            return new CommandAccessor(reference, _execute, _canExecute, _dependsOnProperties);
        }

        private sealed class CommandAccessor : PropertyAccessorBase
        {
            private readonly WeakReference<object> _reference;
            private Command _command;
            private readonly ISet<string> _dependsOnProperties;

            public CommandAccessor(WeakReference<object> reference, Action<object, object> execute, Func<object, object, bool> canExecute, ISet<string> dependsOnProperties)
            {
                Contract.Requires<ArgumentNullException>(reference != null);

                _reference = reference;
                _dependsOnProperties = dependsOnProperties;
                _command = new Command(reference, execute, canExecute);

            }

            public override object Value => _reference.TryGetTarget(out var _) ? _command : null;

            private void RaiseCanExecuteChanged()
            {
                _command.RaiseCanExecuteChanged();
            }

            private sealed class Command : ICommand
            {
                private readonly WeakReference<object> _target;
                private readonly Action<object, object> _execute;
                private readonly Func<object, object, bool> _canExecute;

                public event EventHandler CanExecuteChanged;

                public Command(WeakReference<object> target, Action<object, object> execute, Func<object, object, bool> canExecute)
                {
                    _target = target;
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
                    if (_target.TryGetTarget(out var target))
                    {
                        if (_canExecute == null)
                        {
                            return true;
                        }
                        return _canExecute(target, parameter);
                    }
                    return false;
                }

                public void Execute(object parameter)
                {
                    if (_target.TryGetTarget(out var target))
                    {
                        _execute(target, parameter);
                    }
                }
            }

            public override Type PropertyType => typeof(ICommand);

            public override bool SetValue(object value, BindingPriority priority)
            {
                return false;
            }

            void OnNotifyPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (string.IsNullOrEmpty(e.PropertyName) || _dependsOnProperties.Contains(e.PropertyName))
                {
                    RaiseCanExecuteChanged();
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
                    WeakEventHandlerManager.Unsubscribe<PropertyChangedEventArgs, CommandAccessor>(
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
                    WeakEventHandlerManager.Subscribe<INotifyPropertyChanged, PropertyChangedEventArgs, CommandAccessor>(
                        inpc,
                        nameof(INotifyPropertyChanged.PropertyChanged),
                        OnNotifyPropertyChanged);
                }
            }
        }
    }
}
