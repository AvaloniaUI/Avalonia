using System;
using Avalonia.Data;

namespace Avalonia.Logging
{
    internal static class LoggerExtensions
    {
        public static void LogIfError(
            this BindingNotification notification,
            object source,
            AvaloniaProperty property)
        {
            if (notification.ErrorType == BindingErrorType.Error)
            {
                if (notification.Error is AggregateException aggregate)
                {
                    foreach (var inner in aggregate.InnerExceptions)
                    {
                        LogError(source, property, inner);
                    }
                }
                else
                {
                    LogError(source, property, notification.Error);
                }
            }
        }

        private static void LogError(object source, AvaloniaProperty property, Exception e)
        {
            var level = LogEventLevel.Warning;

            if (e is BindingChainException b &&
                !string.IsNullOrEmpty(b.Expression) &&
                string.IsNullOrEmpty(b.ExpressionErrorPoint))
            {
                // The error occurred at the root of the binding chain: it's possible that the
                // DataContext isn't set up yet, so log at Information level instead of Warning
                // to prevent spewing hundreds of errors.
                level = LogEventLevel.Information;
            }

            Logger.Log(
                level,
                LogArea.Binding,
                source,
                "Error in binding to {Target}.{Property}: {Message}",
                source,
                property,
                e.Message);
        }
    }
}
