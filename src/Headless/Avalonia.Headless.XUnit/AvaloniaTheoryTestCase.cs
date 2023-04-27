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
    
    public override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
    {
        var session = HeadlessUnitTestSession.GetOrStartForAssembly(Method.ToRuntimeMethod().DeclaringType?.Assembly);

        var task = session.Dispatcher.InvokeAsync<Task<RunSummary>>(async () =>
        {
            var runner = new XunitTestCaseRunner(this, DisplayName, SkipReason, constructorArguments,
                TestMethodArguments, messageBus, aggregator, cancellationTokenSource);
            return await runner.RunAsync();
        }, default, cancellationTokenSource.Token).GetTask().Unwrap();

        return await task;
    }
}
