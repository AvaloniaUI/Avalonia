using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Data;

namespace Avalonia.PropertyStore
{
    internal static class LoggingUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogIfNecessary(
            AvaloniaObject owner,
            AvaloniaProperty property,
            BindingNotification value)
        {
            if (value.ErrorType != BindingErrorType.None)
                Log(owner, property, value.Error!);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogIfNecessary<T>(
            AvaloniaObject owner,
            AvaloniaProperty property,
            BindingValue<T> value)
        {
            if (value.HasError)
                Log(owner, property, value.Error!);
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

        private static void Log(
            AvaloniaObject owner,
            AvaloniaProperty property,
            Exception e)
        {
            if (e is TargetInvocationException t)
                e = t.InnerException!;

            if (e is AggregateException a)
            {
                foreach (var i in a.InnerExceptions)
                    Log(owner, property, i);
            }
            else
            {
                owner.GetBindingWarningLogger(property, e)?.Log(
                    owner,
                    "Error in binding to {Target}.{Property}: {Message}",
                    owner,
                    property,
                    e.Message);
            }
        }
    }
}
