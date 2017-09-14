using Avalonia.Platform;
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
                        s[i] = new MacScreen(bounds, workArea, screens[i] == primary, screens[i].Handle);
                    }

                    _allScreens = s;
                    _observer = NSNotificationCenter.DefaultCenter.AddObserver(NSApplicationDidChangeScreenParametersNotification, notification =>
                                                                                                                                  {
                                                                                                                                      _allScreens = null;
                                                                                                                                      NSNotificationCenter
                                                                                                                                          .DefaultCenter
                                                                                                                                          .RemoveObserver(_observer);
                                                                                                                                  });
                }
                return _allScreens;
            }
        }
        
        public Screen PrimaryScreen
        {
            get
            {
                for (int i = 0; i < _allScreens.Length; i++)
                {
                    if (_allScreens[i].Primary)
                        return _allScreens[i];
                }

                return null;
            }
        }

        private Screen[] _allScreens;
        private NSObject _observer;
    }
}