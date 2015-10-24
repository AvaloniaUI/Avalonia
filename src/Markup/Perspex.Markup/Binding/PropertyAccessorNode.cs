// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Perspex.Markup.Binding
{
    internal class PropertyAccessorNode : ExpressionNode
    {
        private PropertyInfo _propertyInfo;
        private IDisposable _subscription;

        public PropertyAccessorNode(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; }

        public Type PropertyType => _propertyInfo?.PropertyType;

        public override bool SetValue(object value)
        {
            if (Next != null)
            {
                return Next.SetValue(value);
            }
            else
            {
                if (_propertyInfo != null && _propertyInfo.CanWrite)
                {
                    _propertyInfo.SetValue(Target, value);
                    return true;
                }

                return false;
            }
        }

        protected override void SubscribeAndUpdate(object target)
        {
            bool set = false;

            if (target != null)
            {
                _propertyInfo = target.GetType().GetRuntimeProperty(PropertyName);

                if (_propertyInfo != null)
                {
                    ReadValue(target);
                    set = true;

                    var inpc = target as INotifyPropertyChanged;

                    if (inpc != null)
                    {
                        inpc.PropertyChanged += PropertyChanged;
                    }
                }
            }
            else
            {
                _propertyInfo = null;
            }

            if (!set)
            {
                CurrentValue = PerspexProperty.UnsetValue;
            }
        }

        protected override void Unsubscribe(object target)
        {
            var inpc = target as INotifyPropertyChanged;

            if (inpc != null)
            {
                inpc.PropertyChanged -= PropertyChanged;
            }

            _propertyInfo = null;
        }

        private void ReadValue(object target)
        {
            var value = _propertyInfo.GetValue(target);
            var observable = value as IObservable<object>;
            var command = value as ICommand;
            var task = value as Task;
            bool set = false;

            // ReactiveCommand is an IObservable but we want to bind to it, not its value.
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

        private void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == PropertyName)
            {
                ReadValue(sender);
            }
        }
    }
}
