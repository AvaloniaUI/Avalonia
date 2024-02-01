using System;

namespace Avalonia.Threading;

public partial class Dispatcher
{
    internal static readonly object ExceptionDataKey = new();
    private DispatcherUnhandledExceptionFilterEventHandler? _unhandledExceptionFilter;

    // Pre-allocated arguments for exception handling.
    // This helps avoid allocations in the handler code, a potential
    // source of secondary exceptions (i.e. in Out-Of-Memory cases).
    private DispatcherUnhandledExceptionEventArgs _unhandledExceptionEventArgs;
    private DispatcherUnhandledExceptionFilterEventArgs _exceptionFilterEventArgs;

    /// <summary>
    /// Occurs when a thread exception is thrown and uncaught during execution of a delegate by way of <see cref="Invoke(System.Action)"/> or <see cref="InvokeAsync(System.Action)"/>.
    /// </summary>
    /// <remarks>
    /// This event is raised when an exception that was thrown during execution of a delegate by way of <see cref="Invoke(System.Action)"/> or <see cref="InvokeAsync(System.Action)"/> is uncaught.
    /// A handler can mark the exception as handled, which will prevent the internal exception handler from being called.
    /// Event handlers for this event must be written with care to avoid creating secondary exceptions and to catch any that occur. It is recommended to avoid allocating memory or doing any resource intensive operations in the handler.
    /// </remarks>
    public event DispatcherUnhandledExceptionEventHandler? UnhandledException;

    /// <summary>
    /// Occurs when a thread exception is thrown and uncaught during execution of a delegate by way of <see cref="Invoke(System.Action)"/> or <see cref="InvokeAsync(System.Action)"/> when in the filter stage.
    /// </summary>
    /// <remarks>
    /// This event is raised during the filter stage for an exception that is raised during execution of a delegate by way of <see cref="Invoke(System.Action)"/> or <see cref="InvokeAsync(System.Action)"/> and is uncaught.
    /// The call stack is not unwound at this point (first-chance exception).
    /// Event handlers for this event must be written with care to avoid creating secondary exceptions and to catch any that occur. It is recommended to avoid allocating memory or doing any resource intensive operations in the handler.
    /// The <see cref="UnhandledExceptionFilter"/> event provides a means to not raise the <see cref="UnhandledException"/> event. The <see cref="UnhandledExceptionFilter"/> event is raised first,
    /// and If <see cref="DispatcherUnhandledExceptionFilterEventArgs.RequestCatch" /> is set to false, the <see cref="UnhandledException"/> event will not be raised.
    /// </remarks>
    public event DispatcherUnhandledExceptionFilterEventHandler? UnhandledExceptionFilter
    {
        add
        {
            _unhandledExceptionFilter += value;
        }
        remove
        {
            _unhandledExceptionFilter -= value;
        }
    }

    /// Exception filter returns true if exception should be caught.
    internal bool ExceptionFilter(Exception e)
    {
        // see whether this dispatcher has already seen the exception.
        // This can happen when the dispatcher is re-entered via
        // PushFrame (or similar).
        if (!e.Data.Contains(ExceptionDataKey))
        {
            // first time we've seen this exception - add data to the exception
            e.Data.Add(ExceptionDataKey, null);
        }
        else
        {
            // we've seen this exception before - don't catch it
            return false;
        }

        // By default, Request catch if there's anyone signed up to catch it;
        var requestCatch = UnhandledException is not null;

        // The app can hook up an ExceptionFilter to avoid catching it.
        // ExceptionFilter will run REGARDLESS of whether there are exception handlers.
        if (_unhandledExceptionFilter != null)
        {
            // The default requestCatch value that is passed in the args
            // should be returned unchanged if filters don't set them explicitly.
            _exceptionFilterEventArgs.Initialize(e, requestCatch);
            var bSuccess = false;
            try
            {
                _unhandledExceptionFilter(this, _exceptionFilterEventArgs);
                bSuccess = true;
            }
            finally
            {
                if (bSuccess)
                {
                    requestCatch = _exceptionFilterEventArgs.RequestCatch;
                }

                // For bSuccess is false,
                // To be in line with default behavior of structured exception handling,
                // we would want to set requestCatch to false, however, this means one
                // poorly programmed filter will knock out all dispatcher exception handling.
                // If an exception filter fails, we run with whatever value is set thus far.
            }
        }

        return requestCatch;
    }

    internal bool CatchException(Exception e)
    {
        var handled = false;

        if (UnhandledException != null)
        {
            _unhandledExceptionEventArgs.Initialize(e, false);

            var bSuccess = false;
            try
            {
                UnhandledException(this, _unhandledExceptionEventArgs);
                handled = _unhandledExceptionEventArgs.Handled;
                bSuccess = true;
            }
            finally
            {
                if (!bSuccess)
                    handled = false;
            }
        }

        return handled;
    }

    /// Returns true, if exception was handled.
    internal bool TryCatchWhen(Exception e)
    {
        if (ExceptionFilter(e))
        {
            if (!CatchException(e))
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        return true;
    }
}
