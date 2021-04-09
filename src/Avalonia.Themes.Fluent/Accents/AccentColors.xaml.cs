using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Avalonia.Themes.Fluent.Accents
{
    public class AccentColors : Styles
    {
        public AccentColors()
        {
            AvaloniaXamlLoader.Load(this);

            TryGetResource("FallbackSystemAccentColor", out object accentcolorResource);
            var accentColorProvider = AvaloniaLocator.CurrentMutable.GetService<IPlatformColorSchemeProvider>();
            Color accentcolor;

            if(accentColorProvider is null)
            {
                accentcolor = (Color)accentcolorResource;
            }
            else
            {
                var systemAccentColor = accentColorProvider.GetSystemAccentColor();
                switch (systemAccentColor.HasValue)
                {
                    case true:
                        accentcolor = systemAccentColor.Value;
                        break;
                    case false:
                        accentcolor = (Color)accentcolorResource;
                        break;
                }
            }

            
            var light1 = ChangeColorLuminosity(accentcolor, 0.3);
            var light2 = ChangeColorLuminosity(accentcolor, 0.5);
            var light3 = ChangeColorLuminosity(accentcolor, 0.7);
            var dark1 = ChangeColorLuminosity(accentcolor, -0.3);
            var dark2 = ChangeColorLuminosity(accentcolor, -0.5);
            var dark3 = ChangeColorLuminosity(accentcolor, -0.7);


            this.Resources.Add("SystemAccentColor", accentcolor);
            this.Resources.Add("SystemAccentColorLight1", light1);
            this.Resources.Add("SystemAccentColorLight2", light2);
            this.Resources.Add("SystemAccentColorLight3", light3);
            this.Resources.Add("SystemAccentColorDark1", dark1);
            this.Resources.Add("SystemAccentColorDark2", dark2);
            this.Resources.Add("SystemAccentColorDark3", dark3);
        }

        internal static Color ChangeColorLuminosity(Color color, double newluminosityFactor)
        {
            var red = (double)color.R;
            var green = (double)color.G;
            var blue = (double)color.B;

            if (newluminosityFactor < 0)//applies darkness
            {
                newluminosityFactor = 1 + newluminosityFactor;
                red *= newluminosityFactor;
                green *= newluminosityFactor;
                blue *= newluminosityFactor;
            }
            else if (newluminosityFactor >= 0) //applies lightness
            {
                red = (255 - red) * newluminosityFactor + red;
                green = (255 - green) * newluminosityFactor + green;
                blue = (255 - blue) * newluminosityFactor + blue;
            }
            else
            {
                throw new ArgumentOutOfRangeException("The Luminosity Factor must be a finite number.");
            }

            return new Color(color.A, (byte)red, (byte)green, (byte)blue);
        }
    }
}
