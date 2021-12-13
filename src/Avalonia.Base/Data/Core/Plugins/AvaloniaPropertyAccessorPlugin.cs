using System;
using System.Runtime.ExceptionServices;

#nullable enable

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Reads a property from a <see cref="AvaloniaObject"/>.
    /// </summary>
    public class AvaloniaPropertyAccessorPlugin : IPropertyAccessorPlugin
    {
        /// <inheritdoc/>
        public bool Match(object obj, string propertyName)
        {
            if (obj is AvaloniaObject o)
            {
                return LookupProperty(o, propertyName) != null;
            }

            return false;
        }

        /// <summary>
        /// Starts monitoring the value of a property on an object.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>
        /// An <see cref="IPropertyAccessor"/> interface through which future interactions with the 
        /// property will be made.
        /// </returns>
        public IPropertyAccessor? Start(WeakReference<object?> reference, string propertyName)
        {
            _ = reference ?? throw new ArgumentNullException(nameof(reference));
            _ = propertyName ?? throw new ArgumentNullException(nameof(propertyName));

            if (!reference.TryGetTarget(out var instance) || instance is null)
                return null;

            var o = (AvaloniaObject)instance;
            var p = LookupProperty(o, propertyName);

            if (p != null)
            {
                return new Accessor(new WeakReference<AvaloniaObject>(o), p);
            }
            else if (instance != AvaloniaProperty.UnsetValue)
            {
                var message = $"Could not find AvaloniaProperty '{propertyName}' on '{instance}'";
                var exception = new MissingMemberException(message);
                return new PropertyError(new BindingNotification(exception, BindingErrorType.Error));
            }
            else
            {
                return null;
            }
        }

        private static AvaloniaProperty? LookupProperty(AvaloniaObject o, string propertyName)
        {
            return AvaloniaPropertyRegistry.Instance.FindRegistered(o, propertyName);
        }

        private class Accessor : PropertyAccessorBase, IObserver<object?>
        {
            private readonly WeakReference<AvaloniaObject> _reference;
            private readonly AvaloniaProperty _property;
            private IDisposable? _subscription;

            public Accessor(WeakReference<AvaloniaObject> reference, AvaloniaProperty property)
            {
                _reference = reference ?? throw new ArgumentNullException(nameof(reference));
                _property = property ?? throw new ArgumentNullException(nameof(property));
            }

            public AvaloniaObject Instance
            {
                get
                {
                    AvaloniaObject result;
                    _reference.TryGetTarget(out result);
                    return result;
                }
            }

            public override Type? PropertyType => _property?.PropertyType;
            public override object? Value => Instance?.GetValue(_property);

            public override bool SetValue(object? value, BindingPriority priority)
            {
                if (!_property.IsReadOnly)
                {
                    Instance.SetValue(_property, value, priority);
                    return true;
                }

                return false;
            }

            protected override void SubscribeCore()
            {
                _subscription = Instance?.GetObservable(_property).Subscribe(this);
            }

            protected override void UnsubscribeCore()
            {
                _subscription?.Dispose();
                _subscription = null;
            }

            void IObserver<object?>.OnCompleted()
            {
            }

            void IObserver<object?>.OnError(Exception error)
            {
                ExceptionDispatchInfo.Capture(error).Throw();
            }

            void IObserver<object?>.OnNext(object? value)
            {
                PublishValue(value);
            }
        }
    }
}
