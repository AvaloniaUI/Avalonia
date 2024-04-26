using System;
using System.Diagnostics;
using Avalonia.Interactivity;

namespace Avalonia.Threading;

/// <summary>
/// Represents the method that will handle the <see cref="Dispatcher.UnhandledException"/> event.
/// </summary>
public delegate void DispatcherUnhandledExceptionEventHandler(object sender, DispatcherUnhandledExceptionEventArgs e);

/// <summary>
/// Provides data for the <see cref="Dispatcher.UnhandledException"/> event.
/// </summary>
public sealed class DispatcherUnhandledExceptionEventArgs : DispatcherEventArgs
{
    private Exception _exception;
    private bool _handled;

    internal DispatcherUnhandledExceptionEventArgs(Dispatcher dispatcher) : base(dispatcher)
    {
        _exception = null!;
    }

    /// <summary>
    /// Gets the exception that was raised when executing code by way of the dispatcher.
    /// </summary>
    public Exception Exception => _exception;
    
    /// <summary>
    /// Gets or sets whether the exception event has been handled.
    /// </summary>
    public bool Handled
    {
        get
        {
            return _handled;
        }
        set
        {
            // Only allow to be set true.
            if (value)
            {
                _handled = value;
            }
        }
    }

    internal void Initialize(Exception exception, bool handled)
    {
        Debug.Assert(exception != null);
        _exception = exception;
        _handled = handled;
    }
}
