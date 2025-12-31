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

        var dispatcher = Dispatcher.UIThread;
        var jobs = dispatcher.GetJobs();
        if (jobs.Count == 0)
            return;

        dispatcher.ClearJobs();

        // Ignore the Control.Loaded callback. It might happen synchronously or might be posted.
        if (jobs.Count == 1 && IsLoadedCallback(jobs[0]))
            return;

        Assert.Fail(
            $"The test left {jobs.Count} unprocessed dispatcher {(jobs.Count == 1 ? "job" : "jobs")}:\n" +
            $"{string.Join(Environment.NewLine, jobs.Select(job => $"  - {job.DebugDisplay}"))}\n" +
            $"Consider using ScopedTestBase or UnitTestApplication.Start().");

        static bool IsLoadedCallback(DispatcherOperation job)
            => job.Priority == DispatcherPriority.Loaded &&
               (job.Callback as Delegate)?.Method.DeclaringType?.DeclaringType == typeof(Control);
    }
}
