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
    private readonly Action? _onAfterTestInvoked;

    public AvaloniaTestCaseRunner(
        Action? onAfterTestInvoked,
        IXunitTestCase testCase, string displayName, string skipReason, object[] constructorArguments,
        object[] testMethodArguments, IMessageBus messageBus, ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource) : base(testCase, displayName, skipReason, constructorArguments,
        testMethodArguments, messageBus, aggregator, cancellationTokenSource)
    {
        _onAfterTestInvoked = onAfterTestInvoked;
    }

    public static Task<RunSummary> RunTest(HeadlessUnitTestSession session,
        IXunitTestCase testCase, string displayName, string skipReason, object[] constructorArguments,
        object[] testMethodArguments, IMessageBus messageBus, ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        var afterTest = () => Dispatcher.UIThread.RunJobs();
        return session.Dispatch(async () =>
        {
            var runner = new AvaloniaTestCaseRunner(afterTest, testCase, displayName,
                skipReason, constructorArguments, testMethodArguments, messageBus, aggregator, cancellationTokenSource);
            return await runner.RunAsync();
        }, cancellationTokenSource.Token);
    }
    
    protected override XunitTestRunner CreateTestRunner(ITest test, IMessageBus messageBus, Type testClass,
        object[] constructorArguments,
        MethodInfo testMethod, object[] testMethodArguments, string skipReason,
        IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
        ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
    {
        return new AvaloniaTestRunner(_onAfterTestInvoked, test, messageBus, testClass, constructorArguments,
            testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource);
    }

    private class AvaloniaTestRunner : XunitTestRunner
    {
        private readonly Action? _onAfterTestInvoked;

        public AvaloniaTestRunner(
            Action? onAfterTestInvoked,
            ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod,
            object[] testMethodArguments, string skipReason,
            IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource) : base(test, messageBus, testClass, constructorArguments,
            testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
            _onAfterTestInvoked = onAfterTestInvoked;
        }

        protected override Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
        {
            return new AvaloniaTestInvoker(_onAfterTestInvoked, Test, MessageBus, TestClass, ConstructorArguments,
                TestMethod, TestMethodArguments, BeforeAfterAttributes, aggregator, CancellationTokenSource).RunAsync();
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
