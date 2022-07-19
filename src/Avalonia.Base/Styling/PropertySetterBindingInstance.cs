using System;
using System.Reactive.Linq;
using Avalonia.Data;
using Avalonia.PropertyStore;

namespace Avalonia.Styling
{
    internal class PropertySetterBindingInstance : BindingEntry, ISetterInstance
    {
        private readonly IDisposable? _twoWaySubscription;

        public PropertySetterBindingInstance(
            AvaloniaObject target,
            StyleInstance instance,
            AvaloniaProperty property,
            BindingMode mode,
            IObservable<object?> source)
            : base(instance, property, source)
        {
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

        public override void Unsubscribe()
        {
            _twoWaySubscription?.Dispose();
            base.Unsubscribe();
        }
    }
}
