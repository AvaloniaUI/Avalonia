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

        public abstract string Description { get; }
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
                        _valueSubscription = StartListening();
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
            _valueSubscription = StartListening();

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

        protected virtual IObservable<object> StartListeningCore(WeakReference reference)
        {
            return Observable.Return(reference.Target);
        }

        protected virtual void NextValueChanged(object value)
        {
            var bindingBroken = BindingNotification.ExtractError(value) as MarkupBindingChainException;
            bindingBroken?.AddNode(Description);
            _observer.OnNext(value);
        }

        private IDisposable StartListening()
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
                source = StartListeningCore(_target);
            }

            return source.Subscribe(ValueChanged);
        }

        private void ValueChanged(object value)
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
                if (Next != null)
                {
                    Next.Target = new WeakReference(notification.Value);
                }
                
                if (Next == null || notification.Error != null)
                {
                    _observer.OnNext(value);
                }
            }
        }

        private BindingNotification TargetNullNotification()
        {
            return new BindingNotification(
                new MarkupBindingChainException("Null value"),
                BindingErrorType.Error,
                AvaloniaProperty.UnsetValue);
        }
    }
}
