using System;
using System.Globalization;
using System.Threading;

namespace Avalonia.Threading;

/// <summary>
/// A wrapper around <see cref="ExecutionContext"/> for running dispatcher operations that deliberately
/// opts OUT of the runtime flowing <see cref="CultureInfo.CurrentCulture"/> /
/// <see cref="CultureInfo.CurrentUICulture"/> through the execution context.
/// </summary>
/// <remarks>
/// <para>
/// Since .NET Framework 4.6 the current culture and UI culture flow with the <see cref="ExecutionContext"/>,
/// like any <see cref="AsyncLocal{T}"/>. For a UI toolkit this is the wrong model, and we explicitly do NOT
/// want it. The culture is a property of the UI thread: the application sets it once (e.g. for localization)
/// and every dispatcher operation - layout, rendering, input, user callbacks - must observe that single,
/// live thread culture. With the built-in flow an operation instead runs under whatever culture happened to
/// be current when it was <em>queued</em> (captured into its execution context), and any culture change the
/// operation makes is silently discarded when the context is restored afterwards. The net effect is that a
/// culture the user sets on the UI thread gets reverted by the next queued operation - see
/// https://github.com/AvaloniaUI/Avalonia/issues/21451.
/// </para>
/// <para>
/// This type restores the pre-4.6 semantics, which are the only ones that make sense for a UI application:
/// culture is plain ambient state on the UI thread. It still runs the callback under the captured execution
/// context, so impersonation and every other <see cref="AsyncLocal{T}"/> continue to flow normally; only the
/// culture is special-cased. It:
/// </para>
/// <list type="number">
///   <item>snapshots the executing (UI) thread's live culture immediately before running;</item>
///   <item>re-applies that live culture <em>inside</em> the context, overriding whatever culture the captured
///   context flows in, so the callback always runs under the current UI-thread culture; and</item>
///   <item>reads the culture back after the callback and writes it onto the thread once the context has been
///   restored, so any culture change the callback made persists instead of being thrown away.</item>
/// </list>
/// <para>
/// This is a port of WPF's <c>CulturePreservingExecutionContext</c>, which exists for exactly the same reason.
/// </para>
/// </remarks>
internal sealed class CulturePreservingExecutionContext
{
    private readonly ExecutionContext _context;
    private CultureAndContext? _cultureAndContext;

    private static readonly ContextCallback s_callbackWrapperDelegate = CallbackWrapper;

    private CulturePreservingExecutionContext(ExecutionContext context)
    {
        _context = context;
    }

    public static CulturePreservingExecutionContext? Capture()
    {
        // Match ExecutionContext.Capture(): when the flow is suppressed it returns null and there is
        // nothing to restore later, so we behave the same way.
        if (ExecutionContext.IsFlowSuppressed())
            return null;

        var context = ExecutionContext.Capture();
        if (context is null)
            return null;

        return new CulturePreservingExecutionContext(context);
    }

    public static void Run(CulturePreservingExecutionContext executionContext, ContextCallback callback, object? state)
    {
        if (executionContext is null)
            throw new InvalidOperationException("Cannot run on a null CulturePreservingExecutionContext.");

        // Snapshot the live culture of the thread that is about to run the callback (the dispatcher thread).
        // We need it to override the culture that ExecutionContext.Run flows in from the captured context.
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
            // ExecutionContext.Run has reverted the context, discarding any culture change made inside it.
            // Re-apply the culture we read back after the callback so the change persists on the thread.
            executionContext._cultureAndContext.WriteCultureInfosToCurrentThread();
        }
    }

    private static void CallbackWrapper(object? obj)
    {
        var cultureAndContext = (CultureAndContext)obj!;

        // Override the culture flowed in by the captured context with the live UI-thread culture, run the
        // callback, then capture whatever culture the callback leaves behind.
        cultureAndContext.WriteCultureInfosToCurrentThread();
        cultureAndContext.Callback(cultureAndContext.State);
        cultureAndContext.ReadCultureInfosFromCurrentThread();
    }

    private sealed class CultureAndContext
    {
        private CultureInfo _culture;
        private CultureInfo _uiCulture;

        public ContextCallback Callback { get; }
        public object? State { get; }

        private CultureAndContext(ContextCallback callback, object? state)
        {
            Callback = callback;
            State = state;
            _culture = Thread.CurrentThread.CurrentCulture;
            _uiCulture = Thread.CurrentThread.CurrentUICulture;
        }

        public static CultureAndContext Initialize(ContextCallback callback, object? state)
            => new(callback, state);

        public void ReadCultureInfosFromCurrentThread()
        {
            _culture = Thread.CurrentThread.CurrentCulture;
            _uiCulture = Thread.CurrentThread.CurrentUICulture;
        }

        public void WriteCultureInfosToCurrentThread()
        {
            Thread.CurrentThread.CurrentCulture = _culture;
            Thread.CurrentThread.CurrentUICulture = _uiCulture;
        }
    }
}
