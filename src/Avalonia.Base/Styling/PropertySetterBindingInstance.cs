using System;
using System.Reactive.Linq;
using Avalonia.Data;
using Avalonia.PropertyStore;

namespace Avalonia.Styling
{
    internal class PropertySetterBindingInstance : BindingEntry, ISetterInstance
    {
        private readonly AvaloniaObject _target;
        private readonly BindingMode _mode;
        private IDisposable? _twoWaySubscription;

        public PropertySetterBindingInstance(
            AvaloniaObject target,
            StyleInstance instance,
            AvaloniaProperty property,
            BindingMode mode,
            IObservable<object?> source)
            : base(instance, property, source)
        {
            _target = target;
            _mode = mode;

            if (mode == BindingMode.TwoWay &&
                source is not IObserver<object?>)
            {
                throw new NotSupportedException(
                    "Attempting to bind two-way with a binding source which doesn't support it.");
            }
        }

        public override void Unsubscribe()
        {
            _twoWaySubscription?.Dispose();
            base.Unsubscribe();
        }

        protected override void Start(bool produceValue)
        {
            if (_mode == BindingMode.TwoWay)
            {
                var observer = (IObserver<object?>)Source;
                _twoWaySubscription = _target.GetObservable(Property).Skip(1).Subscribe(observer);
            }

            base.Start(produceValue);
        }
    }
}
