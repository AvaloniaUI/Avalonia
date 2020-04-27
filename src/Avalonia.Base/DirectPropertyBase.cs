using System;
using Avalonia.Data;
using Avalonia.Reactive;
using Avalonia.Utilities;

#nullable enable

namespace Avalonia
{
    /// <summary>
    /// Base class for direct properties.
    /// </summary>
    /// <typeparam name="TValue">The type of the property's value.</typeparam>
    /// <remarks>
    /// Whereas <see cref="DirectProperty{TOwner, TValue}"/> is typed on the owner type, this base
    /// class provides a non-owner-typed interface to a direct poperty.
    /// </remarks>
    public abstract class DirectPropertyBase<TValue> : AvaloniaProperty<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectPropertyBase{TValue}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="metadata">The property metadata.</param>
        /// <param name="enableDataValidation">
        /// Whether the property is interested in data validation.
        /// </param>
        protected DirectPropertyBase(
            string name,
            Type ownerType,
            PropertyMetadata metadata,
            bool enableDataValidation)
            : base(name, ownerType, metadata)
        {
            IsDataValidationEnabled = enableDataValidation;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaProperty"/> class.
        /// </summary>
        /// <param name="source">The property to copy.</param>
        /// <param name="ownerType">The new owner type.</param>
        /// <param name="metadata">Optional overridden metadata.</param>
        /// <param name="enableDataValidation">
        /// Whether the property is interested in data validation.
        /// </param>
        protected DirectPropertyBase(
            AvaloniaProperty source,
            Type ownerType,
            PropertyMetadata metadata,
            bool enableDataValidation)
            : base(source, ownerType, metadata)
        {
            IsDataValidationEnabled = enableDataValidation;
        }

        /// <summary>
        /// Gets the type that registered the property.
        /// </summary>
        public abstract Type Owner { get; }

        /// <summary>
        /// Gets a value that indicates whether data validation is enabled for the property.
        /// </summary>
        public bool IsDataValidationEnabled { get; }

        /// <summary>
        /// Gets the value of the property on the instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>The property value.</returns>
        internal abstract TValue InvokeGetter(IAvaloniaObject instance);

        /// <summary>
        /// Sets the value of the property on the instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="value">The value.</param>
        internal abstract void InvokeSetter(IAvaloniaObject instance, BindingValue<TValue> value);

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

        /// <inheritdoc/>
        public override void Accept<TData>(IAvaloniaPropertyVisitor<TData> vistor, ref TData data)
        {
            vistor.Visit(this, ref data);
        }

        /// <inheritdoc/>
        internal override void RouteClearValue(IAvaloniaObject o)
        {
            o.ClearValue<TValue>(this);
        }

        /// <inheritdoc/>
        internal override object? RouteGetValue(IAvaloniaObject o)
        {
            return o.GetValue<TValue>(this);
        }

        /// <inheritdoc/>
        internal override IDisposable? RouteSetValue(
            IAvaloniaObject o,
            object value,
            BindingPriority priority)
        {
            var v = BindingValue<TValue>.FromUntyped(value);

            if (v.HasValue)
            {
                o.SetValue<TValue>(this, v.Value);
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

        /// <inheritdoc/>
        internal override IDisposable RouteBind(
            IAvaloniaObject o,
            IObservable<object> source,
            BindingPriority priority)
        {
            var adapter = TypedBindingAdapter<TValue>.Create(o, this, source);
            return o.Bind<TValue>(this, adapter);
        }

        internal override void RouteInheritanceParentChanged(AvaloniaObject o, IAvaloniaObject oldParent)
        {
            throw new NotSupportedException("Direct properties do not support inheritance.");
        }
    }
}
