using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Android.OS;
using Android.Views;

using Avalonia.Reactive;
using Avalonia.Rendering;

using Java.Lang;

namespace Avalonia.Android
{
    internal sealed class ChoreographerTimer : Java.Lang.Object, IRenderTimer, Choreographer.IFrameCallback
    {
        private readonly object _lock = new object();

        private readonly Thread _thread;
        private readonly TaskCompletionSource<Choreographer> _choreographer = new TaskCompletionSource<Choreographer>();

        private readonly ISet<AvaloniaView> _views = new HashSet<AvaloniaView>();

        private Action<TimeSpan> _tick;
        private int _count;

        public ChoreographerTimer()
        {
            _thread = new Thread(Loop);
            _thread.Start();
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
            _choreographer.SetResult(Choreographer.Instance);
            Looper.Loop();
        }

        public void DoFrame(long frameTimeNanos)
        {
            _tick?.Invoke(TimeSpan.FromTicks(frameTimeNanos / 100));

            lock (_lock)
            {
                if (_count > 0 && _views.Count > 0)
                {
                    Choreographer.Instance.PostFrameCallback(this);
                }
            }
        }
    }
}
