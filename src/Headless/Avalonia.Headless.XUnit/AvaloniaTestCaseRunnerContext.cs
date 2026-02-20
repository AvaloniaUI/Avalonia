using System.Collections.Generic;
using System.Threading;
using Xunit.Sdk;
using Xunit.v3;

namespace Avalonia.Headless.XUnit;

internal sealed class AvaloniaTestCaseRunnerContext(
    IXunitTestCase testCase,
    IReadOnlyCollection<IXunitTest> tests,
    IMessageBus messageBus,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource,
    string displayName,
    string? skipReason,
    ExplicitOption explicitOption,
    object?[] constructorArguments,
    HeadlessUnitTestSession session)
    : XunitTestCaseRunnerContext(
        testCase,
        tests,
        messageBus,
        aggregator,
        cancellationTokenSource,
        displayName,
        skipReason,
        explicitOption,
        constructorArguments)
{
    public HeadlessUnitTestSession Session { get; } = session;
}
