#if NET6_0_OR_GREATER
// In .NET Core, the security context and call context are not supported, however,
// the impersonation context and culture would typically flow with the execution context.
// See: https://learn.microsoft.com/en-us/dotnet/api/system.threading.executioncontext
//
// So we can safely use ExecutionContext without worrying about culture flowing issues.
#else
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

namespace Avalonia.Threading;

/// <summary>
/// An ExecutionContext that preserves culture information across async operations.
/// This is a modernized version that removes legacy compatibility switches and
/// includes nullable reference type annotations.
/// </summary>
internal sealed class CulturePreservingExecutionContext
{
    private readonly ExecutionContext _context;
    private CultureAndContext? _cultureAndContext;

    private CulturePreservingExecutionContext(ExecutionContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Captures the current ExecutionContext and culture information.
    /// </summary>
    /// <returns>A new CulturePreservingExecutionContext instance, or null if no context needs to be captured.</returns>
    public static CulturePreservingExecutionContext? Capture()
    {
        // ExecutionContext.SuppressFlow had been called.
        // We expect ExecutionContext.Capture() to return null, so match that behavior and return null.
        if (ExecutionContext.IsFlowSuppressed())
        {
            return null;
        }

        var context = ExecutionContext.Capture();
        if (context == null)
            return null;

        return new CulturePreservingExecutionContext(context);
    }

    /// <summary>
    /// Runs the specified callback in the captured execution context while preserving culture information.
    /// This method is used for .NET Framework and earlier .NET versions.
    /// </summary>
    /// <param name="executionContext">The execution context to run in.</param>
    /// <param name="callback">The callback to execute.</param>
    /// <param name="state">The state to pass to the callback.</param>
    public static void Run(CulturePreservingExecutionContext executionContext, ContextCallback callback, object? state)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (callback == null)
            return;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (executionContext == null)
            ThrowNullContext();

        // Save culture information - we will need this to restore just before
        // the callback is actually invoked from CallbackWrapper.
        executionContext._cultureAndContext = CultureAndContext.Initialize(callback, state);

        try
        {
            ExecutionContext.Run(
                executionContext._context,
                s_callbackWrapperDelegate,
                executionContext._cultureAndContext);
        }
        finally
        {
            // Restore culture information - it might have been modified during callback execution.
            executionContext._cultureAndContext.RestoreCultureInfos();
        }
    }

    [DoesNotReturn]
    private static void ThrowNullContext()
    {
        throw new InvalidOperationException("ExecutionContext cannot be null.");
    }

    private static readonly ContextCallback s_callbackWrapperDelegate = CallbackWrapper;

    /// <summary>
    /// Executes the callback and saves culture values immediately afterwards.
    /// </summary>
    /// <param name="obj">Contains the actual callback and state.</param>
    private static void CallbackWrapper(object? obj)
    {
        var cultureAndContext = (CultureAndContext)obj!;

        // Restore culture information saved during Run()
        cultureAndContext.RestoreCultureInfos();

        try
        {
            // Execute the actual callback
            cultureAndContext.Callback(cultureAndContext.State);
        }
        finally
        {
            // Save any culture changes that might have occurred during callback execution
            cultureAndContext.CaptureCultureInfos();
        }
    }

    /// <summary>
    /// Helper class to manage culture information across execution contexts.
    /// </summary>
    private sealed class CultureAndContext
    {
        public ContextCallback Callback { get; }
        public object? State { get; }

        private CultureInfo? _culture;
        private CultureInfo? _uiCulture;

        private CultureAndContext(ContextCallback callback, object? state)
        {
            Callback = callback;
            State = state;
            CaptureCultureInfos();
        }

        public static CultureAndContext Initialize(ContextCallback callback, object? state)
        {
            return new CultureAndContext(callback, state);
        }

        public void CaptureCultureInfos()
        {
            _culture = Thread.CurrentThread.CurrentCulture;
            _uiCulture = Thread.CurrentThread.CurrentUICulture;
        }

        public void RestoreCultureInfos()
        {
            if (_culture != null)
                Thread.CurrentThread.CurrentCulture = _culture;

            if (_uiCulture != null)
                Thread.CurrentThread.CurrentUICulture = _uiCulture;
        }
    }
}
#endif
