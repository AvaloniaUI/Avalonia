using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Win32
{
    public class AccentColorProvider : IPlatformAccentColorProvider
    {
        public Color AccentColor
        {
            get => GetAccentColorWin("ImmersiveSystemAccent");
        }

        private static Color GetAccentColorWin(string name)
        {
            var colorSet = Avalonia.Win32.Interop.UnmanagedMethods.GetImmersiveUserColorSetPreference(false, false);
            var colorType = Avalonia.Win32.Interop.UnmanagedMethods.GetImmersiveColorTypeFromName(name);
            var rawColor = Avalonia.Win32.Interop.UnmanagedMethods.GetImmersiveColorFromColorSetEx(colorSet, colorType, false, 0);

            var bytes = BitConverter.GetBytes(rawColor);
            return Color.FromArgb(bytes[3], bytes[0], bytes[1], bytes[2]);
        }
    }
}
