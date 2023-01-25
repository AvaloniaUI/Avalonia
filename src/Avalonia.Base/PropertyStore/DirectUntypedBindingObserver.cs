using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;
using Avalonia.Threading;

namespace Avalonia.PropertyStore
{
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.ImplicitTypeConvertionSupressWarningMessage)]
    internal class DirectUntypedBindingObserver<T> : IObserver<object?>,
        IDisposable
    {
        private readonly ValueStore _owner;
        private IDisposable? _subscription;

        public DirectUntypedBindingObserver(ValueStore owner, DirectPropertyBase<T> property)
        {
            _owner = owner;
            Property = property;
        }

        public DirectPropertyBase<T> Property { get;}

        public void Start(IObservable<object?> source)
        {
            _subscription = source.Subscribe(this);
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
            _owner.OnLocalValueBindingCompleted(Property, this);
        }

        public void OnCompleted() => _owner.OnLocalValueBindingCompleted(Property, this);
        public void OnError(Exception error) => OnCompleted();

        public void OnNext(object? value)
        {
            var typed = BindingValue<T>.FromUntyped(value);

            if (Dispatcher.UIThread.CheckAccess())
            {
                _owner.Owner.SetDirectValueUnchecked<T>(Property, typed);
            }
            else
            {
                // To avoid allocating closure in the outer scope we need to capture variables
                // locally. This allows us to skip most of the allocations when on UI thread.
                var instance = _owner.Owner;
                var property = Property;
                var newValue = value;
                Dispatcher.UIThread.Post(() => instance.SetDirectValueUnchecked(property, typed));
            }
        }
    }
}
