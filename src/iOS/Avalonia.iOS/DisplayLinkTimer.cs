using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Logging;
using Avalonia.Rendering;
using CoreAnimation;
using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    /// <summary>
    /// Render timer driven by a <see cref="CADisplayLink"/>, paused while the render loop is idle
    /// or the application is in the background.
    /// </summary>
    internal sealed class DisplayLinkTimer : IRenderTimer
    {
        private readonly CADisplayLink _link;
        private readonly Stopwatch _st = Stopwatch.StartNew();
        // Guards _tick, _inBackground and _paused, which the render loop, the main thread and the link thread share.
        private readonly object _lock = new();
        private Action<TimeSpan>? _tick;
        private bool _inBackground;
        private bool _paused = true;

        public DisplayLinkTimer()
        {
            _link = CADisplayLink.Create(OnLinkTick);
            _link.Paused = true;
            SetPreferredFrameRateRange(_link);
            WarnIfLimitedTo60Hz();
            TimerThread = new Thread(() =>
            {
                _link.AddToRunLoop(NSRunLoop.Current, NSRunLoopMode.Common);
                NSRunLoop.Current.Run();
            });
            TimerThread.Start();

            UIApplication.Notifications.ObserveDidEnterBackground((_, _) => SetInBackground(true));
            UIApplication.Notifications.ObserveWillEnterForeground((_, _) => SetInBackground(false));
        }

        public Thread TimerThread { get; }

        public bool RunsInBackground => true;

        public Action<TimeSpan>? Tick
        {
            get
            {
                lock (_lock)
                    return _tick;
            }
            set
            {
                lock (_lock)
                {
                    _tick = value;
                    UpdateLinkState();
                }
            }
        }

        /// <summary>
        /// Lets <paramref name="link"/> follow the display refresh rate instead of the 60 Hz default,
        /// unless <see cref="iOSPlatformOptions.EnableHighRefreshRate"/> is false.
        /// On iPhone, this also needs CADisableMinimumFrameDurationOnPhone in the app's Info.plist.
        /// </summary>
        internal static void SetPreferredFrameRateRange(CADisplayLink link)
        {
            if (Platform.Options is { EnableHighRefreshRate: true }
                && (OperatingSystem.IsIOSVersionAtLeast(15)
                    || OperatingSystem.IsTvOSVersionAtLeast(15)
                    || OperatingSystem.IsMacCatalystVersionAtLeast(15)))
            {
                link.PreferredFrameRateRange = CAFrameRateRange.Create(60, 120, 120);
            }
        }

        private void WarnIfLimitedTo60Hz()
        {
#if !TVOS
            if (Platform.Options is { EnableHighRefreshRate: true }
                && UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone
                && UIScreen.MainScreen.MaximumFramesPerSecond > 60
                && NSBundle.MainBundle.ObjectForInfoDictionary("CADisableMinimumFrameDurationOnPhone") is not NSNumber { BoolValue: true })
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.IOSPlatform)?.Log(this,
                    "Rendering is limited to 60 Hz on this ProMotion display. Set CADisableMinimumFrameDurationOnPhone " +
                    "to true in Info.plist, or set iOSPlatformOptions.EnableHighRefreshRate to false.");
            }
#endif
        }

        private void SetInBackground(bool inBackground)
        {
            lock (_lock)
            {
                _inBackground = inBackground;
                UpdateLinkState();
            }
        }

        // Called with _lock held. CADisplayLink.Paused is documented as thread safe.
        private void UpdateLinkState()
        {
            var paused = _inBackground || _tick is null;
            if (_paused != paused)
            {
                _paused = paused;
                _link.Paused = paused;
            }
        }

        private void OnLinkTick()
        {
            Action<TimeSpan>? tick;
            // A callback already scheduled when the link was paused can still arrive.
            lock (_lock)
                tick = _paused ? null : _tick;
            tick?.Invoke(_st.Elapsed);
        }
    }
}
