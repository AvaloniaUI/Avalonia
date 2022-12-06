using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Avalonia.Data;
using Avalonia.PropertyStore;
using Avalonia.Reactive;
using Avalonia.Styling;
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// Base class for styled properties.
    /// </summary>
    public abstract class StyledPropertyBase<TValue> : AvaloniaProperty<TValue>, IStyledPropertyAccessor
    {
        private readonly bool _inherits;

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
            Func<TValue, bool>? validate = null,
            Action<AvaloniaObject, bool>? notifying = null)
                : base(name, ownerType, metadata, notifying)
        {
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
        public Func<TValue, bool>? ValidateValue { get; }

        /// <summary>
        /// Gets a value indicating whether this property has any value coercion callbacks defined
        /// in its metadata.
        /// </summary>
        internal bool HasCoercion { get; private set; }

        public TValue CoerceValue(AvaloniaObject instance, TValue baseValue)
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
            _ = type ?? throw new ArgumentNullException(nameof(type));
            return (StyledPropertyMetadata<TValue>)base.GetMetadata(type);
        }

        /// <summary>
        /// Overrides the default value for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="defaultValue">The default value.</param>
        public void OverrideDefaultValue<T>(TValue defaultValue) where T : AvaloniaObject
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
        public void OverrideMetadata<T>(StyledPropertyMetadata<TValue> metadata) where T : AvaloniaObject
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
        object? IStyledPropertyAccessor.GetDefaultValue(Type type) => GetDefaultBoxedValue(type);

        bool IStyledPropertyAccessor.ValidateValue(object? value)
        {
            if (value is null && !typeof(TValue).IsValueType)
                return ValidateValue?.Invoke(default!) ?? true;
            if (value is TValue typed)
                return ValidateValue?.Invoke(typed) ?? true;
            return false;
        }

        internal override EffectiveValue CreateEffectiveValue(AvaloniaObject o)
        {
            return new EffectiveValue<TValue>(o, this);
        }

        /// <inheritdoc/>
        internal override void RouteClearValue(AvaloniaObject o)
        {
            o.ClearValue<TValue>(this);
        }

        /// <inheritdoc/>
        internal override object? RouteGetValue(AvaloniaObject o)
        {
            return o.GetValue<TValue>(this);
        }

        /// <inheritdoc/>
        internal override object? RouteGetBaseValue(AvaloniaObject o)
        {
            var value = o.GetBaseValue<TValue>(this);
            return value.HasValue ? value.Value : AvaloniaProperty.UnsetValue;
        }

        /// <inheritdoc/>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.ImplicitTypeConvertionSupressWarningMessage)]
        internal override IDisposable? RouteSetValue(
            AvaloniaObject target,
            object? value,
            BindingPriority priority)
        {
            if (value == BindingOperations.DoNothing)
            {
                return null;
            }
            else if (value == UnsetValue)
            {
                target.ClearValue(this);
                return null;
            }
            else if (TypeUtilities.TryConvertImplicit(PropertyType, value, out var converted))
            {
                return target.SetValue<TValue>(this, (TValue)converted!, priority);
            }
            else
            {
                var type = value?.GetType().FullName ?? "(null)";
                throw new ArgumentException($"Invalid value for Property '{Name}': '{value}' ({type})");
            }
        }

        internal override IDisposable RouteBind(
            AvaloniaObject target,
            IObservable<object?> source,
            BindingPriority priority)
        {
            return target.Bind<TValue>(this, source, priority);
        }

        private object? GetDefaultBoxedValue(Type type)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            return GetMetadata(type).DefaultValue;
        }
    }
}
