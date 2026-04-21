using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Headless;
using Avalonia.Threading;

namespace Avalonia.Headless.UnitTests;

public class IsolationTests
{
    private static WeakReference<Application> s_previousAppRef;
    private static WeakReference<Dispatcher> s_previousDispatcherRef;

#if NUNIT
    [AvaloniaTheory]
    [TestCase(1), TestCase(2), TestCase(3)]
#elif XUNIT
    [AvaloniaTheory]
    [InlineData(1), InlineData(2), InlineData(3)]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "Usage",
        "xUnit1026:Theory methods should use all of their parameters",
        Justification = "Used to run the test several times with the proper isolation level")]
#endif
    public void Application_Instance_Should_Match_Isolation_Level(int runIndex)
    {
        var currentApp = Application.Current;
        var currentDispatcher = Dispatcher.UIThread;

        if (s_previousAppRef is not null && s_previousDispatcherRef is not null)
        {
            var isolationLevel =
                GetType().Assembly.GetCustomAttribute<AvaloniaTestIsolationAttribute>()?.IsolationLevel ??
                AvaloniaTestIsolationLevel.PerTest;

            if (isolationLevel == AvaloniaTestIsolationLevel.PerTest)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                AssertHelper.False(s_previousAppRef.TryGetTarget(out var previousApp),
                    "Previous Application instance should have been collected.");
                AssertHelper.False(s_previousDispatcherRef.TryGetTarget(out var previousDispatcher),
                    "Previous Dispatcher instance should have been collected.");

                AssertHelper.False(previousApp == currentApp);
                AssertHelper.False(previousDispatcher == currentDispatcher);
            }
            else if (isolationLevel == AvaloniaTestIsolationLevel.PerAssembly)
            {
                AssertHelper.True(s_previousAppRef.TryGetTarget(out var previousApp),
                    "Previous Application instance should still be alive.");
                AssertHelper.True(s_previousDispatcherRef.TryGetTarget(out var previousDispatcher),
                    "Previous Dispatcher instance should still be alive.");

                AssertHelper.True(previousApp == currentApp);
                AssertHelper.True(previousDispatcher == currentDispatcher);
            }
            else
            {
                throw new InvalidOperationException($"Unknown isolation level: {isolationLevel}");
            }
        }

        s_previousAppRef = new WeakReference<Application>(currentApp);
        s_previousDispatcherRef = new WeakReference<Dispatcher>(currentDispatcher);
    }

#if NUNIT
    [Test]
#elif XUNIT
    [Fact]
#endif
    public async Task Dispatch_Cleanup_Should_Complete_Before_Task_Returns()
    {
        // Regression test for https://github.com/AvaloniaUI/Avalonia/issues/20664.
        // EnsureIsolatedApplication().Dispose() must complete (resetting s_uiThread to null)
        // before the dispatch task resolves. If Dispose ran after tcs.TrySetResult, s_uiThread
        // could still point to the headless dispatcher here, making CheckAccess() return false
        // on this non-headless thread.
        //
        // Only applies to PerTest isolation: PerAssembly uses EnsureSharedApplication which
        // intentionally keeps s_uiThread set for the lifetime of the assembly, so CheckAccess()
        // from a non-headless thread would always be false there and the race does not apply.
        var isolationLevel =
            GetType().Assembly.GetCustomAttribute<AvaloniaTestIsolationAttribute>()?.IsolationLevel
            ?? AvaloniaTestIsolationLevel.PerTest;

        AssertHelper.SkipWhen(isolationLevel != AvaloniaTestIsolationLevel.PerTest, "Only applies to PerTest isolation.");

        // Uses the shared assembly session (not StartNew) so no competing thread calls
        // ResetGlobalState() concurrently with other tests in the suite.
        var session = HeadlessUnitTestSession.GetOrStartForAssembly(GetType().Assembly);

        await session.Dispatch(() => { }, CancellationToken.None);

        AssertHelper.True(Dispatcher.UIThread.CheckAccess());
    }
}
