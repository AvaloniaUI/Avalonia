using System;
using System.Diagnostics;
using Avalonia.Data;
using Avalonia.PropertyStore;
using Avalonia.Reactive;
using Avalonia.Utilities;

#nullable enable

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
            Func<TValue?, bool>? validate = null,
            Action<IAvaloniaObject, bool>? notifying = null)
                : base(name, ownerType, metadata, notifying)
        {
            _ = name ?? throw new ArgumentNullException(nameof(name));
            _ = ownerType ?? throw new ArgumentNullException(nameof(ownerType));

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
        public Func<TValue?, bool>? ValidateValue { get; }

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
            _ = type ?? throw new ArgumentNullException(nameof(type));

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

        /// <inheritdoc/>
        public override void Accept<TData>(IAvaloniaPropertyVisitor<TData> vistor, ref TData data)
        {
            vistor.Visit(this, ref data);
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
        object? IStyledPropertyAccessor.GetDefaultValue(Type type) => GetDefaultBoxedValue(type);

        bool IStyledPropertyAccessor.ValidateValue(object? value)
        {
            if (value is TValue typed)
                return ValidateValue?.Invoke(typed) ?? true;
            return false;
        }

        internal override IObservable<object?> GetObservable(AvaloniaObject target)
        {
            return (AvaloniaPropertyObservable)target.GetObservable(this);
        }

        internal override object? GetValue(AvaloniaObject target) => target.GetValue(this);

        internal override object GetValueByPriority(
            AvaloniaObject o,
            BindingPriority minPriority,
            BindingPriority maxPriority)
        {
            var v = o.GetValueByPriority(this, minPriority, maxPriority);
            return v.HasValue ? v.Value : AvaloniaProperty.UnsetValue;
        }

        internal override void RaisePropertyChanged(
            AvaloniaObject owner,
            object? oldValue,
            object? newValue,
            BindingPriority priority,
            bool isEffectiveValueChange)
        {
            var o = oldValue != UnsetValue ? new Optional<TValue>((TValue?)oldValue) : default;
            var n = BindingValue<TValue>.FromUntyped(newValue);
            owner.RaisePropertyChanged(this, o, n, priority, isEffectiveValueChange);
        }

        internal override void SetValue(AvaloniaObject target, object? value)
        {
            if (value == BindingOperations.DoNothing)
                return;
            else if (value == UnsetValue)
                target.ClearValue(this);
            else if (TypeUtilities.TryConvertImplicit(PropertyType, value, out var converted))
                target.SetValue<TValue>(this, (TValue?)converted);
            else
            {
                var type = value?.GetType().FullName ?? "(null)";
                throw new ArgumentException($"Invalid value for Property '{Name}': '{value}' ({type})");
            }
        }

        private object? GetDefaultBoxedValue(Type type)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));

            return GetMetadata(type).DefaultValue;
        }

        [DebuggerHidden]
        private Func<IAvaloniaObject, TValue, TValue> Cast<THost>(Func<THost, TValue, TValue> validate)
        {
            return (o, v) => validate((THost)o, v);
        }
    }
}
