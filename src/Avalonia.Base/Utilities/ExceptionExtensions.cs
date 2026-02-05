using System;
using System.Reflection;

namespace Avalonia.Utilities;

internal static class ExceptionExtensions
{
    /// <summary>
    /// Unwraps <see cref="AggregateException"/> and <see cref="TargetInvocationException"/>.
    /// </summary>
    public static Exception Unwrap(this Exception exception)
    {
        while (true)
        {
            if (exception is AggregateException a && a.InnerExceptions.Count == 1)
                exception = a.InnerExceptions[0];
            else if (exception is TargetInvocationException && exception.InnerException != null)
                exception = exception.InnerException;
            else
                return exception;
        }
    }
}
