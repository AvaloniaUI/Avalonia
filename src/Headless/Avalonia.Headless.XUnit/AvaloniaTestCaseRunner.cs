using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Avalonia.Headless.XUnit;

internal class AvaloniaTestCaseRunner : XunitTestCaseRunner
{
    private readonly HeadlessUnitTestSession _session;
    private readonly Action? _onAfterTestInvoked;

    public AvaloniaTestCaseRunner(
        HeadlessUnitTestSession session, Action? onAfterTestInvoked,
        IXunitTestCase testCase, string displayName, string skipReason, object[] constructorArguments,
        object[] testMethodArguments, IMessageBus messageBus, ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource) : base(testCase, displayName, skipReason, constructorArguments,
        testMethodArguments, messageBus, aggregator, cancellationTokenSource)
    {
        _session = session;
        _onAfterTestInvoked = onAfterTestInvoked;
    }

    public static Task<RunSummary> RunTest(HeadlessUnitTestSession session,
        IXunitTestCase testCase, string displayName, string skipReason, object[] constructorArguments,
        object[] testMethodArguments, IMessageBus messageBus, ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        var afterTest = () => Dispatcher.UIThread.RunJobs();

        var runner = new AvaloniaTestCaseRunner(session, afterTest, testCase, displayName,
            skipReason, constructorArguments, testMethodArguments, messageBus, aggregator, cancellationTokenSource);
        return runner.RunAsync();
    }

    protected override XunitTestRunner CreateTestRunner(ITest test, IMessageBus messageBus, Type testClass,
        object[] constructorArguments,
        MethodInfo testMethod, object[] testMethodArguments, string skipReason,
        IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
        ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
    {
        return new AvaloniaTestRunner(_session, _onAfterTestInvoked, test, messageBus, testClass, constructorArguments,
            testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource);
    }

    private class AvaloniaTestRunner : XunitTestRunner
    {
        private readonly HeadlessUnitTestSession _session;
        private readonly Action? _onAfterTestInvoked;

        public AvaloniaTestRunner(
            HeadlessUnitTestSession session, Action? onAfterTestInvoked,
            ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod,
            object[] testMethodArguments, string skipReason,
            IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource) : base(test, messageBus, testClass, constructorArguments,
            testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
            _session = session;
            _onAfterTestInvoked = onAfterTestInvoked;
        }

        protected override Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
        {
            return _session.Dispatch(
                () => new AvaloniaTestInvoker(_onAfterTestInvoked, Test, MessageBus, TestClass,
                    ConstructorArguments, TestMethod, TestMethodArguments, BeforeAfterAttributes, aggregator,
                    CancellationTokenSource).RunAsync(),
                CancellationTokenSource.Token);
        }
    }

    private class AvaloniaTestInvoker : XunitTestInvoker
    {
        private readonly Action? _onAfterTestInvoked;

        public AvaloniaTestInvoker(
            Action? onAfterTestInvoked,
            ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod,
            object[] testMethodArguments, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
            ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource) : base(test, messageBus,
            testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator,
            cancellationTokenSource)
        {
            _onAfterTestInvoked = onAfterTestInvoked;
        }

        protected override async Task AfterTestMethodInvokedAsync()
        {
            await base.AfterTestMethodInvokedAsync();

            // Only here we can execute random code after the test, where exception will be properly handled by the XUnit.
            if (_onAfterTestInvoked is not null)
            {
                Aggregator.Run(_onAfterTestInvoked);
            }
        }
    }
}
