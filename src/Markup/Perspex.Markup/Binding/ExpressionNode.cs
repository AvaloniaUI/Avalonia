// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Subjects;

namespace Perspex.Markup.Binding
{
    public abstract class ExpressionNode : IObservable<ExpressionValue>
    {
        private object _target;

        private Subject<ExpressionValue> _subject;

        private ExpressionValue _value = ExpressionValue.None;

        public ExpressionNode Next
        {
            get;
            set;
        }

        public object Target
        {
            get { return _target; }
            set
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
                    CurrentValue = ExpressionValue.None;
                }

                if (Next != null)
                {
                    Next.Target = CurrentValue.Value;
                }
            }
        }

        public ExpressionValue CurrentValue
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
                    Next.Target = value.Value;
                }

                if (_subject != null)
                {
                    _subject.OnNext(value);
                }
            }
        }

        public IDisposable Subscribe(IObserver<ExpressionValue> observer)
        {
            if (Next != null)
            {
                return Next.Subscribe(observer);
            }
            else
            {
                if (_subject == null)
                {
                    _subject = new Subject<ExpressionValue>();
                }

                observer.OnNext(CurrentValue);
                return _subject.Subscribe(observer);
            }
        }

        protected abstract void SubscribeAndUpdate(object target);

        protected abstract void Unsubscribe(object target);

    }
}
