using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Data;
using Avalonia.Threading;
using Avalonia.Utilities;

#nullable enable

namespace Avalonia.PropertyStore
{
    internal class DirectBindingUntyped<T> : DirectBindingBase<T>, IObserver<object?>
    {
        private readonly IDisposable _subscription;

        public DirectBindingUntyped(
            AvaloniaObject owner,
            DirectPropertyBase<T> property,
            IObservable<object?> source)
                : base(owner, property)
        {
            _subscription = source.Subscribe(this);
        }

        public override void Dispose()
        {
            _subscription.Dispose();
            base.Dispose();
        }

        public void OnNext(object? value)
        {
            var bindingValue = BindingValue<T>.FromUntyped(value);

            if (Dispatcher.UIThread.CheckAccess())
            {
                Owner.SetDirectValueUnchecked(Property, bindingValue);
            }
            else
            {
                // To avoid allocating closure in the outer scope we need to capture variables
                // locally. This allows us to skip most of the allocations when on UI thread.
                var instance = Owner;
                var property = Property;
                var newValue = value;

                Dispatcher.UIThread.Post(() => instance.SetDirectValueUnchecked(property, bindingValue));
            }
        }
    }
}
