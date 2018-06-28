using System;
using System.Reactive.Disposables;
using Avalonia.Rendering;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    internal class RenderTimer : DefaultRenderTimer
    {
        private UnmanagedMethods.TimeCallback timerDelegate;

        public RenderTimer(int framesPerSecond)
            : base(framesPerSecond)
        {
        }

        protected override IDisposable StartCore(Action<long> tick)
        {
            timerDelegate = (id, uMsg, user, dw1, dw2) =>
            {
                UnmanagedMethods.QueryPerformanceCounter(out long tickCount);
                tick(tickCount);
            };

            var handle = UnmanagedMethods.timeSetEvent(
                (uint)(1000 / FramesPerSecond),
                0,
                timerDelegate,
                UIntPtr.Zero,
                1);

            return Disposable.Create(() =>
            {
                timerDelegate = null;
                UnmanagedMethods.timeKillEvent(handle);
            });
        }
    }
}
