using System;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Benchmarks
{
    internal class NullThreadingPlatform : IPlatformThreadingInterface
    {
        public void RunLoop(CancellationToken cancellationToken)
        {
        }

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            return Disposable.Empty;
        }

        public void Signal(DispatcherPriority priority)
        {
        }

        public bool CurrentThreadIsLoopThread => true;

        public event Action<DispatcherPriority?> Signaled;
    }
}
