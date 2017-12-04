using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Platform;
using Avalonia.Rendering;
using MonoMac.Foundation;

namespace Avalonia.MonoMac
{
    //TODO: Switch to using CVDisplayLink
    public class RenderLoop : IRenderLoop
    {
        private readonly object _lock = new object();
        private readonly IDisposable _timer;

        public RenderLoop()
        {
            _timer = AvaloniaLocator.Current.GetService<IRuntimePlatform>().StartSystemTimer(new TimeSpan(0, 0, 0, 0, 1000 / 60),
                () =>
                {
                    lock (_lock)
                    {
                        using (new NSAutoreleasePool())
                        {
                            Tick?.Invoke(this, EventArgs.Empty);
                        }
                    }
                });
        }

        public event EventHandler<EventArgs> Tick;
    }
}
