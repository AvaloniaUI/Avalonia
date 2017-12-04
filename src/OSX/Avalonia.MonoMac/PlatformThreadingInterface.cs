using System;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Threading;
using MonoMac.AppKit;
using MonoMac.CoreFoundation;
using MonoMac.CoreGraphics;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;

namespace Avalonia.MonoMac
{
    class PlatformThreadingInterface : NSObject, IPlatformThreadingInterface
    {
        private bool _signaled;
        private const string SignaledSelectorName = "avaloniauiSignaled";
        private readonly Selector _signaledSelector = new Selector(SignaledSelectorName);
        public static PlatformThreadingInterface Instance { get; } = new PlatformThreadingInterface();
        public bool CurrentThreadIsLoopThread => NSThread.Current.IsMainThread;

        public event Action<DispatcherPriority?> Signaled;

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
            => NSTimer.CreateRepeatingScheduledTimer(interval, () => tick());

        public void Signal(DispatcherPriority prio)
        {
            lock (this)
            {
                if (_signaled)
                    return;
                _signaled = true;
            }
            PerformSelector(_signaledSelector, NSThread.MainThread, this, false,
                new[]
                {
                    NSRunLoop.NSDefaultRunLoopMode, NSRunLoop.NSRunLoopEventTracking, NSRunLoop.NSRunLoopModalPanelMode,
                    NSRunLoop.NSRunLoopCommonModes, NSRunLoop.NSRunLoopConnectionReplyMode
                });
        }

        [Export(SignaledSelectorName)]
        public void CallSignaled()
        {
            lock (this)
            {
                if (!_signaled)
                    return;
                _signaled = false;
            }
            Signaled?.Invoke(null);
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
                    Console.WriteLine("NSEVENT");
                    app.SendEvent(ev);
                    ev.Dispose();
                }
            }
        }
    }
}