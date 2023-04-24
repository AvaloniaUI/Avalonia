using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Avalonia.Headless.XUnit;

internal class AvaloniaTestFramework<TAppBuilderEntry> : XunitTestFramework
{
    public AvaloniaTestFramework(IMessageSink messageSink) : base(messageSink)
    {
    }

    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
        => new Executor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);


    private class Executor : XunitTestFrameworkExecutor
    {
        public Executor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider,
            IMessageSink diagnosticMessageSink) : base(assemblyName, sourceInformationProvider,
            diagnosticMessageSink)
        {
        }

        protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases,
            IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions)
        {
            executionOptions.SetValue("xunit.execution.DisableParallelization", false);
            using (var assemblyRunner = new AvaloniaTestRunner<TAppBuilderEntry>(
                       TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink,
                       executionOptions)) await assemblyRunner.RunAsync();
        }
    }
}
