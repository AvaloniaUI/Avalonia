using System;
using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    /// <summary>
    /// An <see cref="IValueEntry"/> that holds a binding whose source observable and target
    /// property are both untyped.
    /// </summary>
    internal class UntypedBindingEntry : BindingEntryBase<object?, object?>
    {
        private readonly Func<object?, bool>? _validate;

        public UntypedBindingEntry(
            ValueFrame frame,
            AvaloniaProperty property,
            IObservable<object?> source)
            : base(frame, property, source)
        {
            _validate = ((IStyledPropertyAccessor)property).ValidateValue;
        }

        protected override BindingValue<object?> ConvertAndValidate(object? value)
        {
            return UntypedValueUtils.ConvertAndValidate(value, Property.PropertyType, _validate);
        }

        protected override BindingValue<object?> ConvertAndValidate(BindingValue<object?> value)
        {
            throw new NotSupportedException();
        }
    }
}
