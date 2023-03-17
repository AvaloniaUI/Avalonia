using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using Xunit;
namespace Avalonia.Base.UnitTests;

public class DispatcherTests
{
    class SimpleDispatcherImpl : IDispatcherImpl, IDispatcherClock, IDispatcherImplWithPendingInput
    {
        public bool CurrentThreadIsLoopThread => true;

        public void Signal() => AskedForSignal = true;

        public event Action Signaled;
        public event Action Timer;
        public int? NextTimer { get; private set; }
        public bool AskedForSignal { get; private set; }
        
        public void UpdateTimer(int? dueTimeInTicks)
        {
            NextTimer = dueTimeInTicks;
        }

        public int TickCount { get; set; }

        public void ExecuteSignal()
        {
            if (!AskedForSignal)
                return;
            AskedForSignal = false;
            Signaled?.Invoke();
        }

        public void ExecuteTimer()
        {
            if (NextTimer == null)
                return;
            TickCount = NextTimer.Value;
            Timer?.Invoke();
        }

        public bool CanQueryPendingInput => TestInputPending != null;
        public bool HasPendingInput => TestInputPending == true;
        public bool? TestInputPending { get; set; }
    }
    
    
    [Fact]
    public void DispatcherExecutesJobsAccordingToPriority()
    {
        var impl = new SimpleDispatcherImpl();
        var disp = new Dispatcher(impl, impl);
        var actions = new List<string>();
        disp.Post(()=>actions.Add("Background"), DispatcherPriority.Background);
        disp.Post(()=>actions.Add("Render"), DispatcherPriority.Render);
        disp.Post(()=>actions.Add("Input"), DispatcherPriority.Input);
        Assert.True(impl.AskedForSignal);
        impl.ExecuteSignal();
        Assert.Equal(new[] { "Render", "Input", "Background" }, actions);
    }
    
    [Fact]
    public void DispatcherPreservesOrderWhenChangingPriority()
    {
        var impl = new SimpleDispatcherImpl();
        var disp = new Dispatcher(impl, impl);
        var actions = new List<string>();
        var toPromote = disp.InvokeAsync(()=>actions.Add("PromotedRender"), DispatcherPriority.Background);
        var toPromote2 = disp.InvokeAsync(()=>actions.Add("PromotedRender2"), DispatcherPriority.Input);
        disp.Post(() => actions.Add("Render"), DispatcherPriority.Render);
        
        toPromote.Priority = DispatcherPriority.Render;
        toPromote2.Priority = DispatcherPriority.Render;
        
        Assert.True(impl.AskedForSignal);
        impl.ExecuteSignal();
        
        Assert.Equal(new[] { "PromotedRender", "PromotedRender2", "Render" }, actions);
    }

    [Fact]
    public void DispatcherStopsItemProcessingWhenInteractivityDeadlineIsReached()
    {
        var impl = new SimpleDispatcherImpl();
        var disp = new Dispatcher(impl, impl);
        var actions = new List<int>();
        for (var c = 0; c < 10; c++)
        {
            var itemId = c;
            disp.Post(() =>
            {
                actions.Add(itemId);
                impl.TickCount += 20;
            }, DispatcherPriority.Background);
        }

        Assert.False(impl.AskedForSignal);
        Assert.NotNull(impl.NextTimer);

        impl.ExecuteTimer();
        Assert.True(impl.AskedForSignal);
        Assert.Null(impl.NextTimer);

        for (var c = 0; c < 4; c++)
        {
            if (impl.NextTimer != null)
                impl.ExecuteTimer();
            Assert.True(impl.AskedForSignal);
            impl.ExecuteSignal();
            var expectedCount = (c + 1) * 3;
            if (c == 3)
                expectedCount = 10;
            
            Assert.Equal(Enumerable.Range(0, expectedCount), actions);
            Assert.False(impl.AskedForSignal);
            if (c < 3)
            {
                Assert.True(impl.NextTimer > impl.TickCount);
            }
            else
                Assert.Null(impl.NextTimer);
        }
    }
    
    
    [Fact]
    public void DispatcherStopsItemProcessingWhenInputIsPending()
    {
        var impl = new SimpleDispatcherImpl();
        impl.TestInputPending = false;
        var disp = new Dispatcher(impl, impl);
        var actions = new List<int>();
        for (var c = 0; c < 10; c++)
        {
            var itemId = c;
            disp.Post(() =>
            {
                actions.Add(itemId);
                if (itemId == 0 || itemId == 3 || itemId == 7)
                    impl.TestInputPending = true;
            }, DispatcherPriority.Background);
        }
        Assert.False(impl.AskedForSignal);
        Assert.NotNull(impl.NextTimer);

        impl.ExecuteTimer();
        Assert.True(impl.AskedForSignal);
        Assert.Null(impl.NextTimer);

        for (var c = 0; c < 4; c++)
        {
            if (impl.NextTimer != null)
                impl.ExecuteTimer();
            Assert.True(impl.AskedForSignal);
            impl.ExecuteSignal();
            var expectedCount = c switch
            {
                0 => 1,
                1 => 4,
                2 => 8,
                3 => 10
            };
            
            Assert.Equal(Enumerable.Range(0, expectedCount), actions);
            Assert.False(impl.AskedForSignal);
            if (c < 3)
            {
                Assert.True(impl.NextTimer > impl.TickCount);
                impl.TickCount = impl.NextTimer.Value + 1;
            }
            else
                Assert.Null(impl.NextTimer);

            impl.TestInputPending = false;
        }
    }
    
}