using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.PropertyStore;
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// A styled avalonia property.
    /// </summary>
    public class StyledProperty<TValue> : AvaloniaProperty<TValue>, IStyledPropertyAccessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StyledProperty{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="hostType">The class that the property being is registered on.</param>
        /// <param name="metadata">The property metadata.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="validate">
        /// <para>A method which returns "false" for values that are never valid for this property.</para>
        /// <para>This method is not part of the property's metadata and so cannot be changed after registration.</para>
        /// </param>
        /// <param name="notifying">A <see cref="AvaloniaProperty.Notifying"/> callback.</param>
        internal StyledProperty(
            string name,
            Type ownerType,
            Type hostType,
            StyledPropertyMetadata<TValue> metadata,
            bool inherits = false,
            Func<TValue, bool>? validate = null,
            Action<AvaloniaObject, bool>? notifying = null)
                : base(name, ownerType, hostType, metadata, notifying)
        {
            Inherits = inherits;
            ValidateValue = validate;

            if (validate?.Invoke(metadata.DefaultValue) == false)
            {
                throw new ArgumentException(
                    $"'{metadata.DefaultValue}' is not a valid default value for '{name}'.");
            }
        }

        /// <summary>
        /// A method which returns "false" for values that are never valid for this property.
        /// </summary>
        public Func<TValue, bool>? ValidateValue { get; }

        /// <summary>
        /// Registers the property on another type.
        /// </summary>
        /// <typeparam name="TOwner">The type of the additional owner.</typeparam>
        /// <returns>The property.</returns>        
        public StyledProperty<TValue> AddOwner<TOwner>(StyledPropertyMetadata<TValue>? metadata = null) where TOwner : AvaloniaObject
        {
            AvaloniaPropertyRegistry.Instance.Register(typeof(TOwner), this);
            if (metadata != null)
            {
                OverrideMetadata<TOwner>(metadata);
            }

            return this;
        }

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
        public void OverrideMetadata<T>(StyledPropertyMetadata<TValue> metadata) where T : AvaloniaObject => OverrideMetadata(typeof(T), metadata);

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
            if (value is null)
            {
                if (!typeof(TValue).IsValueType || Nullable.GetUnderlyingType(typeof(TValue)) != null)
                    return ValidateValue?.Invoke(default!) ?? true;
            }
            else if (value is TValue typed)
            {
                return ValidateValue?.Invoke(typed) ?? true;
            }

            return false;
        }

        internal override EffectiveValue CreateEffectiveValue(AvaloniaObject o)
        {
            return o.GetValueStore().CreateEffectiveValue(this);
        }

        /// <inheritdoc/>
        internal override void RouteClearValue(AvaloniaObject o)
        {
            o.ClearValue<TValue>(this);
        }

        internal override void RouteCoerceDefaultValue(AvaloniaObject o)
        {
            o.GetValueStore().CoerceDefaultValue(this);
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
        internal override IDisposable? RouteSetValue(
            AvaloniaObject target,
            object? value,
            BindingPriority priority)
        {
            if (ShouldSetValue(target, value, out var converted))
                return target.SetValue<TValue>(this, converted, priority);
            return null;
        }

        internal override void RouteSetCurrentValue(AvaloniaObject target, object? value)
        {
            if (ShouldSetValue(target, value, out var converted))
                target.SetCurrentValue<TValue>(this, converted);
        }

        internal override IDisposable RouteBind(
            AvaloniaObject target,
            IObservable<object?> source,
            BindingPriority priority)
        {
            return target.Bind<TValue>(this, source, priority);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.ImplicitTypeConversionSupressWarningMessage)]
        private bool ShouldSetValue(AvaloniaObject target, object? value, [NotNullWhen(true)] out TValue? converted)
        {
            if (value == BindingOperations.DoNothing)
            {
                converted = default;
                return false; 
            }
            if (value == UnsetValue)
            {
                target.ClearValue(this);
                converted = default;
                return false;
            }
            else if (TypeUtilities.TryConvertImplicit(PropertyType, value, out var v))
            {
                converted = (TValue)v!;
                return true;
            }
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
    }
}
