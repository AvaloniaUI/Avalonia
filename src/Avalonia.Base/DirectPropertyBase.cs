using System;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.PropertyStore;

namespace Avalonia
{
    /// <summary>
    /// Base class for direct properties.
    /// </summary>
    /// <typeparam name="TValue">The type of the property's value.</typeparam>
    /// <remarks>
    /// Whereas <see cref="DirectProperty{TOwner, TValue}"/> is typed on the owner type, this base
    /// class provides a non-owner-typed interface to a direct property.
    /// </remarks>
    public abstract class DirectPropertyBase<TValue> : AvaloniaProperty<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectPropertyBase{TValue}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="metadata">The property metadata.</param>
        private protected DirectPropertyBase(
            string name,
            Type ownerType,
            AvaloniaPropertyMetadata metadata)
            : base(name, ownerType, ownerType, metadata)
        {
            Owner = ownerType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectPropertyBase{TValue}"/> class.
        /// </summary>
        /// <param name="source">The property to copy.</param>
        /// <param name="ownerType">The new owner type.</param>
        /// <param name="metadata">Optional overridden metadata.</param>
        private protected DirectPropertyBase(
            DirectPropertyBase<TValue> source,
            Type ownerType,
            AvaloniaPropertyMetadata metadata)
            : base(source, ownerType, metadata)
        {
            Owner = ownerType;
        }

        /// <summary>
        /// Gets the type that registered the property.
        /// </summary>
        public Type Owner { get; }

        /// <summary>
        /// Gets the value of the property on the instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>The property value.</returns>
        internal abstract TValue InvokeGetter(AvaloniaObject instance);

        /// <summary>
        /// Sets the value of the property on the instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="value">The value.</param>
        internal abstract void InvokeSetter(AvaloniaObject instance, BindingValue<TValue> value);

        /// <summary>
        /// Gets the unset value for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The unset value.</returns>
        public TValue GetUnsetValue(Type type)
        {
            type = type ?? throw new ArgumentNullException(nameof(type));
            return GetMetadata(type).UnsetValue;
        }

        /// <summary>
        /// Gets the property metadata for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// The property metadata.
        /// </returns>
        public new DirectPropertyMetadata<TValue> GetMetadata(Type type)
        {
            return (DirectPropertyMetadata<TValue>)base.GetMetadata(type);
        }

        /// <summary>
        /// Overrides the metadata for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="metadata">The metadata.</param>
        public void OverrideMetadata<T>(DirectPropertyMetadata<TValue> metadata) where T : AvaloniaObject
        {
            base.OverrideMetadata(typeof(T), metadata);
        }

        /// <summary>
        /// Overrides the metadata for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="metadata">The metadata.</param>
        public void OverrideMetadata(Type type, DirectPropertyMetadata<TValue> metadata)
        {
            base.OverrideMetadata(type, metadata);
        }

        internal override EffectiveValue CreateEffectiveValue(AvaloniaObject o)
        {
            throw new InvalidOperationException("Cannot create EffectiveValue for direct property.");
        }

        /// <inheritdoc/>
        internal override void RouteClearValue(AvaloniaObject o)
        {
            o.ClearValue<TValue>(this);
        }

        internal override void RouteCoerceDefaultValue(AvaloniaObject o)
        {
            // Do nothing.
        }

        /// <inheritdoc/>
        internal override object? RouteGetValue(AvaloniaObject o)
        {
            return o.GetValue<TValue>(this);
        }

        internal override object? RouteGetBaseValue(AvaloniaObject o)
        {
            return o.GetValue<TValue>(this);
        }

        /// <inheritdoc/>
        internal override IDisposable? RouteSetValue(
            AvaloniaObject o,
            object? value,
            BindingPriority priority)
        {
            var v = TryConvert(value);

            if (v.HasValue)
            {
                o.SetValue<TValue>(this, (TValue)v.Value!);
            }
            else if (v.Type == BindingValueType.UnsetValue)
            {
                o.ClearValue(this);
            }
            else if (v.HasError)
            {
                throw v.Error!;
            }

            return null;
        }

        internal override void RouteSetDirectValueUnchecked(AvaloniaObject o, object? value)
        {
            var bindingValue = BindingValue<TValue>.FromUntypedStrict(value);
            o.SetDirectValueUnchecked<TValue>(this, bindingValue);
        }

        internal override void RouteSetCurrentValue(AvaloniaObject o, object? value)
        {
            RouteSetValue(o, value, BindingPriority.LocalValue);
        }

        /// <summary>
        /// Routes an untyped Bind call to a typed call.
        /// </summary>
        /// <param name="o">The object instance.</param>
        /// <param name="source">The binding source.</param>
        /// <param name="priority">The priority.</param>
        internal override IDisposable RouteBind(
            AvaloniaObject o,
            IObservable<object?> source,
            BindingPriority priority)
        {
            return o.Bind(this, source);
        }
    }
}
