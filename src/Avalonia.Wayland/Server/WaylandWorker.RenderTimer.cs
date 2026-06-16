using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using Avalonia.Logging;
using Avalonia.Rendering;

namespace Avalonia.Wayland.Server;

partial class WaylandWorker
{
    private bool _renderLoopWakeupPending = false;
    private ServerSignaler _renderLoopWakeupSignaler = null!;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private TimeSpan? _renderLoopStarvedSince;
    private readonly object _renderLoopStarvationLock = new object();
    
    
    private readonly RenderLoopImpl _renderLoop = new RenderLoopImpl();
    public IRenderLoop RenderLoop => _renderLoop;
    
    // The target FPS for UI thread animations when Wayland compositor doesn't think
    // that we should be rendering yet
    private const int ThrottledUiThreadFps = 20;
    private static readonly TimeSpan s_RenderLoopStarvationInterval = TimeSpan.FromSeconds(1.0 / ThrottledUiThreadFps);

    private readonly System.Timers.Timer _renderLoopStarvationTimer = new System.Timers.Timer(s_RenderLoopStarvationInterval);

    class RenderLoopImpl : IRenderLoop
    {
        private readonly List<IRenderLoopTask> _tasks = new();
        private readonly List<IRenderLoopTask> _tasksCopy = new();
        private int _inTick;
        internal Action? WakeupCallback;

        public bool RunsInBackground => true;
        
        public void Add(IRenderLoopTask i)
        {
            lock (_tasks)
                _tasks.Add(i);
            Wakeup();
        }

        public void Remove(IRenderLoopTask i)
        {
            lock (_tasks)
                _tasks.Remove(i);
        }

        public void Wakeup()
        {
            WakeupCallback?.Invoke();
        }

        public void DoTick()
        {
            if (Interlocked.CompareExchange(ref _inTick, 1, 0) != 0)
                return;
            try
            {
                lock (_tasks)
                {
                    _tasksCopy.Clear();
                    _tasksCopy.AddRange(_tasks);
                }

                for (int i = 0; i < _tasksCopy.Count; i++)
                    _tasksCopy[i].Render();

                _tasksCopy.Clear();
            }
            finally
            {
                Interlocked.Exchange(ref _inTick, 0);
            }
        }
    }
    
    public void WakeupRenderLoop() => _renderLoopWakeupPending = true;

    public void AnyThreadWakeupRenderLoop() => _renderLoopWakeupSignaler.Signal();
    
    void InitRenderTimer()
    {
        _renderLoop.WakeupCallback = () =>
        {
            WakeupRenderLoop();
            _wakeupFd.Set();
        };
        
        Compositor.AfterCommit += delegate
        {
            if (_hasPendingServerJobs)
            {
                _hasPendingServerJobs = false;
                AnyThreadWakeupRenderLoop();
            }

            lock (_renderLoopStarvationLock)
            {
                if (_renderLoopStarvedSince == null)
                {
                    _renderLoopStarvedSince = _clock.Elapsed;
                    _renderLoopStarvationTimer.Enabled = true;
                }
            }
            _wakeupFd.Set();
        };
        _renderLoopWakeupSignaler = new ServerSignaler(this, WakeupRenderLoop);
        _renderLoopStarvationTimer.Elapsed += delegate { OnRenderLoopStarved(); };
    }

    private void OnRenderLoopStarved()
    {
        lock (_renderLoopStarvationLock)
        {
            if (_renderLoopStarvedSince.HasValue &&
                _renderLoopStarvedSince.Value + s_RenderLoopStarvationInterval < _clock.Elapsed)
                AnyThreadWakeupRenderLoop();
        }
    }

    private void TickRenderLoopIfNeeded()
    {
        if (_renderLoopWakeupPending)
        {
            lock (_renderLoopStarvationLock)
            {
                _renderLoopStarvedSince = null;
                _renderLoopStarvationTimer.Stop();
            }

            _renderLoopWakeupPending = false;
            try
            {
                _renderLoop.DoTick();
            }
            catch (Exception ex)
            {
                AnyThreadWakeupRenderLoop();
                Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this, "Exception in render loop: {Error}", ex);
            }
        }
    }
}