using System;
using System.Threading;
using Avalonia.Platform;
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
using MonoMac.Foundation;

namespace Avalonia.MonoMac
{
    class PlatformThreadingInterface : IPlatformThreadingInterface
    {
        private bool _signaled;
        public static PlatformThreadingInterface Instance { get; } = new PlatformThreadingInterface();
        public bool CurrentThreadIsLoopThread => NSThread.Current.IsMainThread;

        public event Action Signaled;

        public IDisposable StartTimer(TimeSpan interval, Action tick)
            => NSTimer.CreateRepeatingScheduledTimer(interval, () => tick());

        public void Signal()
        {
            lock (this)
            {
                if (_signaled)
                    return;
                _signaled = true;
            }
            NSApplication.SharedApplication.BeginInvokeOnMainThread(() =>
            {
                lock (this)
                {
                    if (!_signaled)
                        return;
                    _signaled = false;
                }
                Signaled?.Invoke();
            });
        }



        public void RunLoop(CancellationToken cancellationToken)
        {
            NSApplication.SharedApplication.ActivateIgnoringOtherApps(true);
            var app = NSApplication.SharedApplication;
            cancellationToken.Register(() =>
            {
                app.PostEvent(NSEvent.OtherEvent(NSEventType.ApplicationDefined, default(CGPoint),
                    default(NSEventModifierMask), 0, 0, null, 0, 0, 0), true);
            });
            while (!cancellationToken.IsCancellationRequested)
            {
                var ev = app.NextEvent(NSEventMask.AnyEvent, NSDate.DistantFuture, NSRunLoop.NSDefaultRunLoopMode, true);
                if (ev != null)
                {
                    app.SendEvent(ev);
                    ev.Dispose();
                }
            }
        }
    }
}