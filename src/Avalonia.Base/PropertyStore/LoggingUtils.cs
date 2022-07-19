using System;
using System.Runtime.CompilerServices;
using Avalonia.Data;
using Avalonia.Logging;

namespace Avalonia.PropertyStore
{
    internal static class LoggingUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogIfNecessary<T>(
            AvaloniaObject owner,
            AvaloniaProperty property,
            BindingValue<T> value)
        {
            if (value.HasError)
                Log(owner, property, value);
        }

        public static void LogInvalidValue(
            AvaloniaObject owner,
            AvaloniaProperty property,
            Type expectedType,
            object? value)
        {
            if (value is not null)
            {
                owner.GetBindingWarningLogger(property, null)?.Log(
                    owner,
                    "Error in binding to {Target}.{Property}: expected {ExpectedType}, got {Value} ({ValueType})",
                    owner,
                    property,
                    expectedType,
                    value,
                    value.GetType());
            }
            else
            {
                owner.GetBindingWarningLogger(property, null)?.Log(
                    owner,
                    "Error in binding to {Target}.{Property}: expected {ExpectedType}, got null",
                    owner,
                    property,
                    expectedType);
            }
        }

        private static void Log<T>(
            AvaloniaObject owner,
            AvaloniaProperty property,
            BindingValue<T> value)
        {
            owner.GetBindingWarningLogger(property, value.Error)?.Log(
                owner,
                "Error in binding to {Target}.{Property}: {Message}",
                owner,
                property,
                value.Error!.Message);
        }
    }
}
