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
            if (UsesHighRefreshRate)
                link.PreferredFrameRateRange = CAFrameRateRange.Create(60, 120, 120);
        }

        /// <summary>
        /// Gets whether display links ask for more than the default 60 Hz: this needs OS version 15
        /// and <see cref="iOSPlatformOptions.EnableHighRefreshRate"/>.
        /// </summary>
        internal static bool UsesHighRefreshRate =>
            Platform.Options is { EnableHighRefreshRate: true }
            && (OperatingSystem.IsIOSVersionAtLeast(15)
                || OperatingSystem.IsTvOSVersionAtLeast(15)
                || OperatingSystem.IsMacCatalystVersionAtLeast(15));

#if !TVOS
        /// <summary>
        /// Gets whether display links can actually run faster than 60 Hz on <paramref name="screen"/>:
        /// <see cref="UsesHighRefreshRate"/>, a screen faster than 60 Hz and, on iPhone,
        /// CADisableMinimumFrameDurationOnPhone in Info.plist.
        /// </summary>
        internal static bool CanExceed60Hz(UIScreen? screen) =>
            UsesHighRefreshRate && !s_isPhoneWithoutHighRefreshRateKey.Value && screen?.MaximumFramesPerSecond > 60;

        private static readonly Lazy<bool> s_isPhoneWithoutHighRefreshRateKey = new(() =>
            UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone
            && NSBundle.MainBundle.ObjectForInfoDictionary("CADisableMinimumFrameDurationOnPhone") is not NSNumber { BoolValue: true });

        private static bool s_warnedLimitedTo60Hz;

        /// <summary>
        /// Logs a warning once if rendering on <paramref name="screen"/> could go above 60 Hz but iOS keeps it at 60 Hz.
        /// </summary>
        internal static void WarnIfLimitedTo60Hz(UIScreen? screen)
        {
            if (s_warnedLimitedTo60Hz
                || Platform.Options is not { EnableHighRefreshRate: true }
                || !(screen?.MaximumFramesPerSecond > 60)
                || !s_isPhoneWithoutHighRefreshRateKey.Value)
                return;

            s_warnedLimitedTo60Hz = true;
            Logger.TryGet(LogEventLevel.Warning, LogArea.IOSPlatform)?.Log(null,
                "Rendering is limited to 60 Hz on this ProMotion display. Set CADisableMinimumFrameDurationOnPhone " +
                "to true in Info.plist, or set iOSPlatformOptions.EnableHighRefreshRate to false.");
        }
#endif

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
