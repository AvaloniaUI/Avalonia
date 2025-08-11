﻿using System;
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
#if NET6_0_OR_GREATER
    private readonly CultureInfo _culture;
    private readonly CultureInfo _uiCulture;
#endif
    private CultureAndContext? _cultureAndContext;

#if NET6_0_OR_GREATER
    private CulturePreservingExecutionContext(ExecutionContext context, CultureInfo culture, CultureInfo uiCulture)
    {
        _context = context;
        _culture = culture;
        _uiCulture = uiCulture;
    }
#else
    private CulturePreservingExecutionContext(ExecutionContext context)
    {
        _context = context;
    }
#endif

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

#if NET6_0_OR_GREATER
        var culture = Thread.CurrentThread.CurrentCulture;
        var uiCulture = Thread.CurrentThread.CurrentUICulture;

        return new CulturePreservingExecutionContext(context, culture, uiCulture);
#else
        return new CulturePreservingExecutionContext(context);
#endif
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Restores the captured execution context and culture information to the current thread.
    /// This is the preferred method for .NET 6+ scenarios.
    /// </summary>
    /// <param name="executionContext">The execution context to restore.</param>
    public static void Restore(CulturePreservingExecutionContext executionContext)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (executionContext == null)
            ThrowNullContext();

        // Restore the execution context
        ExecutionContext.Restore(executionContext._context);

        // Restore the culture information
        Thread.CurrentThread.CurrentCulture = executionContext._culture;
        Thread.CurrentThread.CurrentUICulture = executionContext._uiCulture;
    }
#endif

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
#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
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
