using System;
using System.Reactive.Disposables;
using Avalonia.Rendering;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    internal class RenderLoop : DefaultRenderLoop
    {
        private UnmanagedMethods.TimeCallback timerDelegate;

        public RenderLoop(int framesPerSecond)
            : base(framesPerSecond)
        {
        }

        protected override IDisposable StartCore(Action tick)
        {
            timerDelegate = (id, uMsg, user, dw1, dw2) => tick();

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
