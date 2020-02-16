// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Diagnostics;
using Avalonia.Data;
using Avalonia.Reactive;

namespace Avalonia
{
    /// <summary>
    /// Base class for styled properties.
    /// </summary>
    public abstract class StyledPropertyBase<TValue> : AvaloniaProperty<TValue>, IStyledPropertyAccessor
    {
        private bool _inherits;

        /// <summary>
        /// Initializes a new instance of the <see cref="StyledPropertyBase{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="metadata">The property metadata.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="validate">A value validation callback.</param>
        /// <param name="notifying">A <see cref="AvaloniaProperty.Notifying"/> callback.</param>
        protected StyledPropertyBase(
            string name,
            Type ownerType,            
            StyledPropertyMetadata<TValue> metadata,
            bool inherits = false,
            Func<TValue, bool> validate = null,
            Action<IAvaloniaObject, bool> notifying = null)
                : base(name, ownerType, metadata, notifying)
        {
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentNullException>(ownerType != null);

            if (name.Contains("."))
            {
                throw new ArgumentException("'name' may not contain periods.");
            }

            _inherits = inherits;
            ValidateValue = validate;
            HasCoercion |= metadata.CoerceValue != null;

            if (validate?.Invoke(metadata.DefaultValue) == false)
            {
                throw new ArgumentException(
                    $"'{metadata.DefaultValue}' is not a valid default value for '{name}'.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StyledPropertyBase{T}"/> class.
        /// </summary>
        /// <param name="source">The property to add the owner to.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        protected StyledPropertyBase(StyledPropertyBase<TValue> source, Type ownerType)
            : base(source, ownerType, null)
        {
            _inherits = source.Inherits;
        }

        /// <summary>
        /// Gets a value indicating whether the property inherits its value.
        /// </summary>
        /// <value>
        /// A value indicating whether the property inherits its value.
        /// </value>
        public override bool Inherits => _inherits;

        /// <summary>
        /// Gets the value validation callback for the property.
        /// </summary>
        public Func<TValue, bool> ValidateValue { get; }

        /// <summary>
        /// Gets a value indicating whether this property has any value coercion callbacks defined
        /// in its metadata.
        /// </summary>
        internal bool HasCoercion { get; private set; }

        public TValue CoerceValue(IAvaloniaObject instance, TValue baseValue)
        {
            var metadata = GetMetadata(instance.GetType());

            if (metadata.CoerceValue != null)
            {
                return metadata.CoerceValue.Invoke(instance, baseValue);
            }

            return baseValue;
        }

        /// <summary>
        /// Gets the default value for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The default value.</returns>
        public TValue GetDefaultValue(Type type)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            return GetMetadata(type).DefaultValue;
        }

        /// <summary>
        /// Gets the property metadata for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// The property metadata.
        /// </returns>
        public new StyledPropertyMetadata<TValue> GetMetadata(Type type)
        {
            return (StyledPropertyMetadata<TValue>)base.GetMetadata(type);
        }

        /// <summary>
        /// Overrides the default value for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="defaultValue">The default value.</param>
        public void OverrideDefaultValue<T>(TValue defaultValue) where T : IAvaloniaObject
        {
            OverrideDefaultValue(typeof(T), defaultValue);
        }

        /// <summary>
        /// Overrides the default value for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="defaultValue">The default value.</param>
        public void OverrideDefaultValue(Type type, TValue defaultValue)
        {
            OverrideMetadata(type, new StyledPropertyMetadata<TValue>(defaultValue));
        }

        /// <summary>
        /// Overrides the metadata for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="metadata">The metadata.</param>
        public void OverrideMetadata<T>(StyledPropertyMetadata<TValue> metadata) where T : IAvaloniaObject
        {
            base.OverrideMetadata(typeof(T), metadata);
        }

        /// <summary>
        /// Overrides the metadata for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="metadata">The metadata.</param>
        public void OverrideMetadata(Type type, StyledPropertyMetadata<TValue> metadata)
        {
            if (ValidateValue != null)
            {
                if (!ValidateValue(metadata.DefaultValue))
                {
                    throw new ArgumentException(
                        $"'{metadata.DefaultValue}' is not a valid default value for '{Name}'.");
                }
            }

            HasCoercion |= metadata.CoerceValue != null;

            base.OverrideMetadata(type, metadata);
        }

        /// <summary>
        /// Gets the string representation of the property.
        /// </summary>
        /// <returns>The property's string representation.</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <inheritdoc/>
        object IStyledPropertyAccessor.GetDefaultValue(Type type) => GetDefaultBoxedValue(type);

        /// <inheritdoc/>
        internal override void RouteClearValue(IAvaloniaObject o)
        {
            o.ClearValue<TValue>(this);
        }

        /// <inheritdoc/>
        internal override object RouteGetValue(IAvaloniaObject o)
        {
            return o.GetValue<TValue>(this);
        }

        /// <inheritdoc/>
        internal override void RouteSetValue(
            IAvaloniaObject o,
            object value,
            BindingPriority priority)
        {
            var v = TryConvert(value);

            if (v.HasValue)
            {
                o.SetValue<TValue>(this, (TValue)v.Value, priority);
            }
            else if (v.Type == BindingValueType.UnsetValue)
            {
                o.ClearValue(this);
            }
            else if (v.HasError)
            {
                throw v.Error;
            }
        }

        /// <inheritdoc/>
        internal override IDisposable RouteBind(
            IAvaloniaObject o,
            IObservable<BindingValue<object>> source,
            BindingPriority priority)
        {
            var adapter = TypedBindingAdapter<TValue>.Create(o, this, source);
            return o.Bind<TValue>(this, adapter, priority);
        }

        /// <inheritdoc/>
        internal override void RouteInheritanceParentChanged(
            AvaloniaObject o,
            IAvaloniaObject oldParent)
        {
            o.InheritanceParentChanged(this, oldParent);
        }

        private object GetDefaultBoxedValue(Type type)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            return GetMetadata(type).DefaultValue;
        }

        [DebuggerHidden]
        private Func<IAvaloniaObject, TValue, TValue> Cast<THost>(Func<THost, TValue, TValue> validate)
        {
            return (o, v) => validate((THost)o, v);
        }
    }
}
