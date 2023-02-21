using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Avalonia.UnitTests;

[TestFrameworkDiscoverer("Avalonia.UnitTests.AvaloniaTestFrameworkTypeDiscoverer", "Avalonia.UnitTests")]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class AvaloniaTestFrameworkAttribute : Attribute, ITestFrameworkAttribute
{
    public AvaloniaTestFrameworkAttribute(Type avaloniaBuilderType) { }
}

public class AvaloniaTestFrameworkTypeDiscoverer : ITestFrameworkTypeDiscoverer
{
    public AvaloniaTestFrameworkTypeDiscoverer(IMessageSink _)
    {
    }

    public Type GetTestFrameworkType(IAttributeInfo attribute)
    {
        var builderType = attribute.GetConstructorArguments().First() as Type;
        return typeof(AvaloniaTestFramework<>).MakeGenericType(builderType);
    }
}

public class AvaloniaTestFramework<TAppBuilderEntry> : XunitTestFramework
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
            using (var assemblyRunner = new Runner(
                       TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink,
                       executionOptions)) await assemblyRunner.RunAsync();
        }
    }

    private class Runner : XunitTestAssemblyRunner
    {
        public Runner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases,
            IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions) : base(testAssembly, testCases, diagnosticMessageSink,
            executionMessageSink, executionOptions)
        {
        }


        protected override void SetupSyncContext(int maxParallelThreads)
        {
            var tcs = new TaskCompletionSource<SynchronizationContext>();
            new Thread(() =>
            {
                try
                {
                    var appBuilder = (AppBuilder)typeof(TAppBuilderEntry)
                        .GetMethod("BuildAvaloniaApp", BindingFlags.Static | BindingFlags.Public)?
                        .Invoke(null, new object[0])
                        ?? throw new InvalidOperationException("Invalid TAppBuilderEntry type");
                    appBuilder
                        .SetupWithoutStarting();
                    tcs.SetResult(SynchronizationContext.Current);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }

                Dispatcher.UIThread.MainLoop(CancellationToken.None);
            }) { IsBackground = true }.Start();

            SynchronizationContext.SetSynchronizationContext(tcs.Task.Result);
        }
    }
}
