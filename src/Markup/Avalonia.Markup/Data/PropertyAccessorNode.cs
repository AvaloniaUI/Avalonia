// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Data;
using Avalonia.Logging;
using Avalonia.Markup.Data.Plugins;

namespace Avalonia.Markup.Data
{
    internal class PropertyAccessorNode : ExpressionNode, IObserver<object>
    {
        private IPropertyAccessor _accessor;
        private IDisposable _subscription;
        private bool _enableValidation;

        public PropertyAccessorNode(string propertyName, bool enableValidation)
        {
            PropertyName = propertyName;
            _enableValidation = enableValidation;
        }

        public string PropertyName { get; }

        public Type PropertyType => _accessor?.PropertyType;

        public override bool SetValue(object value, BindingPriority priority)
        {
            if (Next != null)
            {
                return Next.SetValue(value, priority);
            }
            else
            {
                if (_accessor != null)
                {
                    try { return _accessor.SetValue(value, priority); } catch { }
                }

                return false;
            }
        }

        void IObserver<object>.OnCompleted()
        {
            // Should not be called by IPropertyAccessor.
        }

        void IObserver<object>.OnError(Exception error)
        {
            // Should not be called by IPropertyAccessor.
        }

        void IObserver<object>.OnNext(object value)
        {
            SetCurrentValue(value);
        }

        protected override void SubscribeAndUpdate(WeakReference reference)
        {
            var instance = reference.Target;

            if (instance != null && instance != AvaloniaProperty.UnsetValue)
            {
                var plugin = ExpressionObserver.PropertyAccessors.FirstOrDefault(x => x.Match(reference));
                var accessor = plugin?.Start(reference, PropertyName);

                if (_enableValidation)
                {
                    foreach (var validator in ExpressionObserver.DataValidators)
                    {
                        if (validator.Match(reference))
                        {
                            accessor = validator.Start(reference, PropertyName, accessor);
                        }
                    }
                }

                _accessor = accessor;
                _accessor.Subscribe(this);
            }
            else
            {
                CurrentValue = UnsetReference;
            }
        }

        protected override void Unsubscribe(object target)
        {
            _accessor?.Dispose();
            _accessor = null;
        }

        private void SetCurrentValue(object value)
        {
            var observable = value as IObservable<object>;
            var command = value as ICommand;
            var task = value as Task;
            bool set = false;

            // HACK: ReactiveCommand is an IObservable but we want to bind to it, not its value.
            // We may need to make this a more general solution.
            if (observable != null && command == null)
            {
                CurrentValue = UnsetReference;
                set = true;
                _subscription = observable
                    .ObserveOn(SynchronizationContext.Current)
                    .Subscribe(x => CurrentValue = new WeakReference(x));
            }
            else if (task != null)
            {
                var resultProperty = task.GetType().GetTypeInfo().GetDeclaredProperty("Result");

                if (resultProperty != null)
                {
                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        CurrentValue = new WeakReference(resultProperty.GetValue(task));
                        set = true;
                    }
                    else
                    {
                        task.ContinueWith(
                                x => CurrentValue = new WeakReference(resultProperty.GetValue(task)),
                                TaskScheduler.FromCurrentSynchronizationContext())
                            .ConfigureAwait(false);
                    }
                }
            }
            else
            {
                CurrentValue = new WeakReference(value);
                set = true;
            }

            if (!set)
            {
                CurrentValue = UnsetReference;
            }
        }
    }
}
