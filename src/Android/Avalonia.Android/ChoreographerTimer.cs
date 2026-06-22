using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Reactive;
using Avalonia.Rendering;
using static Avalonia.Android.Platform.SkiaPlatform.AndroidFramebuffer;

using Looper = Android.OS.Looper;

namespace Avalonia.Android
{
    internal sealed class ChoreographerTimer : IRenderTimer
    {
        private readonly object _lock = new();
        private readonly TaskCompletionSource<IntPtr> _choreographer = new();
        private readonly AutoResetEvent _event = new(false);
        private readonly GCHandle _timerHandle;
        private readonly HashSet<AvaloniaView> _views = new();
        private Action<TimeSpan>? _tick;
        private bool _pendingCallback;
        private long _lastTime;

        public ChoreographerTimer()
        {
            _timerHandle = GCHandle.Alloc(this);
            new Thread(Loop)
            {
                Priority = ThreadPriority.AboveNormal,
                Name = "Choreographer Thread"
            }.Start();
            new Thread(RenderLoop)
            {
                Priority = ThreadPriority.AboveNormal,
                Name = "Render Thread"
            }.Start();
        }

        public bool RunsInBackground => true;

        public Action<TimeSpan>? Tick
        {
            get => _tick;
            set
            {
                _tick = value;
                PostFrameCallbackIfNeeded();
            }
        }

        internal IDisposable SubscribeView(AvaloniaView view)
        {
            lock (_lock)
            {
                _views.Add(view);
                PostFrameCallbackIfNeeded();
            }

            return Disposable.Create(
                () =>
                {
                    lock (_lock) 
                        _views.Remove(view);
                }
            );
        }

        private void Loop()
        {
            Looper.Prepare();
            _choreographer.SetResult(AChoreographer_getInstance());
            Looper.Loop();
        }

        private void RenderLoop()
        {
            while (true)
            {
                _event.WaitOne();
                long time;
                lock (_lock)
                {
                    time = _lastTime;
                }
                _tick?.Invoke(TimeSpan.FromTicks(time / 100));
            }
        }

        private void PostFrameCallbackIfNeeded()
        {
            lock (_lock)
            {
                if(_pendingCallback)
                    return;
                
                if (_tick == null || _views.Count == 0)
                    return;

                _pendingCallback = true;
                
                PostFrameCallback(_choreographer.Task.Result, GCHandle.ToIntPtr(_timerHandle));
            }
        }

        private void DoFrameCallback(long frameTimeNanos, IntPtr data)
        {
            lock (_lock)
            {
                _pendingCallback = false;
                PostFrameCallbackIfNeeded();
                _lastTime = frameTimeNanos;
                _event.Set();
            }
        }

        private static unsafe void PostFrameCallback(IntPtr choreographer, IntPtr data)
        {
            // AChoreographer_postFrameCallback is deprecated on 10.0+. 
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                AChoreographer_postFrameCallback64(choreographer, &FrameCallback64, data);
            }
            else
            {
                AChoreographer_postFrameCallback(choreographer, &FrameCallback, data);
            }

            return;

            [UnmanagedCallersOnly]
            static void FrameCallback(int frameTimeNanos, IntPtr data)
            {
                var timer = (ChoreographerTimer)GCHandle.FromIntPtr(data).Target!;
                timer.DoFrameCallback(frameTimeNanos, data);
            }

            [UnmanagedCallersOnly]
            static void FrameCallback64(long frameTimeNanos, IntPtr data)
            {
                var timer = (ChoreographerTimer)GCHandle.FromIntPtr(data).Target!;
                timer.DoFrameCallback(frameTimeNanos, data);
            }
        }
    }
}
