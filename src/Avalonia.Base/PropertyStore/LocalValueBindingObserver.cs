using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    internal class LocalValueBindingObserver<T> : LocalValueBindingObserverBase<T>,
        IObserver<object?>
    {
        public LocalValueBindingObserver(ValueStore owner, StyledProperty<T> property)
            : base(owner, property)
        {
        }

        public void Start(IObservable<object?> source) => _subscription = source.Subscribe(this);

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.ImplicitTypeConversionSupressWarningMessage)]
        public void OnNext(object? value)
        {
            if (value == BindingOperations.DoNothing)
                return;
            base.OnNext(BindingValue<T>.FromUntyped(value, Property.PropertyType));
        }
    }
}
