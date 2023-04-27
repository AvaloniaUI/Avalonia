using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Avalonia.Headless.XUnit;

internal class AvaloniaTestRunner : XunitTestAssemblyRunner
{
    private HeadlessUnitTestSession? _session;

    public AvaloniaTestRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases,
        IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions) : base(testAssembly, testCases, diagnosticMessageSink,
        executionMessageSink, executionOptions)
    {
    }

    protected override void SetupSyncContext(int maxParallelThreads)
    {
        _session = HeadlessUnitTestSession.GetOrStartForAssembly(Assembly.Load(new AssemblyName(TestAssembly.Assembly.Name)));
        SynchronizationContext.SetSynchronizationContext(_session.SynchronizationContext);
    }

    public override void Dispose()
    {
        _session?.Dispose();
        base.Dispose();
    }
}
