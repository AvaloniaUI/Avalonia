using System;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Threading;
using CoreFoundation;
using Foundation;

namespace Avalonia.iOS
{
    class PlatformThreadingInterface :  IPlatformThreadingInterface
    {
        private bool _signaled;
        public static PlatformThreadingInterface Instance { get; } = new PlatformThreadingInterface();
        public bool CurrentThreadIsLoopThread => NSThread.Current.IsMainThread;
        
        public event Action<DispatcherPriority?> Signaled;
        
        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
            => NSTimer.CreateRepeatingScheduledTimer(interval, _ => tick());

        public void Signal(DispatcherPriority prio)
        {
            lock (this)
            {
                if(_signaled)
                    return;
                _signaled = true;
            }

            DispatchQueue.MainQueue.DispatchAsync(() =>
            {
                lock (this)
                    _signaled = false;
                Signaled?.Invoke(null);
            });
        }
    }
}