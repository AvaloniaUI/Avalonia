using System;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Benchmarks
{
    internal class NullThreadingPlatform : IPlatformThreadingInterface
    {
        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            return Disposable.Empty;
        }

        public void Signal(DispatcherPriority priority)
        {
        }

        public bool CurrentThreadIsLoopThread => true;

#pragma warning disable CS0067
        public event Action<DispatcherPriority?> Signaled;
#pragma warning restore CS0067

    }
}
