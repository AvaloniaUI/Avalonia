using System;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.Rendering;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    internal class RenderTimer : DefaultRenderTimer
    {
        private UnmanagedMethods.WaitOrTimerCallback timerDelegate;

        private static IntPtr _timerQueue;

        private static void EnsureTimerQueueCreated()
        {
            if (Volatile.Read(ref _timerQueue) == null)
            {
                var queue = UnmanagedMethods.CreateTimerQueue();
                if (Interlocked.CompareExchange(ref _timerQueue, queue, IntPtr.Zero) != IntPtr.Zero)
                {
                    UnmanagedMethods.DeleteTimerQueueEx(queue, IntPtr.Zero);
                }
            }
        }

        public RenderTimer(int framesPerSecond)
            : base(framesPerSecond)
        {
        }

        protected override IDisposable StartCore(Action<TimeSpan> tick)
        {
            EnsureTimerQueueCreated();
            var msPerFrame = 1000 / FramesPerSecond;

            timerDelegate = (_, __) => tick(TimeSpan.FromMilliseconds(Environment.TickCount));

            UnmanagedMethods.CreateTimerQueueTimer(
                out var timer,
                _timerQueue,
                timerDelegate,
                IntPtr.Zero,
                (uint)msPerFrame,
                (uint)msPerFrame,
                0
                );

            return Disposable.Create(() =>
            {
                timerDelegate = null;
                UnmanagedMethods.DeleteTimerQueueTimer(_timerQueue, timer, IntPtr.Zero);
            });
        }
    }
}
