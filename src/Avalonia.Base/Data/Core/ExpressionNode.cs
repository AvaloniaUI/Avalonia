using System;

namespace Avalonia.Data.Core
{
    public abstract class ExpressionNode
    {
        protected static readonly WeakReference<object?> UnsetReference = 
            new WeakReference<object?>(AvaloniaProperty.UnsetValue);

        protected static readonly WeakReference<object?> NullReference =
            new WeakReference<object?>(null);

        private WeakReference<object?> _target = UnsetReference;
        private Action<object?>? _subscriber;
        private bool _listening;

        protected WeakReference<object?>? LastValue { get; private set; }

        public abstract string? Description { get; }
        public ExpressionNode? Next { get; set; }

        public WeakReference<object?> Target
        {
            get { return _target; }
            set
            {
                _ = value ?? throw new ArgumentNullException(nameof(value));

                _target.TryGetTarget(out var oldTarget);
                value.TryGetTarget(out var newTarget);

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

        public void Subscribe(Action<object?> subscriber)
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

        protected virtual void StartListeningCore(WeakReference<object?> reference)
        {
            reference.TryGetTarget(out var target);

            ValueChanged(target);
        }

        protected virtual void StopListeningCore()
        {
        }

        protected virtual void NextValueChanged(object? value)
        {
            if (_subscriber is null)
                return;

            var bindingBroken = BindingNotification.ExtractError(value) as MarkupBindingChainException;
            bindingBroken?.AddNode(Description ?? "{empty}");
            _subscriber(value);
        }

        protected void ValueChanged(object? value) => ValueChanged(value, true);

        private void ValueChanged(object? value, bool notify)
        {
            if (_subscriber is { } subscriber)
            {
                var notification = value as BindingNotification;
                var next = Next;

                if (notification == null)
                {
                    LastValue = value != null ? new WeakReference<object?>(value) : NullReference;
                    if (next != null)
                    {
                        next.Target = LastValue;
                    }
                    else if (notify)
                    {
                        subscriber(value);
                    }
                }
                else
                {
                    LastValue = notification.Value != null ? new WeakReference<object?>(notification.Value) : NullReference;

                    if (next != null)
                    {
                        next.Target = LastValue;
                    }

                    if (next == null || notification.Error != null)
                    {
                        subscriber(value);
                    }
                }
            }
        }

        private void StartListening()
        {
            _target.TryGetTarget(out var target);

            if (target == null)
            {
                ValueChanged(TargetNullNotification());
                _listening = false;
            }
            else if (target != AvaloniaProperty.UnsetValue)
            {
                _listening = true;
                StartListeningCore(_target!);
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

        private static BindingNotification TargetNullNotification()
        {
            return new BindingNotification(
                new MarkupBindingChainException("Null value"),
                BindingErrorType.Error,
                AvaloniaProperty.UnsetValue);
        }
    }
}
