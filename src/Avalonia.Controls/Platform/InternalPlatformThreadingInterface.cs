using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Controls.Platform
{
    public class InternalPlatformThreadingInterface : IPlatformThreadingInterface, IRenderLoop
    {
        public InternalPlatformThreadingInterface()
        {
            TlsCurrentThreadIsLoopThread = true;
            StartTimer(DispatcherPriority.Render, new TimeSpan(0, 0, 0, 0, 66), () => Tick?.Invoke(this, new EventArgs()));
        }

        private readonly AutoResetEvent _signaled = new AutoResetEvent(false);
        private readonly AutoResetEvent _queued = new AutoResetEvent(false);

        private readonly Queue<Action> _actions = new Queue<Action>();

        public void RunLoop(CancellationToken cancellationToken)
        {
            var handles = new[] {_signaled, _queued};
            while (true)
            {
                if (0 == WaitHandle.WaitAny(handles))
                    Signaled?.Invoke(null);
                else
                {
                    while (true)
                    {
                        Action item;
                        lock (_actions)
                            if (_actions.Count == 0)
                                break;
                            else
                                item = _actions.Dequeue();
                        item();
                    }
                }
            }
        }

        public void Send(Action cb)
        {
            lock (_actions)
            {
                _actions.Enqueue(cb);
                _queued.Set();
            }
        }

        class WatTimer : IDisposable
        {
            private readonly IDisposable _timer;
            private GCHandle _handle;

            public WatTimer(IDisposable timer)
            {
                _timer = timer;
                _handle = GCHandle.Alloc(_timer);
            }

            public void Dispose()
            {
                _handle.Free();
                _timer.Dispose();
            }
        }

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            return new WatTimer(new System.Threading.Timer(delegate
            {
                var tcs = new TaskCompletionSource<int>();
                Send(() =>
                {
                    try
                    {
                        tick();
                    }
                    finally
                    {
                        tcs.SetResult(0);
                    }
                });


                tcs.Task.Wait();
            }, null, TimeSpan.Zero, interval));


        }

        public void Signal(DispatcherPriority prio)
        {
            _signaled.Set();
        }

        [ThreadStatic] private static bool TlsCurrentThreadIsLoopThread;

        public bool CurrentThreadIsLoopThread => TlsCurrentThreadIsLoopThread;
        public event Action<DispatcherPriority?> Signaled;
        public event EventHandler<EventArgs> Tick;

    }
}