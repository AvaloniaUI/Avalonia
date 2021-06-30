using System;
using Avalonia.Data;
using Avalonia.Threading;

#nullable enable

namespace Avalonia.PropertyStore
{
    internal class DirectBinding<T> : DirectBindingBase<T>, IObserver<BindingValue<T>>, IObserver<T>
    {
        private readonly IDisposable _subscription;

        public DirectBinding(
            AvaloniaObject owner,
            DirectPropertyBase<T> property,
            IObservable<BindingValue<T>> source)
                : base(owner, property)
        {
            _subscription = source.Subscribe(this);
        }

        public DirectBinding(
            AvaloniaObject owner,
            DirectPropertyBase<T> property,
            IObservable<T> source)
                : base(owner, property)
        {
            _subscription = source.Subscribe(this);
        }

        public override void Dispose()
        {
            _subscription.Dispose();
            base.Dispose();
        }

        public void OnNext(T value)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                Owner.SetDirectValueUnchecked(Property, value);
            }
            else
            {
                // To avoid allocating closure in the outer scope we need to capture variables
                // locally. This allows us to skip most of the allocations when on UI thread.
                var instance = Owner;
                var property = Property;
                var newValue = value;

                Dispatcher.UIThread.Post(() => instance.SetDirectValueUnchecked(property, newValue));
            }
        }

        public void OnNext(BindingValue<T> value)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                Owner.SetDirectValueUnchecked(Property, value);
            }
            else
            {
                // To avoid allocating closure in the outer scope we need to capture variables
                // locally. This allows us to skip most of the allocations when on UI thread.
                var instance = Owner;
                var property = Property;
                var newValue = value;

                Dispatcher.UIThread.Post(() => instance.SetDirectValueUnchecked(property, newValue));
            }
        }
    }
}
