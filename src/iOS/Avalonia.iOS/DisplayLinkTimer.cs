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
    class DisplayLinkTimer : IRenderTimer
    {
        private volatile Action<TimeSpan>? _tick;
        private Stopwatch _st = Stopwatch.StartNew();

        public DisplayLinkTimer()
        {
            var link = CADisplayLink.Create(OnLinkTick);
            SetPreferredFrameRateRange(link);
            WarnIfLimitedTo60Hz();
            TimerThread = new Thread(() =>
            {
                link.AddToRunLoop(NSRunLoop.Current, NSRunLoopMode.Common);
                NSRunLoop.Current.Run();
            });
            TimerThread.Start();
            UIApplication.Notifications.ObserveDidEnterBackground((_,__) => link.Paused = true);
            UIApplication.Notifications.ObserveWillEnterForeground((_, __) => link.Paused = false);
        }

        public Thread TimerThread { get;  }
        
        public bool RunsInBackground => true;

        // TODO: start/stop on RenderLoop request
        public Action<TimeSpan>? Tick
        {
            get => _tick;
            set => _tick = value;
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

        private void OnLinkTick()
        {
            _tick?.Invoke(_st.Elapsed);
        }
    }
}
