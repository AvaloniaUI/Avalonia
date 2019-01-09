// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Data.Core
{
    public abstract class ExpressionNode
    {
        private static readonly object CacheInvalid = new object();
        protected static readonly WeakReference UnsetReference = 
            new WeakReference(AvaloniaProperty.UnsetValue);

        private WeakReference _target = UnsetReference;
        private Action<object> _subscriber;
        private bool _listening;

        protected WeakReference LastValue { get; private set; }

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

                if (!ReferenceEquals(oldTarget, newTarget))
                {
                    if (_listening)
                    {
                        StopListening();
                    }

                    _target = value;

                    if (_subscriber != null)
                    {
                        StartListening();
                    }
                }
            }
        }

        public void Subscribe(Action<object> subscriber)
        {
            if (_subscriber != null)
            {
                throw new AvaloniaInternalException("ExpressionNode can only be subscribed once.");
            }

            _subscriber = subscriber;
            Next?.Subscribe(NextValueChanged);
            StartListening();
        }

        public void Unsubscribe()
        {
            Next?.Unsubscribe();

            if (_listening)
            {
                StopListening();
            }

            LastValue = null;
            _subscriber = null;
        }

        protected virtual void StartListeningCore(WeakReference reference)
        {
            ValueChanged(reference.Target);
        }

        protected virtual void StopListeningCore()
        {
        }

        protected virtual void NextValueChanged(object value)
        {
            var bindingBroken = BindingNotification.ExtractError(value) as MarkupBindingChainException;
            bindingBroken?.AddNode(Description);
            _subscriber(value);
        }

        protected void ValueChanged(object value) => ValueChanged(value, true);

        private void ValueChanged(object value, bool notify)
        {
            var notification = value as BindingNotification;

            if (notification == null)
            {
                LastValue = new WeakReference(value);

                if (Next != null)
                {
                    Next.Target = LastValue;
                }
                else if (notify)
                {
                    _subscriber(value);
                }
            }
            else
            {
                LastValue = new WeakReference(notification.Value);

                if (Next != null)
                {
                    Next.Target = LastValue;
                }

                if (Next == null || notification.Error != null)
                {
                    _subscriber(value);
                }
            }
        }

        private void StartListening()
        {
            var target = _target.Target;

            if (target == null)
            {
                ValueChanged(TargetNullNotification());
                _listening = false;
            }
            else if (target != AvaloniaProperty.UnsetValue)
            {
                StartListeningCore(_target);
                _listening = true;
            }
            else
            {
                ValueChanged(AvaloniaProperty.UnsetValue, notify:false);
                _listening = false;
            }
        }

        private void StopListening()
        {
            StopListeningCore();
            _listening = false;
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
