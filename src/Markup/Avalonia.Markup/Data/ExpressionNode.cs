// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Subjects;
using Avalonia.Data;

namespace Avalonia.Markup.Data
{
    internal abstract class ExpressionNode : IObservable<object>
    {
        protected static readonly WeakReference UnsetReference = 
            new WeakReference(AvaloniaProperty.UnsetValue);

        private WeakReference _target;

        private Subject<object> _subject;

        private WeakReference _value = UnsetReference;

        public ExpressionNode Next { get; set; }

        public WeakReference Target
        {
            get
            {
                return _target;
            }
            set
            {
                var newInstance = value?.Target;
                var oldInstance = _target?.Target;

                if (!object.Equals(oldInstance, newInstance))
                {
                    if (oldInstance != null)
                    {
                        Unsubscribe(oldInstance);
                    }

                    _target = value;

                    if (newInstance != null)
                    {
                        SubscribeAndUpdate(_target);
                    }
                    else
                    {
                        CurrentValue = UnsetReference;
                    }

                    if (Next != null)
                    {
                        Next.Target = _value;
                    }
                }
            }
        }

        public WeakReference CurrentValue
        {
            get
            {
                return _value;
            }

            set
            {
                _value = value;

                if (Next != null)
                {
                    Next.Target = value;
                }

                _subject?.OnNext(value.Target);
            }
        }

        public virtual bool SetValue(object value, BindingPriority priority)
        {
            return Next?.SetValue(value, priority) ?? false;
        }

        public virtual IDisposable Subscribe(IObserver<object> observer)
        {
            if (Next != null)
            {
                return Next.Subscribe(observer);
            }
            else
            {
                if (_subject == null)
                {
                    _subject = new Subject<object>();
                }

                observer.OnNext(CurrentValue.Target);
                return _subject.Subscribe(observer);
            }
        }

        protected virtual void SubscribeAndUpdate(WeakReference reference)
        {
            CurrentValue = reference;
        }

        protected virtual void Unsubscribe(object target)
        {
        }
    }
}
