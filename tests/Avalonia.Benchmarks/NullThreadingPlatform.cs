using System;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Benchmarks
{
    internal class NullThreadingPlatform : IDispatcherImpl
    {
        public void Signal()
        {
        }
        
        public void UpdateTimer(long? dueTimeInMs)
        {
        }

        public bool CurrentThreadIsLoopThread => true;

#pragma warning disable CS0067
        public event Action Signaled;
        public event Action Timer;
        public long Now => 0;
#pragma warning restore CS0067
    }
}
