using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Animation;
using Avalonia.Data;
using Avalonia.Metadata;
using Avalonia.PropertyStore;

namespace Avalonia.Styling
{
    /// <summary>
    /// A setter for a <see cref="Style"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="Setter"/> is used to set a <see cref="AvaloniaProperty"/> value on a
    /// <see cref="AvaloniaObject"/> depending on a condition.
    /// </remarks>
    public class Setter : SetterBase, IValueEntry, ISetterInstance, IAnimationSetter
    {
        private object? _value;
        private DirectPropertySetterInstance? _direct;

        /// <summary>
        /// Initializes a new instance of the <see cref="Setter"/> class.
        /// </summary>
        public Setter()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Setter"/> class.
        /// </summary>
        /// <param name="property">The property to set.</param>
        /// <param name="value">The property value.</param>
        public Setter(AvaloniaProperty property, object? value)
        {
            Property = property;
            Value = value;
        }

        /// <summary>
        /// Gets or sets the property to set.
        /// </summary>
        public AvaloniaProperty? Property { get; set; }

        /// <summary>
        /// Gets or sets the property value.
        /// </summary>
        [Content]
        [AssignBinding]
        [DependsOn(nameof(Property))]
        public object? Value
        {
            get => _value;
            set
            {
                (value as ISetterValue)?.Initialize(this);
                _value = value;
            }
        }

        bool IValueEntry.HasValue => true;
        AvaloniaProperty IValueEntry.Property => EnsureProperty();

        public override string ToString() => $"Setter: {Property} = {Value}";

        void IValueEntry.Unsubscribe() { }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.ImplicitTypeConversionSupressWarningMessage)]
        internal override ISetterInstance Instance(IStyleInstance instance, StyledElement target)
        {
            if (target is not AvaloniaObject ao)
                throw new InvalidOperationException("Don't know how to instance a style on this type.");
            if (Property is null)
                throw new InvalidOperationException("Setter.Property must be set.");
            if (Property.IsDirect && instance.HasActivator)
                throw new InvalidOperationException(
                    $"Cannot set direct property '{Property}' in '{instance.Source}' because the style has an activator.");

            if (Value is IBinding binding)
                return SetBinding((StyleInstance)instance, ao, binding);
            else if (Value is ITemplate template && !typeof(ITemplate).IsAssignableFrom(Property.PropertyType))
                return new PropertySetterTemplateInstance(Property, template);
            else if (!Property.IsValidValue(Value))
                throw new InvalidCastException($"Setter value '{Value}' is not a valid value for property '{Property}'.");
            else if (Property.IsDirect)
                return SetDirectValue(target);
            else
                return this;
        }

        object? IValueEntry.GetValue() => Value;

        bool IValueEntry.GetDataValidationState(out BindingValueType state, out Exception? error)
        {
            state = BindingValueType.Value;
            error = null;
            return false;
        }

        private AvaloniaProperty EnsureProperty()
        {
            return Property ?? throw new InvalidOperationException("Setter.Property must be set.");
        }

        private ISetterInstance SetBinding(StyleInstance instance, AvaloniaObject target, IBinding binding)
        {
            if (!Property!.IsDirect)
            {
                var hasDataValidation = Property.GetMetadata(target.GetType()).EnableDataValidation ?? false;
                var i = binding.Initiate(target, Property, enableDataValidation: hasDataValidation)!;
                var mode = i.Mode;

                if (mode == BindingMode.Default)
                {
                    mode = Property!.GetMetadata(target.GetType()).DefaultBindingMode;
                }

                if (mode == BindingMode.OneWay || mode == BindingMode.TwoWay)
                {
                    return new PropertySetterBindingInstance(target, instance, Property, mode, i.Source);
                }

                throw new NotSupportedException();
            }
            else
            {
                target.Bind(Property, binding);
                return new DirectPropertySetterBindingInstance();
            }
        }

        private ISetterInstance SetDirectValue(StyledElement target)
        {
            target.SetValue(Property!, Value);
            return _direct ??= new DirectPropertySetterInstance();
        }
    }
}
