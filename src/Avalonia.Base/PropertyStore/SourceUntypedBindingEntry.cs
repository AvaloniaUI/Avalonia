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
            ValueFrame frame, 
            StyledPropertyBase<TTarget> property,
            IObservable<object?> source)
                : base(frame, property, source)
        {
            _validate = property.ValidateValue;
        }

        public new StyledPropertyBase<TTarget> Property => (StyledPropertyBase<TTarget>)base.Property;

        protected override BindingValue<TTarget> ConvertAndValidate(object? value)
        {
            return UntypedValueUtils.ConvertAndValidate(value, Property.PropertyType, _validate);
        }

        protected override BindingValue<TTarget> ConvertAndValidate(BindingValue<object?> value)
        {
            throw new NotSupportedException();
        }
    }
}
