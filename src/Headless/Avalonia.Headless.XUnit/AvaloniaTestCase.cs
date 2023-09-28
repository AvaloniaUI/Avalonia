using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Avalonia.Headless.XUnit;

internal class AvaloniaTestCase : XunitTestCase
{
    public AvaloniaTestCase(
        IMessageSink diagnosticMessageSink,
        TestMethodDisplay defaultMethodDisplay,
        ITestMethod testMethod,
        object?[]? testMethodArguments = null)
        : base(diagnosticMessageSink, defaultMethodDisplay, TestMethodDisplayOptions.None, testMethod, testMethodArguments)
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public AvaloniaTestCase()
    {
    }

    public override Task<RunSummary> RunAsync(
        IMessageSink diagnosticMessageSink,
        IMessageBus messageBus,
        object[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        var session = HeadlessUnitTestSession.GetOrStartForAssembly(Method.ToRuntimeMethod().DeclaringType?.Assembly);

        // We need to block the XUnit thread to ensure its concurrency throttle is effective.
        // See https://github.com/AArnott/Xunit.StaFact/pull/55#issuecomment-826187354 for details.
        var runSummary =
            Task.Run(() => AvaloniaTestCaseRunner.RunTest(session, this, DisplayName, SkipReason, constructorArguments,
                TestMethodArguments, messageBus, aggregator, cancellationTokenSource))
            .GetAwaiter()
            .GetResult();

        return Task.FromResult(runSummary);
    }
}
