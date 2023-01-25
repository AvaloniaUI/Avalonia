using System;

namespace Avalonia.Styling.Activators
{
    /// <summary>
    /// An <see cref="IStyleActivator"/> which listens to a property value on a control.
    /// </summary>
    internal class PropertyEqualsActivator : StyleActivatorBase, IObserver<object?>
    {
        private readonly StyledElement _control;
        private readonly AvaloniaProperty _property;
        private readonly object? _value;
        private IDisposable? _subscription;

        public PropertyEqualsActivator(
            StyledElement control,
            AvaloniaProperty property,
            object? value)
        {
            _control = control ?? throw new ArgumentNullException(nameof(control));
            _property = property ?? throw new ArgumentNullException(nameof(property));
            _value = value;
        }

        protected override bool EvaluateIsActive()
        {
            var value = _control.GetValue(_property);
            return PropertyEqualsSelector.Compare(_property.PropertyType, value, _value);
        }

        protected override void Initialize()
        {
            _subscription = _control.GetObservable(_property).Subscribe(this);
        }

        protected override void Deinitialize() => _subscription?.Dispose();

        void IObserver<object?>.OnCompleted() { }
        void IObserver<object?>.OnError(Exception error) { }
        void IObserver<object?>.OnNext(object? value) => ReevaluateIsActive();
    }
}
