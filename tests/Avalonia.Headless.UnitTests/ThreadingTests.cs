using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Headless.XUnit.Tests;

public class ThreadingTests
{
    [Fact]
    public void Should_Be_On_Dispatcher_Thread()
    {
        Dispatcher.UIThread.VerifyAccess();
    }
    
    [Fact]
    public async Task DispatcherTimer_Works_On_The_Same_Thread()
    {
        var currentThread = Thread.CurrentThread;
        var tcs = new TaskCompletionSource();

        DispatcherTimer.RunOnce(() =>
        {
            Assert.Equal(currentThread, Thread.CurrentThread);

            tcs.SetResult();
        }, TimeSpan.FromTicks(1));

        await tcs.Task;
    }
}
