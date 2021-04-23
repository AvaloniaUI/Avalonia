using System;

using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Win32.WinRT;

namespace Avalonia.Win32
{
    public class WindowsColorSchemeProvider : IPlatformColorSchemeProvider
    {
        public Color? GetSystemAccentColor()
        {
            if (Win32Platform.WindowsVersion >= new Version(10, 0, 10586))
            {
                var settings = NativeWinRTMethods.CreateInstance<IUISettings3>("Windows.UI.ViewManagement.UISettings");
                var color = settings.GetColorValue(UIColorType.Accent);
                return new Color(color.A, color.R, color.G, color.B);
            }

            return null;
        }
    }
}
