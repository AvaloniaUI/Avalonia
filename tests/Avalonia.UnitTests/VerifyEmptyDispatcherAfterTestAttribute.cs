using System;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Threading;
using Xunit;
using Xunit.v3;

namespace Avalonia.UnitTests;

public sealed class VerifyEmptyDispatcherAfterTestAttribute : BeforeAfterTestAttribute
{
    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (typeof(ScopedTestBase).IsAssignableFrom(methodUnderTest.DeclaringType))
            return;

        // Dispatcher.UIThread would create a dispatcher when none exists.
        var dispatcher = Dispatcher.TryGetUIThread();
        if (dispatcher is null)
            return;

        var jobs = dispatcher.GetJobs();
        dispatcher.ClearJobs();

        // A UI thread dispatcher stays bound to the thread that created it. Tests run one at a
        // time but not on one thread, so a leftover one denies access to the next test that runs
        // elsewhere. Release it so only this test fails.
        Dispatcher.ResetForUnitTests();

        // Ignore the Control.Loaded callback. It might happen synchronously or might be posted.
        if (jobs.Count == 1 && IsLoadedCallback(jobs[0]))
            jobs.Clear();

        var leftoverJobs = jobs.Count == 0
            ? string.Empty
            : $" with {jobs.Count} unprocessed {(jobs.Count == 1 ? "job" : "jobs")}:\n" +
              string.Join(Environment.NewLine, jobs.Select(job => $"  - {job.DebugDisplay}"));

        Assert.Fail(
            $"The test left a UI thread dispatcher behind{leftoverJobs}\n" +
            $"Use ScopedTestBase or UnitTestApplication.Start().");

        static bool IsLoadedCallback(DispatcherOperation job)
            => job.Priority == DispatcherPriority.Loaded &&
               (job.Callback as Delegate)?.Method.DeclaringType?.DeclaringType == typeof(Control);
    }
}
