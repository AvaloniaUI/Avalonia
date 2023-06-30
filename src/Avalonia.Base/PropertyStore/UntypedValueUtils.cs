using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;
using Avalonia.Utilities;

namespace Avalonia.PropertyStore
{
    internal static class UntypedValueUtils
    {
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.ImplicitTypeConversionSupressWarningMessage)]
        public static BindingValue<T> ConvertAndValidate<T>(
            object? value,
            Type targetType,
            Func<T, bool>? validate)
        {
            var v = BindingValue<T>.FromUntyped(value, targetType);
            
            if (v.HasValue && validate?.Invoke(v.Value) == false)
            {
                return BindingValue<T>.BindingError(
                    new InvalidCastException($"'{v.Value}' is not a valid value."));
            }

            return v;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.ImplicitTypeConversionSupressWarningMessage)]
        public static bool TryConvertAndValidate<T>(
            StyledProperty<T> property,
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
