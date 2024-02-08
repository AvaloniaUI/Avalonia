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
    }
}
