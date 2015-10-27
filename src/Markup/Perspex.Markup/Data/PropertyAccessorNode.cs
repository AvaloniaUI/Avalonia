// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
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

        public override bool SetValue(object value)
        {
            if (Next != null)
            {
                return Next.SetValue(value);
            }
            else
            {
                if (_accessor != null)
                {
                    return _accessor.SetValue(value);
                }

                return false;
            }
        }

        protected override void SubscribeAndUpdate(object target)
        {
            if (target != null)
            {
                var plugin = ExpressionObserver.PropertyAccessors.FirstOrDefault(x => x.Match(target));

                if (plugin != null)
                {
                    _accessor = plugin.Start(target, PropertyName, SetCurrentValue);

                    if (_accessor != null)
                    {
                        SetCurrentValue(_accessor.Value);
                        return;
                    }
                }
            }

            CurrentValue = PerspexProperty.UnsetValue;
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
                CurrentValue = PerspexProperty.UnsetValue;
                set = true;
                _subscription = observable
                    .ObserveOn(SynchronizationContext.Current)
                    .Subscribe(x => CurrentValue = x);
            }
            else if (task != null)
            {
                var resultProperty = task.GetType().GetTypeInfo().GetDeclaredProperty("Result");

                if (resultProperty != null)
                {
                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        CurrentValue = resultProperty.GetValue(task);
                        set = true;
                    }
                    else
                    {
                        task.ContinueWith(
                                x => CurrentValue = resultProperty.GetValue(task),
                                TaskScheduler.FromCurrentSynchronizationContext())
                            .ConfigureAwait(false);
                    }
                }
            }
            else
            {
                CurrentValue = value;
                set = true;
            }

            if (!set)
            {
                CurrentValue = PerspexProperty.UnsetValue;
            }
        }
    }
}
