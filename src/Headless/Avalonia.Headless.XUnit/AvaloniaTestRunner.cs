using Avalonia.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Avalonia.Headless.XUnit;

internal class AvaloniaTestRunner<TAppBuilderEntry> : XunitTestAssemblyRunner
{
    private CancellationTokenSource? _cancellationTokenSource;
    
    public AvaloniaTestRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases,
        IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions) : base(testAssembly, testCases, diagnosticMessageSink,
        executionMessageSink, executionOptions)
    {
    }

    protected override void SetupSyncContext(int maxParallelThreads)
    {
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        SynchronizationContext.SetSynchronizationContext(InitNewApplicationContext(_cancellationTokenSource.Token).Result);
    }

    public override void Dispose()
    {
        _cancellationTokenSource?.Dispose();
        base.Dispose();
    }

    internal static Task<SynchronizationContext> InitNewApplicationContext(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<SynchronizationContext>();

        new Thread(() =>
        {
            try
            {
                var appBuilder = AppBuilder.Configure(typeof(TAppBuilderEntry));

                // If windowing subsystem wasn't initialized by user, force headless with default parameters.
                if (appBuilder.WindowingSubsystemName != "Headless")
                {
                    appBuilder = appBuilder.UseHeadless(new AvaloniaHeadlessPlatformOptions());
                }
                    
                appBuilder.SetupWithoutStarting();

                tcs.SetResult(SynchronizationContext.Current!);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }

            Dispatcher.UIThread.MainLoop(cancellationToken);
        }) { IsBackground = true }.Start();

        return tcs.Task;
    }
}
