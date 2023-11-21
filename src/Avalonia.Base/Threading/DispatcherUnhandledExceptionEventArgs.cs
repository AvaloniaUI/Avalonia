using System;
using Avalonia.Interactivity;

namespace Avalonia.Threading;

public class DispatcherUnhandledExceptionEventArgs : RoutedEventArgs
{
    public Dispatcher Dispatcher { get; }
    public Exception Exception { get; }

    public DispatcherUnhandledExceptionEventArgs(Dispatcher dispatcher, Exception exception)
    {
        Dispatcher = dispatcher;
        Exception = exception;
    }
}
