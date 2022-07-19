using System.Diagnostics.CodeAnalysis;
using Avalonia.Utilities;

namespace Avalonia.PropertyStore
{
    internal static class UntypedValueUtils
    {
        public static bool TryConvertAndValidate(
            AvaloniaProperty property,
            object? value,
            out object? result)
        {
            if (TypeUtilities.TryConvertImplicit(property.PropertyType, value, out result))
                return ((IStyledPropertyAccessor)property).ValidateValue(result);

            result = default;
            return false;
        }

        public static bool TryConvertAndValidate<T>(
            StyledPropertyBase<T> property,
            object? value, 
            [MaybeNullWhen(false)] out T result)
        {
            if (TypeUtilities.TryConvertImplicit(typeof(T), value, out var v))
            {
                result = (T)v!;

                if (property.ValidateValue?.Invoke(result) != false)
                    return true;
            }

            result = default;
            return false;
        }
    }
}
