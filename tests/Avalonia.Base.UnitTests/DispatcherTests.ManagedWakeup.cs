using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Base.UnitTests;

public partial class DispatcherTests
{
    private sealed class RecordingWakeupEvent : ManagedDispatcherImpl.IWakeupEvent
    {
        private readonly AutoResetEvent _event = new(false);
        public int SetCount;
        public int WaitCount;

        public void Set()
        {
            Interlocked.Increment(ref SetCount);
            _event.Set();
        }

        public void Wait(TimeSpan? timeout)
        {
            Interlocked.Increment(ref WaitCount);
            if (timeout.HasValue)
                _event.WaitOne(timeout.Value);
            else
                _event.WaitOne();
        }
    }

    [Fact]
    public void ManagedDispatcherWakesUpThroughInjectedEvent()
    {
        var wakeup = new RecordingWakeupEvent();
        using var services = new DispatcherServices(new ManagedDispatcherImpl(null, wakeup));
        var dispatcher = Dispatcher.UIThread;
        var executed = false;

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var poster = Task.Run(async () =>
        {
            await Task.Delay(100);
            dispatcher.Post(() =>
            {
                executed = true;
                dispatcher.ExitAllFrames();
            });
        }, TestContext.Current.CancellationToken);

        dispatcher.MainLoop(cts.Token);
        poster.GetAwaiter().GetResult();

        Assert.True(executed);
        Assert.True(wakeup.WaitCount > 0);
        Assert.True(wakeup.SetCount > 0);
    }

    [Fact]
    public void ManagedDispatcherRaisesHooksAroundWait()
    {
        var impl = new ManagedDispatcherImpl(null);
        using var services = new DispatcherServices(impl);
        var dispatcher = Dispatcher.UIThread;
        var actions = new List<string>();
        var externalPosted = false;
        var postedFromHook = false;

        impl.BeforeWait += () => actions.Add("BeforeWait");
        impl.AfterWakeup += () =>
        {
            actions.Add("AfterWakeup");
            // Only react to the wake-up caused by the external post, the loop may wake earlier on its own.
            if (postedFromHook || !Volatile.Read(ref externalPosted))
                return;
            postedFromHook = true;
            dispatcher.Post(() =>
            {
                actions.Add("HookOperation");
                dispatcher.ExitAllFrames();
            });
        };

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var poster = Task.Run(async () =>
        {
            await Task.Delay(100);
            Volatile.Write(ref externalPosted, true);
            dispatcher.Post(() => actions.Add("ExternalOperation"));
        }, TestContext.Current.CancellationToken);

        dispatcher.MainLoop(cts.Token);
        poster.GetAwaiter().GetResult();

        Assert.Equal("BeforeWait", actions[0]);
        var hookOperation = actions.IndexOf("HookOperation");
        Assert.True(hookOperation > 0);
        var afterWakeup = actions.LastIndexOf("AfterWakeup", hookOperation);
        Assert.True(afterWakeup > 0);
        Assert.Equal("BeforeWait", actions[afterWakeup - 1]);
        Assert.Contains("ExternalOperation", actions);
        // The operation posted from the hook runs before the loop blocks again.
        Assert.DoesNotContain("BeforeWait", actions.GetRange(afterWakeup, hookOperation - afterWakeup));
    }
}
