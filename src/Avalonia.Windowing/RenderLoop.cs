using System;
using System.Reactive.Linq;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Windowing
{
    public class WinitRenderLoop : IRenderLoop
    {
        public event EventHandler<EventArgs> Tick;

        private readonly IDisposable _timer;
        public WinitRenderLoop(WindowingPlatform platform)
        {
            _timer = Observable.Interval(TimeSpan.FromMilliseconds(1000 / 60), AvaloniaScheduler.Instance)
                               .Subscribe((_) =>
            {
                Tick?.Invoke(this, EventArgs.Empty);
            });
        }
    }
}
