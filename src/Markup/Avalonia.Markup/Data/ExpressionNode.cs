// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Data;

namespace Avalonia.Markup.Data
{
    internal abstract class ExpressionNode : ISubject<object>
    {
        protected static readonly WeakReference UnsetReference = 
            new WeakReference(AvaloniaProperty.UnsetValue);

        private WeakReference _target = UnsetReference;
        private IDisposable _valueSubscription;
        private IObserver<object> _observer;

        public ExpressionNode Next { get; set; }

        public WeakReference Target
        {
            get { return _target; }
            set
            {
                Contract.Requires<ArgumentNullException>(value != null);

                var oldTarget = _target?.Target;
                var newTarget = value.Target;
                var running = _valueSubscription != null;

                if (!ReferenceEquals(oldTarget, newTarget))
                {
                    _valueSubscription?.Dispose();
                    _valueSubscription = null;
                    _target = value;

                    if (running)
                    {
                        _valueSubscription = StartListeningCore();
                    }
                }
            }
        }

        public IDisposable Subscribe(IObserver<object> observer)
        {
            if (_observer != null)
            {
                throw new AvaloniaInternalException("ExpressionNode can only be subscribed once.");
            }

            _observer = observer;
            var nextSubscription = Next?.Subscribe(this);
            _valueSubscription = StartListeningCore();

            return Disposable.Create(() =>
            {
                _valueSubscription?.Dispose();
                _valueSubscription = null;
                nextSubscription?.Dispose();
                _observer = null;
            });
        }

        void IObserver<object>.OnCompleted()
        {
            throw new AvaloniaInternalException("ExpressionNode.OnCompleted should not be called.");
        }

        void IObserver<object>.OnError(Exception error)
        {
            throw new AvaloniaInternalException("ExpressionNode.OnError should not be called.");
        }

        void IObserver<object>.OnNext(object value)
        {
            NextValueChanged(value);
        }

        protected virtual IObservable<object> StartListening(WeakReference reference)
        {
            return Observable.Return(reference.Target);
        }

        protected virtual void NextValueChanged(object value)
        {
            _observer.OnNext(value);
        }

        private IDisposable StartListeningCore()
        {
            var target = _target.Target;
            IObservable<object> source;

            if (target == null)
            {
                source = Observable.Return(TargetNullNotification());
            }
            else if (target == AvaloniaProperty.UnsetValue)
            {
                source = Observable.Empty<object>();
            }
            else
            {
                source = StartListening(_target);
            }

            return source.Subscribe(TargetValueChanged);
        }

        private void TargetValueChanged(object value)
        {
            var notification = value as BindingNotification;

            if (notification == null)
            {
                if (Next != null)
                {
                    Next.Target = new WeakReference(value);
                }
                else
                {
                    _observer.OnNext(value);
                }
            }
            else
            {
                if (notification.Error != null)
                {
                    _observer.OnNext(notification);
                }
                else if (notification.HasValue)
                {
                    if (Next != null)
                    {
                        Next.Target = new WeakReference(notification.Value);
                    }
                    else
                    {
                        _observer.OnNext(value);
                    }
                }
            }
        }

        private BindingNotification TargetNullNotification()
        {
            // TODO: Work out a way to give a more useful error message here.
            return new BindingNotification(new NullReferenceException(), BindingErrorType.Error);
        }
    }
}
