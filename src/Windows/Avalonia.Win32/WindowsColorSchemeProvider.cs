using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Win32
{
    public class WindowsColorSchemeProvider : IPlatformColorSchemeProvider
    {
        public Color? GetSystemAccentColor()
        {
            if (Win32Platform.WindowsVersion.Major < 10)
            {
                return null;
            }
            else
            {
                return GetAccentColorWin("ImmersiveSystemAccent");
            }
        }

        private static Color GetAccentColorWin(string name)
        {
            var colorSet = Interop.UnmanagedMethods.GetImmersiveUserColorSetPreference(false, false);
            var colorType = Interop.UnmanagedMethods.GetImmersiveColorTypeFromName(name);
            var rawColor = Interop.UnmanagedMethods.GetImmersiveColorFromColorSetEx(colorSet, colorType, false, 0);

            var bytes = BitConverter.GetBytes(rawColor);
            return Color.FromArgb(bytes[3], bytes[0], bytes[1], bytes[2]);
        }
    }
}
