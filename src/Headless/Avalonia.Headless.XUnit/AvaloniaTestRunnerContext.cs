using System.Collections.Generic;
using System.Threading;
using Xunit.Sdk;
using Xunit.v3;

namespace Avalonia.Headless.XUnit;

internal sealed class AvaloniaTestRunnerContext(
    IXunitTest test,
    IMessageBus messageBus,
    ExplicitOption explicitOption,
    ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource,
    IReadOnlyCollection<IBeforeAfterTestAttribute> beforeAfterTestAttributes,
    object?[] constructorArguments,
    HeadlessUnitTestSession session)
    : XunitTestRunnerContext(
        test,
        messageBus,
        explicitOption,
        aggregator,
        cancellationTokenSource,
        beforeAfterTestAttributes,
        constructorArguments)
{
    public HeadlessUnitTestSession Session { get; } = session;
}
