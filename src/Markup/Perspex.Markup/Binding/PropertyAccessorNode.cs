// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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

        public override bool SetValue(object value)
        {
            if (Next != null)
            {
                return Next.SetValue(value);
            }
            else
            {
                if (_propertyInfo != null)
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
                _propertyInfo = target.GetType().GetTypeInfo().GetDeclaredProperty(PropertyName);

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
                CurrentValue = ExpressionValue.None;
            }
        }

        protected override void Unsubscribe(object target)
        {
            var inpc = target as INotifyPropertyChanged;

            if (inpc != null)
            {
                inpc.PropertyChanged -= PropertyChanged;
            }
        }

        private void ReadValue(object target)
        {
            var value = _propertyInfo.GetValue(target);
            var observable = value as IObservable<object>;
            var task = value as Task;
            bool set = false;

            if (observable != null)
            {
                CurrentValue = ExpressionValue.None;
                set = true;
                _subscription = observable
                    .ObserveOn(SynchronizationContext.Current)
                    .Subscribe(x => CurrentValue = new ExpressionValue(x));
            }
            else if (task != null)
            {
                var resultProperty = task.GetType().GetTypeInfo().GetDeclaredProperty("Result");

                if (resultProperty != null)
                {
                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        CurrentValue = new ExpressionValue(resultProperty.GetValue(task));
                        set = true;
                    }
                    else
                    {
                        task.ContinueWith(
                                x => CurrentValue = new ExpressionValue(resultProperty.GetValue(task)),
                                TaskScheduler.FromCurrentSynchronizationContext())
                            .ConfigureAwait(false);
                    }
                }
            }
            else
            {
                CurrentValue = new ExpressionValue(value);
                set = true;
            }

            if (!set)
            {
                CurrentValue = ExpressionValue.None;
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
