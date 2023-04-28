using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Avalonia.Headless.XUnit;

internal class AvaloniaTestAssemblyRunner : XunitTestAssemblyRunner
{
    private HeadlessUnitTestSession? _session;

    public AvaloniaTestAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases,
        IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions) : base(testAssembly, testCases, diagnosticMessageSink,
        executionMessageSink, executionOptions)
    {
    }

    protected override void SetupSyncContext(int maxParallelThreads)
    {
        _session = HeadlessUnitTestSession.GetOrStartForAssembly(
            Assembly.Load(new AssemblyName(TestAssembly.Assembly.Name)));
        base.SetupSyncContext(1);
    }

    public override void Dispose()
    {
        _session?.Dispose();
        base.Dispose();
    }

    protected override Task<RunSummary> RunTestCollectionAsync(
        IMessageBus messageBus,
        ITestCollection testCollection,
        IEnumerable<IXunitTestCase> testCases,
        CancellationTokenSource cancellationTokenSource)
    {
        return new AvaloniaTestCollectionRunner(_session!, testCollection, testCases, DiagnosticMessageSink, messageBus,
            TestCaseOrderer, new ExceptionAggregator(Aggregator), cancellationTokenSource).RunAsync();
    }

    private class AvaloniaTestCollectionRunner : XunitTestCollectionRunner
    {
        private readonly HeadlessUnitTestSession _session;

        public AvaloniaTestCollectionRunner(HeadlessUnitTestSession session,
            ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases,
            IMessageSink diagnosticMessageSink, IMessageBus messageBus, ITestCaseOrderer testCaseOrderer,
            ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource) : base(testCollection,
            testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
        {
            _session = session;
        }

        protected override Task<RunSummary> RunTestClassAsync(
            ITestClass testClass,
            IReflectionTypeInfo @class,
            IEnumerable<IXunitTestCase> testCases)
        {
            return new AvaloniaTestClassRunner(_session, testClass, @class, testCases, DiagnosticMessageSink, MessageBus,
                TestCaseOrderer, new ExceptionAggregator(Aggregator), CancellationTokenSource,
                CollectionFixtureMappings).RunAsync();
        }
    }

    private class AvaloniaTestClassRunner : XunitTestClassRunner
    {
        private readonly HeadlessUnitTestSession _session;

        public AvaloniaTestClassRunner(HeadlessUnitTestSession session, ITestClass testClass,
            IReflectionTypeInfo @class,
            IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus,
            ITestCaseOrderer testCaseOrderer, ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource, IDictionary<Type, object> collectionFixtureMappings) :
            base(testClass, @class, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator,
                cancellationTokenSource, collectionFixtureMappings)
        {
            _session = session;
        }

        protected override Task<RunSummary> RunTestMethodAsync(
            ITestMethod testMethod,
            IReflectionMethodInfo method,
            IEnumerable<IXunitTestCase> testCases,
            object[] constructorArguments)
        {
            return new AvaloniaTestMethodRunner(_session, testMethod, Class, method, testCases, DiagnosticMessageSink,
                MessageBus, new ExceptionAggregator(Aggregator), CancellationTokenSource,
                constructorArguments).RunAsync();
        }
    }

    private class AvaloniaTestMethodRunner : XunitTestMethodRunner
    {
        private readonly HeadlessUnitTestSession _session;
        private readonly IMessageBus _messageBus;
        private readonly ExceptionAggregator _aggregator;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly object[] _constructorArguments;

        public AvaloniaTestMethodRunner(HeadlessUnitTestSession session, ITestMethod testMethod,
            IReflectionTypeInfo @class,
            IReflectionMethodInfo method, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink,
            IMessageBus messageBus, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource,
            object[] constructorArguments) : base(testMethod, @class, method, testCases, diagnosticMessageSink,
            messageBus, aggregator, cancellationTokenSource, constructorArguments)
        {
            _session = session;
            _messageBus = messageBus;
            _aggregator = aggregator;
            _cancellationTokenSource = cancellationTokenSource;
            _constructorArguments = constructorArguments;
        }

        protected override Task<RunSummary> RunTestCaseAsync(IXunitTestCase testCase)
        {
            return AvaloniaTestCaseRunner.RunTest(_session, testCase, testCase.DisplayName, testCase.SkipReason,
                _constructorArguments, testCase.TestMethodArguments, _messageBus, _aggregator,
                _cancellationTokenSource);
        }
    }
}
