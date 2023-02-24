using System;
using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// An <see cref="IValueEntry"/> that holds a binding whose source observable is untyped and
    /// target property is typed.
    /// </summary>
    internal sealed class SourceUntypedBindingEntry<TTarget> : BindingEntryBase<TTarget, object?>
    {
        private readonly Func<TTarget, bool>? _validate;

        public SourceUntypedBindingEntry(
            AvaloniaObject target,
            ValueFrame frame, 
            StyledProperty<TTarget> property,
            IObservable<object?> source)
                : base(target, frame, property, source)
        {
            _validate = property.ValidateValue;
        }

        public new StyledProperty<TTarget> Property => (StyledProperty<TTarget>)base.Property;

        protected override BindingValue<TTarget> ConvertAndValidate(object? value)
        {
            return UntypedValueUtils.ConvertAndValidate(value, Property.PropertyType, _validate);
        }

        protected override BindingValue<TTarget> ConvertAndValidate(BindingValue<object?> value)
        {
            throw new NotSupportedException();
        }

        protected override TTarget GetDefaultValue(Type ownerType) => Property.GetDefaultValue(ownerType);
    }
}
