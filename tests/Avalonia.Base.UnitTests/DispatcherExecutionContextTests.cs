using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

// This file is compiled into BOTH:
//   * Avalonia.Base.UnitTests        - against Avalonia.Threading
//   * Avalonia.UnitTests.WpfCompare  - against System.Windows.Threading (no Avalonia reference)
// so that the exact same test bodies run against Avalonia's dispatcher and WPF's dispatcher,
// proving that Avalonia's culture / ExecutionContext behaviour matches WPF. See issue #21451.
//
// The only per-framework differences are hidden behind two small helpers that each project
// provides in its own namespace:
//   * CrossFactAttribute     - [Fact] on Avalonia, [StaFact] on WPF (WPF dispatcher needs STA).
//   * DispatcherTestServices - DrainQueue(), which differs only in static vs instance PushFrame.
// Everything else (Dispatcher.CurrentDispatcher, InvokeAsync, DispatcherPriority, DispatcherFrame,
// CultureInfo, Thread, AsyncLocal) is API-identical between the two frameworks.

#if WPFCOMPARE
using System.Windows.Threading;

namespace Avalonia.UnitTests.WpfCompare;

public class DispatcherExecutionContextTests
#else
using Avalonia.Threading;

namespace Avalonia.Base.UnitTests;

public class DispatcherExecutionContextTests : global::Avalonia.UnitTests.ScopedTestBase
#endif
{
    // Sumerian: extremely unlikely to be the machine default, so these tests don't depend on the environment.
    private const string CustomCultureName = "sux-Shaw-UM";

    // A second, distinct culture used as the "calling thread" culture in the cross-thread test.
    private const string CallSiteCultureName = "kk-KZ";

    // Regression test for https://github.com/AvaloniaUI/Avalonia/issues/21451. A dispatcher operation must
    // run under the UI thread's live culture - even one queued *before* the culture was set, whose execution
    // context captured the old culture - and running operations must not reset the UI-thread culture.
    [CrossFact]
    public void Dispatcher_Operation_Runs_Under_Live_UI_Thread_Culture_And_Does_Not_Reset_It()
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        var custom = CultureInfo.GetCultureInfo(CustomCultureName);
        var oldCulture = Thread.CurrentThread.CurrentUICulture;
        try
        {
            string? earlyOpSaw = null;
            string? lateOpSaw = null;

            // Queued BEFORE the culture is set: its execution context captures the current (default) culture.
            dispatcher.InvokeAsync(() => earlyOpSaw = Thread.CurrentThread.CurrentUICulture.Name, DispatcherPriority.Normal);

            // The application sets a custom UI culture on the UI thread.
            Thread.CurrentThread.CurrentUICulture = custom;

            dispatcher.InvokeAsync(() => lateOpSaw = Thread.CurrentThread.CurrentUICulture.Name, DispatcherPriority.Normal);

            DispatcherTestServices.DrainQueue(dispatcher, DispatcherPriority.Background);

            // The early operation runs under the LIVE UI-thread culture, not the default it captured...
            Assert.Equal(CustomCultureName, earlyOpSaw);
            Assert.Equal(CustomCultureName, lateOpSaw);
            // ...and running operations must not have reset the UI-thread culture.
            Assert.Equal(CustomCultureName, Thread.CurrentThread.CurrentUICulture.Name);
        }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = oldCulture;
            Assert.NotEqual(CustomCultureName, oldCulture.Name);
        }
    }

    // A culture change made *inside* an operation persists on the UI thread (the outbound half of the
    // pre-4.6 semantics). With the runtime's default ExecutionContext flow the change would be discarded.
    [CrossFact]
    public void Culture_Set_Inside_Dispatcher_Operation_Persists_To_UI_Thread()
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        var custom = CultureInfo.GetCultureInfo(CustomCultureName);
        var oldCulture = Thread.CurrentThread.CurrentUICulture;
        try
        {
            string? nextOpSaw = null;

            dispatcher.InvokeAsync(() => Thread.CurrentThread.CurrentUICulture = custom, DispatcherPriority.Normal);
            dispatcher.InvokeAsync(() => nextOpSaw = Thread.CurrentThread.CurrentUICulture.Name, DispatcherPriority.Normal);

            DispatcherTestServices.DrainQueue(dispatcher, DispatcherPriority.Background);

            // The change made by the first operation is visible to the next one and stays on the thread.
            Assert.Equal(CustomCultureName, nextOpSaw);
            Assert.Equal(CustomCultureName, Thread.CurrentThread.CurrentUICulture.Name);
        }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = oldCulture;
            Assert.NotEqual(CustomCultureName, oldCulture.Name);
        }
    }

    // Non-culture ExecutionContext state (an AsyncLocal) DOES flow into an operation from the context that
    // was captured when it was queued - this is preserved, unlike culture which is special-cased.
    [CrossFact]
    public void ExecutionContext_Flows_Into_Dispatcher_Operation()
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        var asyncLocal = new AsyncLocal<string?>();
        string? seen = null;

        asyncLocal.Value = "captured";
        dispatcher.InvokeAsync(() => seen = asyncLocal.Value, DispatcherPriority.Normal);
        // Change the ambient value AFTER the operation captured its context.
        asyncLocal.Value = "ambient";

        DispatcherTestServices.DrainQueue(dispatcher, DispatcherPriority.Background);

        // The operation observes the value captured at post time, not the later ambient value.
        Assert.Equal("captured", seen);
    }

    // An AsyncLocal set inside one operation must not leak into the next one (each operation restores its
    // own captured context).
    [CrossFact]
    public void ExecutionContext_Does_Not_Flow_Between_Dispatcher_Operations()
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        var asyncLocal = new AsyncLocal<string?>();
        string? seen = "unset";

        dispatcher.InvokeAsync(() => asyncLocal.Value = "set-by-first-op", DispatcherPriority.Normal);
        dispatcher.InvokeAsync(() => seen = asyncLocal.Value, DispatcherPriority.Normal);

        DispatcherTestServices.DrainQueue(dispatcher, DispatcherPriority.Background);

        Assert.Null(seen);
    }

    // A dispatcher operation runs under the UI thread's live culture, NOT under the culture of the thread
    // that happened to queue it - even though that other thread's culture would flow into a plain Task.Run
    // continuation. This is the cross-thread counterpart of the same-thread test above.
    [CrossFact]
    [SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method", Justification = "Tests the dispatcher itself")]
    public void Dispatcher_Operations_Use_Live_UI_Thread_Culture_Not_Calling_Thread_Culture()
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        var uiThreadCulture = CultureInfo.GetCultureInfo(CustomCultureName);
        var callSiteCulture = CultureInfo.GetCultureInfo(CallSiteCultureName);
        var oldCulture = Thread.CurrentThread.CurrentCulture;

        // This (test) thread pumps the frame below, i.e. it is the UI thread. Give it a known culture.
        Thread.CurrentThread.CurrentCulture = uiThreadCulture;
        try
        {
            var frame = new DispatcherFrame();
            string? taskRunSaw = null;
            string? invokeSaw = null;
            string? invokeAsyncSaw = null;

            var callingThread = new Thread(() =>
            {
                // A DIFFERENT culture on this non-UI (calling) thread.
                Thread.CurrentThread.CurrentCulture = callSiteCulture;

                // Baseline: a plain Task.Run continuation DOES flow the calling-thread culture through the EC.
                taskRunSaw = Task.Run(() => Thread.CurrentThread.CurrentCulture.Name).GetAwaiter().GetResult();

                // Queue work onto the dispatcher from this non-UI thread, then stop the frame.
                dispatcher.Invoke(() => invokeSaw = Thread.CurrentThread.CurrentCulture.Name);
                dispatcher.InvokeAsync(() => invokeAsyncSaw = Thread.CurrentThread.CurrentCulture.Name, DispatcherPriority.Normal);
                dispatcher.InvokeAsync(() => frame.Continue = false, DispatcherPriority.Normal);
            });
            callingThread.Start();

            DispatcherTestServices.PushFrame(dispatcher, frame);
            callingThread.Join();

            // Baseline: Task.Run flows the calling-thread culture (guaranteed by the runtime).
            Assert.Equal(CallSiteCultureName, taskRunSaw);
            // Dispatcher operations run under the UI thread's live culture, not the calling thread's culture.
            Assert.Equal(CustomCultureName, invokeSaw);
            Assert.Equal(CustomCultureName, invokeAsyncSaw);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = oldCulture;
            Assert.NotEqual(CustomCultureName, oldCulture.Name);
        }
    }
}
