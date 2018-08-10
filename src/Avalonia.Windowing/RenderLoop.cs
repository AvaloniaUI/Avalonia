using System;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Windowing
{
    public class RenderLoop : IRenderLoop
    {
        private readonly IDisposable _timer;
        public RenderLoop()
        {
            _timer = AvaloniaLocator.Current.GetService<IRuntimePlatform>().StartSystemTimer(
                TimeSpan.FromMilliseconds(1000 / 60),
                () =>
                {
                    Dispatcher.UIThread.Post(() => Tick?.Invoke(this, EventArgs.Empty), DispatcherPriority.Render);    
                });
        }

        public event EventHandler<EventArgs> Tick;
    }
}
