using Avalonia.Platform;
using MonoMac.AppKit;

namespace Avalonia.MonoMac
{
    public class ScreenImpl : IScreenImpl
    {
        public int ScreenCount
        {
            get => NSScreen.Screens.Length;
        }

        public Screen[] AllScreens
        {
            get
            {
                NSScreen[] screens = NSScreen.Screens;
                Screen[] s = new Screen[screens.Length];
                NSScreen primary = NSScreen.MainScreen;
                for (int i = 0; i < screens.Length; i++)
                {
                    Rect bounds = new Rect(screens[i].Frame.X, screens[i].Frame.Height - screens[i].Frame.Y, screens[i].Frame.Width, screens[i].Frame.Height);
                    Rect workArea = new Rect(screens[i].VisibleFrame.X, screens[i].VisibleFrame.Height - screens[i].VisibleFrame.Y, screens[i].VisibleFrame.Width, screens[i].VisibleFrame.Height);
                    s[i] = new Screen(bounds, workArea, screens[i] == primary);
                }

                return s;
            }
        }

        public Screen PrimaryScreen
        {
            get
            {
                NSScreen primary = NSScreen.MainScreen;
                Rect bounds = new Rect(primary.Frame.X, primary.Frame.Height - primary.Frame.Y, primary.Frame.Width, primary.Frame.Height);
                Rect workArea = new Rect(primary.VisibleFrame.X, primary.VisibleFrame.Height - primary.VisibleFrame.Y, primary.VisibleFrame.Width, primary.VisibleFrame.Height);
                return new Screen(bounds, workArea, true);
            }
        }
    }
}