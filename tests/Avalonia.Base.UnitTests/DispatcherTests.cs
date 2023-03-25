using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia.Threading;
using Xunit;
namespace Avalonia.Base.UnitTests;

public class DispatcherTests
{
    class SimpleDispatcherImpl : IDispatcherImpl, IDispatcherImplWithPendingInput
    {
        private Thread _loopThread = Thread.CurrentThread;
        private object _lock = new();
        public bool CurrentThreadIsLoopThread => Thread.CurrentThread == _loopThread;
        public void Signal()
        {
            lock (_lock)
                AskedForSignal = true;
        }

        public event Action Signaled;
        public event Action Timer;
        public long? NextTimer { get; private set; }
        public bool AskedForSignal { get; private set; }
        
        public void UpdateTimer(long? dueTimeInTicks)
        {
            NextTimer = dueTimeInTicks;
        }

        public long Now { get; set; }

        public void ExecuteSignal()
        {
            lock (_lock)
            {
                if (!AskedForSignal)
                    return;
                AskedForSignal = false;
            }
            Signaled?.Invoke();
        }

        public void ExecuteTimer()
        {
            if (NextTimer == null)
                return;
            Now = NextTimer.Value;
            Timer?.Invoke();
        }

        public bool CanQueryPendingInput => TestInputPending != null;
        public bool HasPendingInput => TestInputPending == true;
        public bool? TestInputPending { get; set; }
    }

    class SimpleDispatcherWithBackgroundProcessingImpl : SimpleDispatcherImpl, IDispatcherImplWithExplicitBackgroundProcessing
    {
        public bool AskedForBackgroundProcessing { get; private set; }
        public event Action ReadyForBackgroundProcessing;
        public void RequestBackgroundProcessing()
        {
            if (!CurrentThreadIsLoopThread)
                throw new InvalidOperationException();
            AskedForBackgroundProcessing = true;
        }

        public void FireBackgroundProcessing()
        {
            if(!AskedForBackgroundProcessing)
                return;
            AskedForBackgroundProcessing = false;
            ReadyForBackgroundProcessing?.Invoke();
        }
    }
    
    class SimpleControlledDispatcherImpl : SimpleDispatcherWithBackgroundProcessingImpl, IControlledDispatcherImpl
    {
        private readonly bool _useTestTimeout = true;
        private readonly CancellationToken? _cancel;
        public int RunLoopCount { get; private set; }
        
        public SimpleControlledDispatcherImpl()
        {
            
        }

        public SimpleControlledDispatcherImpl(CancellationToken cancel, bool useTestTimeout = false)
        {
            _useTestTimeout = useTestTimeout;
            _cancel = cancel;
        }
        
        public void RunLoop(CancellationToken token)
        {
            RunLoopCount++;
            var st = Stopwatch.StartNew();
            while (!token.IsCancellationRequested || _cancel?.IsCancellationRequested == true)
            {
                FireBackgroundProcessing();
                ExecuteSignal();
                if (_useTestTimeout)
                    Assert.True(st.ElapsedMilliseconds < 4000, "RunLoop exceeded test time quota");
                else
                    Thread.Sleep(10);
            }
        }


    }
    
    
    [Fact]
    public void DispatcherExecutesJobsAccordingToPriority()
    {
        var impl = new SimpleDispatcherImpl();
        var disp = new Dispatcher(impl);
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
        var disp = new Dispatcher(impl);
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
        var disp = new Dispatcher(impl);
        var actions = new List<int>();
        for (var c = 0; c < 10; c++)
        {
            var itemId = c;
            disp.Post(() =>
            {
                actions.Add(itemId);
                impl.Now += 20;
            }, DispatcherPriority.Background);
        }

        Assert.False(impl.AskedForSignal);
        Assert.NotNull(impl.NextTimer);

        for (var c = 0; c < 4; c++)
        {
            Assert.NotNull(impl.NextTimer);
            Assert.False(impl.AskedForSignal);
            impl.ExecuteTimer();
            Assert.False(impl.AskedForSignal);
            impl.ExecuteSignal();
            var expectedCount = (c + 1) * 3;
            if (c == 3)
                expectedCount = 10;
            
            Assert.Equal(Enumerable.Range(0, expectedCount), actions);
            Assert.False(impl.AskedForSignal);
            if (c < 3)
            {
                Assert.True(impl.NextTimer > impl.Now);
            }
            else
                Assert.Null(impl.NextTimer);
        }
    }
    
    
    [Fact]
    public void DispatcherStopsItemProcessingWhenInputIsPending()
    {
        var impl = new SimpleDispatcherImpl();
        impl.TestInputPending = true;
        var disp = new Dispatcher(impl);
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
        impl.TestInputPending = false;

        for (var c = 0; c < 4; c++)
        {
            Assert.NotNull(impl.NextTimer);
            impl.ExecuteTimer();
            Assert.False(impl.AskedForSignal);
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
                Assert.True(impl.NextTimer > impl.Now);
                impl.Now = impl.NextTimer.Value + 1;
            }
            else
                Assert.Null(impl.NextTimer);

            impl.TestInputPending = false;
        }
    }

    [Theory,
     InlineData(false, false),
     InlineData(false, true),
     InlineData(true, false),
     InlineData(true, true)]
    public void CanWaitForDispatcherOperationFromTheSameThread(bool controlled, bool foreground)
    {
        var impl = controlled ? new SimpleControlledDispatcherImpl() : new SimpleDispatcherImpl();
        var disp = new Dispatcher(impl);
        bool finished = false;

        disp.InvokeAsync(() => finished = true,
            foreground ? DispatcherPriority.Default : DispatcherPriority.Background).Wait();

        Assert.True(finished);
        if (controlled) 
            Assert.Equal(foreground ? 0 : 1, ((SimpleControlledDispatcherImpl)impl).RunLoopCount);
    }

}