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
                if (allScreens == null)
                {
                    NSScreen[] screens = NSScreen.Screens;
                    Screen[] s = new Screen[screens.Length];
                    NSScreen primary = NSScreen.MainScreen;
                    for (int i = 0; i < screens.Length; i++)
                    {
                        Rect bounds = new Rect(screens[i].Frame.X, screens[i].Frame.Height - screens[i].Frame.Y, screens[i].Frame.Width,
                                               screens[i].Frame.Height);
                        Rect workArea = new Rect(screens[i].VisibleFrame.X, screens[i].VisibleFrame.Height - screens[i].VisibleFrame.Y,
                                                 screens[i].VisibleFrame.Width, screens[i].VisibleFrame.Height);
                        s[i] = new MacScreen(bounds, workArea, screens[i] == primary, screens[i].Handle);
                    }

                    allScreens = s;
                    observer = NSNotificationCenter.DefaultCenter.AddObserver(NSApplicationDidChangeScreenParametersNotification, notification =>
                                                                                                                                  {
                                                                                                                                      allScreens = null;
                                                                                                                                      NSNotificationCenter
                                                                                                                                          .DefaultCenter
                                                                                                                                          .RemoveObserver(observer);
                                                                                                                                  });
                }
                return allScreens;
            }
        }
        
        public Screen PrimaryScreen
        {
            get
            {
                for (var i = 0; i < allScreens.Length; i++)
                {
                    if (allScreens[i].Primary)
                        return allScreens[i];
                }

                return null;
            }
        }

        private Screen[] allScreens;
        private NSObject observer;
    }
}