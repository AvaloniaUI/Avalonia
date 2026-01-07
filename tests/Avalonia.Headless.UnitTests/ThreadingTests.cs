using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Avalonia.Headless.UnitTests;

public class ThreadingTests
{
#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Should_Be_On_Dispatcher_Thread()
    {
        ValidateTestContext();
        Dispatcher.UIThread.VerifyAccess();
    }

#if NUNIT
    [AvaloniaTest, Ignore("This test should always fail, enable to test if it fails")]
#elif XUNIT
    [AvaloniaFact(Skip = "This test should always fail, enable to test if it fails")]
#endif
    public void Should_Fail_Test_On_Delayed_Post_When_FlushDispatcher()
    {
        Dispatcher.UIThread.Post(() => throw new InvalidOperationException(), DispatcherPriority.Default);
    }
    
#if NUNIT
    [AvaloniaTheory, TestCase(1), TestCase(10), TestCase(100)]
#elif XUNIT
    [AvaloniaTheory, InlineData(1), InlineData(10), InlineData(100)]
#endif
    public async Task DispatcherTimer_Works_On_The_Same_Thread(int interval)
    {
        AssertHelper.NotNull(SynchronizationContext.Current);
        ValidateTestContext();
        var currentThread = Thread.CurrentThread;

        await Task.Delay(100);

        ValidateTestContext();
        AssertHelper.Same(currentThread, Thread.CurrentThread);

        var tcs = new TaskCompletionSource();

        DispatcherTimer.RunOnce(() =>
        {
            try
            {
                ValidateTestContext();
                AssertHelper.Same(currentThread, Thread.CurrentThread);
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }, TimeSpan.FromTicks(interval));

        await tcs.Task; 
    }

    private void ValidateTestContext([CallerMemberName] string runningMethodName = null)
    {
#if NUNIT
        var testName = TestContext.CurrentContext.Test.Name;
        // Test.Name also includes parameters.
        AssertHelper.Equal(testName.Split('(').First(), runningMethodName);
#endif
    }
}
