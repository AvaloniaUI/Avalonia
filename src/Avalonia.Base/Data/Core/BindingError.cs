using System;
using System.Diagnostics;

namespace Avalonia.Data.Core;

internal class BindingError
{
    public BindingError(Exception exception, BindingErrorType errorType)
    {
        Debug.Assert(errorType != BindingErrorType.None);

        Exception = exception;
        ErrorType = errorType;
    }

    public Exception Exception { get; }
    public BindingErrorType ErrorType { get; }
}
