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
        
        public event System.Action<DispatcherPriority?> Signaled;
        public void RunLoop(CancellationToken cancellationToken)
        {
            //Mobile platforms are using external main loop
            throw new System.NotSupportedException(); 
        }
        
        public System.IDisposable StartTimer(DispatcherPriority priority, System.TimeSpan interval, System.Action tick)
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
