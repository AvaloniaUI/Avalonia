using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Avalonia.Headless.XUnit;

internal class AvaloniaTheoryTestCase : XunitTheoryTestCase
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public AvaloniaTheoryTestCase()
    {
    }

    public AvaloniaTheoryTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, TestMethodDisplayOptions defaultMethodDisplayOptions, ITestMethod testMethod)
        : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod)
    {
    }
    
    public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
    {
        var session = HeadlessUnitTestSession.GetOrStartForAssembly(Method.ToRuntimeMethod().DeclaringType?.Assembly);

        return AvaloniaTestCaseRunner
            .RunTest(session, this, DisplayName, SkipReason, constructorArguments,
                TestMethodArguments, messageBus, aggregator, cancellationTokenSource);
    }
}
