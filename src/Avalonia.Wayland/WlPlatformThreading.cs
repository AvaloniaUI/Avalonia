using System;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Wayland
{
    public class WlPlatformThreading : IPlatformThreadingInterface
    {
        private readonly AvaloniaWaylandPlatform _platform;
        private readonly Thread _mainThread;

        public WlPlatformThreading(AvaloniaWaylandPlatform platform)
        {
            _platform = platform;
            _mainThread = Thread.CurrentThread;
        }

        public void RunLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _platform.WlDisplay.Dispatch() >= 0) { }
        }

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            return Disposable.Empty;
        }

        public void Signal(DispatcherPriority priority)
        {
            
        }

        public bool CurrentThreadIsLoopThread => Thread.CurrentThread == _mainThread;

        public event Action<DispatcherPriority?>? Signaled;
    }
}
