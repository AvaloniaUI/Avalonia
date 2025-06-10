using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Android.OS;
using Android.Views;

using Avalonia.Reactive;
using Avalonia.Rendering;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Avalonia.Android
{
    internal sealed class ChoreographerTimer : Java.Lang.Object, IRenderTimer, Choreographer.IFrameCallback
    {
        private readonly object _lock = new();
        
        private AutoResetEvent _event = new(false);
        private long _lastTime;
        private readonly TaskCompletionSource<Choreographer> _choreographer = new();

        private readonly ISet<AvaloniaView> _views = new HashSet<AvaloniaView>();

        private Action<TimeSpan>? _tick;
        private int _count;

        public ChoreographerTimer()
        {
            new Thread(Loop)
            {
                Priority = ThreadPriority.AboveNormal
            }.Start();
            new Thread(RenderLoop)
            {
                Priority = ThreadPriority.AboveNormal
            }.Start();
        }

        public bool RunsInBackground => true;

        public event Action<TimeSpan> Tick
        {
            add
            {
                lock (_lock)
                {
                    _tick += value;
                    _count++;

                    if (_count == 1)
                    {
                        _choreographer.Task.Result.PostFrameCallback(this);
                    }
                }
            }
            remove
            {
                lock (_lock)
                {
                    _tick -= value;
                    _count--;
                }
            }
        }

        internal IDisposable SubscribeView(AvaloniaView view)
        {
            lock (_lock)
            {
                _views.Add(view);

                if (_views.Count == 1)
                {
                    _choreographer.Task.Result.PostFrameCallback(this);
                }
            }

            return Disposable.Create(
                () =>
                {
                    lock (_lock)
                    {
                        _views.Remove(view);
                    }
                }
            );
        }

        private void Loop()
        {
            Looper.Prepare();
            _choreographer.SetResult(Choreographer.Instance!);
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


        public void DoFrame(long frameTimeNanos)
        {
            lock (_lock)
            {
                if (_count > 0 && _views.Count > 0)
                {
                    Choreographer.Instance!.PostFrameCallback(this);
                }
                _lastTime = frameTimeNanos;
                _event.Set();
            }
        }
    }
}
