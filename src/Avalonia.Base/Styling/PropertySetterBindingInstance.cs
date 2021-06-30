using System;
using System.Reactive.Linq;
using Avalonia.Data;
using Avalonia.PropertyStore;

#nullable enable

namespace Avalonia.Styling
{
    internal class PropertySetterBindingInstance : BindingValueEntryBase, ISetterInstance
    {
        private readonly StyleInstance _instance;
        private readonly IDisposable? _twoWaySubscription;

        public PropertySetterBindingInstance(
            AvaloniaObject target,
            StyleInstance instance,
            AvaloniaProperty property,
            BindingMode mode,
            IObservable<object?> source)
            : base(property, source)
        {
            _instance = instance;

            if (mode == BindingMode.TwoWay)
            {
                // TODO: HUGE HACK FIXME
                if (source is IObserver<object?> observer)
                {
                    _twoWaySubscription = target.GetObservable(property).Skip(1).Subscribe(observer);
                }
                else
                {
                    throw new NotSupportedException(
                        "Attempting to bind two-way with a binding source which doesn't support it.");
                }
            }
        }

        public override void Dispose()
        {
            _twoWaySubscription?.Dispose();
            base.Dispose();
        }

        protected override AvaloniaObject GetOwner() => _instance.ValueStore!.Owner;

        protected override void ValueChanged(object? oldValue)
        {
            _instance.ValueStore!.ValueChanged(_instance, this, oldValue);
        }

        protected override void Completed(object? oldValue) => ValueChanged(oldValue);
    }
}
