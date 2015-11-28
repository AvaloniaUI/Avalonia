// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Subjects;

namespace Perspex.Markup.Data
{
    internal abstract class ExpressionNode : IObservable<object>
    {
        private object _target;

        private Subject<object> _subject;

        private object _value = PerspexProperty.UnsetValue;

        public ExpressionNode Next { get; set; }

        public object Target
        {
            get { return _target; }
            set
            {
                if (!object.Equals(value, _target))
                {
                    if (_target != null)
                    {
                        Unsubscribe(_target);
                    }

                    _target = value;

                    if (_target != null)
                    {
                        SubscribeAndUpdate(_target);
                    }
                    else
                    {
                        CurrentValue = PerspexProperty.UnsetValue;
                    }

                    if (Next != null)
                    {
                        Next.Target = CurrentValue;
                    }
                }
            }
        }

        public object CurrentValue
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

                _subject?.OnNext(value);
            }
        }

        public virtual bool SetValue(object value)
        {
            return Next?.SetValue(value) ?? false;
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

                observer.OnNext(CurrentValue);
                return _subject.Subscribe(observer);
            }
        }

        protected virtual void SubscribeAndUpdate(object target)
        {
            CurrentValue = target;
        }

        protected virtual void Unsubscribe(object target)
        {
        }
    }
}
