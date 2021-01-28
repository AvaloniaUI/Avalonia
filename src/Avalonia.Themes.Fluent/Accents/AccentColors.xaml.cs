using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;

namespace Avalonia.Themes.Fluent.Accents
{
    public class AccentColors : Styles
    {
        public AccentColors()
        {
            AvaloniaXamlLoader.Load(this);

            var accentcolor = GetAccentColor();
            var light1 = ChangeLuminosity(accentcolor, 0.3);
            var light2 = ChangeLuminosity(accentcolor, 0.5);
            var light3 = ChangeLuminosity(accentcolor, 0.7);
            var dark1 = ChangeLuminosity(accentcolor, -0.3);
            var dark2 = ChangeLuminosity(accentcolor, -0.5);
            var dark3 = ChangeLuminosity(accentcolor, -0.7);


            this.Resources.Add("SystemAccentColor", accentcolor);
            this.Resources.Add("SystemAccentColorLight1", light1);
            this.Resources.Add("SystemAccentColorLight2", light2);
            this.Resources.Add("SystemAccentColorLight3", light3);
            this.Resources.Add("SystemAccentColorDark1", dark1);
            this.Resources.Add("SystemAccentColorDark2", dark2);
            this.Resources.Add("SystemAccentColorDark3", dark3);
        }

        private Color GetAccentColor()
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows); //check if the OS is Windows
            var defaultValue = "#FF0078D7";// default Avalonia Accent Color

            switch (isWindows)
            {
                case true:
                    return GetAccentColorCore("ImmersiveSystemAccent");
                case false:
                    return Color.Parse(defaultValue);
            }
        }

        [DllImport("uxtheme.dll", EntryPoint = "#95", CharSet = CharSet.Unicode)]
        private static extern uint GetImmersiveColorFromColorSetEx(uint dwImmersiveColorSet, uint dwImmersiveColorType, bool bIgnoreHighContrast, uint dwHighContrastCacheMode);
        [DllImport("uxtheme.dll", EntryPoint = "#96", CharSet = CharSet.Unicode)]
        private static extern uint GetImmersiveColorTypeFromName(string name);
        [DllImport("uxtheme.dll", EntryPoint = "#98", CharSet = CharSet.Unicode)]
        private static extern uint GetImmersiveUserColorSetPreference(bool bForceCheckRegistry, bool bSkipCheckOnFail);

        private static Color GetAccentColorCore(string name)
        {
            var colorSet = GetImmersiveUserColorSetPreference(false, false);
            var colorType = GetImmersiveColorTypeFromName(name);
            var rawColor = GetImmersiveColorFromColorSetEx(colorSet, colorType, false, 0);

            var bytes = BitConverter.GetBytes(rawColor);
            return Color.FromArgb(bytes[3], bytes[0], bytes[1], bytes[2]);
        }

        private Color ChangeLuminosity(Color color, double correctionFactor)
        {
            var red = (double)color.R;
            var green = (double)color.G;
            var blue = (double)color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            return new Color(color.A, (byte)red, (byte)green, (byte)blue);
        }
    }
}
