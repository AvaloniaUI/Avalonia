// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Perspex.Data;
using Perspex.Markup.Data.Plugins;

namespace Perspex.Markup.Data
{
    internal class PropertyAccessorNode : ExpressionNode
    {
        private IPropertyAccessor _accessor;
        private IDisposable _subscription;

        public PropertyAccessorNode(string propertyName)
        {
            PropertyName = propertyName;
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
                    return _accessor.SetValue(value, priority);
                }

                return false;
            }
        }

        protected override void SubscribeAndUpdate(WeakReference reference)
        {
            var instance = reference.Target;

            if (instance != null && instance != PerspexProperty.UnsetValue)
            {
                var accessorPlugin = ExpressionObserver.PropertyAccessors.FirstOrDefault(x => x.Match(reference));

                if (accessorPlugin != null)
                {
                    _accessor = accessorPlugin.Start(reference, PropertyName, SetCurrentValue);
                    foreach (var validationPlugin in ExpressionObserver.ValidationCheckers.Where(x => x.Match(reference)))
                    {
                        if (validationPlugin != null)
                        {
                            _accessor = validationPlugin.Start(reference, PropertyName, _accessor, SendValidationStatus);
                        } 
                    }

                    if (_accessor != null)
                    {
                        SetCurrentValue(_accessor.Value);
                        return;
                    }
                }
            }

            CurrentValue = UnsetReference;
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
