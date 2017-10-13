using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Utilities;
using MonoMac.AppKit;
using MonoMac.Foundation;

namespace Avalonia.MonoMac
{
    public class ScreenImpl : IScreenImpl
    {
        private const string NSApplicationDidChangeScreenParametersNotification = "NSApplicationDidChangeScreenParametersNotification";

        public int ScreenCount
        {
            get => NSScreen.Screens.Length;
        }

        private Screen[] _allScreens;
        public Screen[] AllScreens
        {
            get
            {
                if (_allScreens == null)
                {
                    NSScreen[] screens = NSScreen.Screens;
                    Screen[] s = new Screen[screens.Length];
                    NSScreen primary = NSScreen.MainScreen;
                    for (int i = 0; i < screens.Length; i++)
                    {
                        Rect bounds = screens[i].Frame.ToAvaloniaRect().ConvertRectY();
                        Rect workArea = screens[i].VisibleFrame.ToAvaloniaRect().ConvertRectY();
                        s[i] = new MacScreen(bounds, workArea, i == 0, screens[i].Handle);
                    }

                    _allScreens = s;
                }
                return _allScreens;
            }
        }

        public ScreenImpl()
        {
            NSNotificationCenter.DefaultCenter.AddObserver(NSApplicationDidChangeScreenParametersNotification, MonitorsChanged);
        }

        private void MonitorsChanged(NSNotification notification)
        {
            _allScreens = null;
        }
    }
}