using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Avalonia.Headless.UnitTests;

public class ThreadingTests
{
    [AvaloniaFact]
    public void Should_Be_On_Dispatcher_Thread()
    {
        Dispatcher.UIThread.VerifyAccess();
    }
    
    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task DispatcherTimer_Works_On_The_Same_Thread(int interval)
    {
        await Task.Delay(100);

        var currentThread = Thread.CurrentThread;
        var tcs = new TaskCompletionSource();
        var hasCompleted = false;

        DispatcherTimer.RunOnce(() =>
        {
            hasCompleted = currentThread == Thread.CurrentThread;

            tcs.SetResult();
        }, TimeSpan.FromTicks(interval));

        await tcs.Task; 
        Assert.True(hasCompleted);
    }
}
