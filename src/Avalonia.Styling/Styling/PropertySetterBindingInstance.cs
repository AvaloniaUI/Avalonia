using System;
using System.Reactive.Subjects;
using Avalonia.Data;
using Avalonia.Reactive;

#nullable enable

namespace Avalonia.Styling
{
    internal class PropertySetterBindingInstance<T> : SingleSubscriberObservableBase<BindingValue<T>>,
        ISubject<BindingValue<T>>,
        ISetterInstance
    {
        private readonly IStyleable _target;
        private readonly StyledPropertyBase<T>? _styledProperty;
        private readonly DirectPropertyBase<T>? _directProperty;
        private readonly InstancedBinding _binding;
        private readonly Inner _inner;
        private BindingValue<T> _value;
        private IDisposable? _subscription;
        private IDisposable? _subscriptionTwoWay;
        private bool _isActive;

        public PropertySetterBindingInstance(
            IStyleable target,
            StyledPropertyBase<T> property,
            IBinding binding)
        {
            _target = target;
            _styledProperty = property;
            _binding = binding.Initiate(_target, property);
            _inner = new Inner(this);
        }

        public PropertySetterBindingInstance(
            IStyleable target,
            DirectPropertyBase<T> property,
            IBinding binding)
        {
            _target = target;
            _directProperty = property;
            _binding = binding.Initiate(_target, property);
            _inner = new Inner(this);
        }

        public void Start(bool hasActivator)
        {
            _isActive = !hasActivator;

            if (_styledProperty is object)
            {
                if (_binding.Mode != BindingMode.OneWayToSource)
                {
                    var priority = hasActivator ? BindingPriority.StyleTrigger : BindingPriority.Style;
                    _subscription = _target.Bind(_styledProperty, this, priority);
                }

                if (_binding.Mode == BindingMode.TwoWay)
                {
                    _subscriptionTwoWay = _target.GetBindingObservable(_styledProperty).Subscribe(this);
                }
            }
            else
            {
                if (_binding.Mode != BindingMode.OneWayToSource)
                {
                    _subscription = _target.Bind(_directProperty!, this);
                }

                if (_binding.Mode == BindingMode.TwoWay)
                {
                    _subscriptionTwoWay = _target.GetBindingObservable(_directProperty!).Subscribe(this);
                }
            }
        }

        public void Activate()
        {
            if (!_isActive)
            {
                _isActive = true;
                PublishNext();
            }
        }

        public void Deactivate()
        {
            if (_isActive)
            {
                _isActive = false;
                PublishNext();
            }
        }

        public override void Dispose()
        {
            if (_subscription is object)
            {
                var sub = _subscription;
                _subscription = null;
                sub.Dispose();
            }

            if (_subscriptionTwoWay is object)
            {
                var sub = _subscriptionTwoWay;
                _subscriptionTwoWay = null;
                sub.Dispose();
            }

            base.Dispose();
        }

        void IObserver<BindingValue<T>>.OnCompleted()
        {
            // This is the observable coming from the target control. It should not complete.
        }

        void IObserver<BindingValue<T>>.OnError(Exception error)
        {
            // This is the observable coming from the target control. It should not error.
        }

        void IObserver<BindingValue<T>>.OnNext(BindingValue<T> value)
        {
            if (value.HasValue && _isActive)
            {
                _binding.Subject.OnNext(value.Value);
            }
        }

        protected override void Subscribed()
        {
            _subscription = _binding.Observable.Subscribe(_inner);
        }

        protected override void Unsubscribed()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private void PublishNext()
        {
            PublishNext(_isActive ? _value : default);
        }

        private void ConvertAndPublishNext(object? value)
        {
            _value = value is T v ? v : BindingValue<object>.FromUntyped(value).Convert<T>();

            if (_isActive)
            {
                PublishNext();
            }
        }

        private class Inner : IObserver<object?>
        {
            private readonly PropertySetterBindingInstance<T> _owner;
            public Inner(PropertySetterBindingInstance<T> owner) => _owner = owner;
            public void OnCompleted() => _owner.PublishCompleted();
            public void OnError(Exception error) => _owner.PublishError(error);
            public void OnNext(object? value) => _owner.ConvertAndPublishNext(value);
        }
    }
}
