using System;
using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// A direct avalonia property.
    /// </summary>
    /// <typeparam name="TOwner">The class that registered the property.</typeparam>
    /// <typeparam name="TValue">The type of the property's value.</typeparam>
    /// <remarks>
    /// Direct avalonia properties are backed by a field on the object, but exposed via the
    /// <see cref="AvaloniaProperty"/> system. They hold a getter and an optional setter which
    /// allows the avalonia property system to read and write the current value.
    /// </remarks>
    public class DirectProperty<TOwner, TValue> : DirectPropertyBase<TValue>, IDirectPropertyAccessor
        where TOwner : AvaloniaObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectProperty{TOwner, TValue}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="getter">Gets the current value of the property.</param>
        /// <param name="setter">Sets the value of the property. May be null.</param>
        /// <param name="metadata">The property metadata.</param>
        internal DirectProperty(
            string name,
            Func<TOwner, TValue> getter,
            Action<TOwner, TValue>? setter,
            DirectPropertyMetadata<TValue> metadata)
            : base(name, typeof(TOwner), metadata)
        {
            Getter = getter ?? throw new ArgumentNullException(nameof(getter));
            Setter = setter;
            IsDirect = true;
            IsReadOnly = setter is null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaProperty"/> class.
        /// </summary>
        /// <param name="source">The property to copy.</param>
        /// <param name="getter">Gets the current value of the property.</param>
        /// <param name="setter">Sets the value of the property. May be null.</param>
        /// <param name="metadata">Optional overridden metadata.</param>
        private DirectProperty(
            DirectPropertyBase<TValue> source,
            Func<TOwner, TValue> getter,
            Action<TOwner, TValue>? setter,
            DirectPropertyMetadata<TValue> metadata)
            : base(source, typeof(TOwner), metadata)
        {
            Getter = getter ?? throw new ArgumentNullException(nameof(getter));
            Setter = setter;
            IsDirect = true;
            IsReadOnly = setter is null;
        }

        /// <summary>
        /// Gets the getter function.
        /// </summary>
        public Func<TOwner, TValue> Getter { get; }

        /// <summary>
        /// Gets the setter function.
        /// </summary>
        public Action<TOwner, TValue>? Setter { get; }

        /// <summary>
        /// Registers the direct property on another type.
        /// </summary>
        /// <typeparam name="TNewOwner">The type of the additional owner.</typeparam>
        /// <param name="getter">Gets the current value of the property.</param>
        /// <param name="setter">Sets the value of the property.</param>
        /// <param name="unsetValue">
        /// The value to use when the property is set to <see cref="AvaloniaProperty.UnsetValue"/>
        /// </param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="enableDataValidation">
        /// Whether the property is interested in data validation.
        /// </param>
        /// <returns>The property.</returns>
        public DirectProperty<TNewOwner, TValue> AddOwner<TNewOwner>(
            Func<TNewOwner, TValue> getter,
            Action<TNewOwner, TValue>? setter = null,
            TValue unsetValue = default!,
            BindingMode defaultBindingMode = BindingMode.Default,
            bool enableDataValidation = false)
                where TNewOwner : AvaloniaObject
        {
            var metadata = new DirectPropertyMetadata<TValue>(
                unsetValue: unsetValue,
                defaultBindingMode: defaultBindingMode,
                enableDataValidation: enableDataValidation);

            metadata.Merge(GetMetadata<TOwner>(), this);

            var result = new DirectProperty<TNewOwner, TValue>(
                (DirectPropertyBase<TValue>)this,
                getter,
                setter,
                metadata);

            AvaloniaPropertyRegistry.Instance.Register(typeof(TNewOwner), result);
            return result;
        }

        /// <inheritdoc/>
        internal override TValue InvokeGetter(AvaloniaObject instance)
        {
            return Getter((TOwner)instance);
        }

        /// <inheritdoc/>
        internal override void InvokeSetter(AvaloniaObject instance, BindingValue<TValue> value)
        {
            if (Setter == null)
            {
                throw new ArgumentException($"The property {Name} is readonly.");
            }

            if (value.HasValue)
            {
                Setter((TOwner)instance, value.Value);
            }
        }

        /// <inheritdoc/>
        object? IDirectPropertyAccessor.GetValue(AvaloniaObject instance)
        {
            return Getter((TOwner)instance);
        }

        /// <inheritdoc/>
        void IDirectPropertyAccessor.SetValue(AvaloniaObject instance, object? value)
        {
            if (Setter == null)
            {
                throw new ArgumentException($"The property {Name} is readonly.");
            }

            Setter((TOwner)instance, (TValue)value!);
        }

        object? IDirectPropertyAccessor.GetUnsetValue(Type type)
        {
            var metadata = GetMetadata(type);
            return metadata.UnsetValue;
        }
    }
}
