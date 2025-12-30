using System;
using System.Reflection;
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
}
