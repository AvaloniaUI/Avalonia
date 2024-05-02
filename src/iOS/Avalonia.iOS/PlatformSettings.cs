using System;
using Avalonia.Media;
using Avalonia.Platform;
using UIKit;

namespace Avalonia.iOS;

// TODO: ideally should be created per view/activity.
internal class PlatformSettings : DefaultPlatformSettings
{
    private PlatformColorValues? _lastColorValues;

    public override PlatformColorValues GetColorValues()
    {
        var themeVariant = UITraitCollection.CurrentTraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark ?
            PlatformThemeVariant.Dark :
            PlatformThemeVariant.Light;


        var contrastPreference = UITraitCollection.CurrentTraitCollection.AccessibilityContrast == UIAccessibilityContrast.High ?
            ColorContrastPreference.High :
            ColorContrastPreference.NoPreference;

        UIColor? tintColor = null;
        if (OperatingSystem.IsIOSVersionAtLeast(14))
        {
            tintColor = UIConfigurationColorTransformer.PreferredTint(UIColor.Clear);
        }

        if (tintColor is not null)
        {
            tintColor.GetRGBA(out var red, out var green, out var blue, out var alpha);
            if (red != 0 && green != 0 && blue != 0 && alpha != 0)
            {
                return _lastColorValues = new PlatformColorValues
                {
                    ThemeVariant = themeVariant,
                    ContrastPreference = contrastPreference,
                    AccentColor1 = new Color(
                        (byte)(alpha * 255),
                        (byte)(red * 255),
                        (byte)(green * 255),
                        (byte)(blue * 255))
                };
            }
        }

        return _lastColorValues = new PlatformColorValues
        {
            ThemeVariant = themeVariant, ContrastPreference = contrastPreference
        };
    }

    public void TraitCollectionDidChange()
    {
        var oldColorValues = _lastColorValues;
        var colorValues = GetColorValues();

        if (oldColorValues != colorValues)
        {
            OnColorValuesChanged(colorValues);
        }
    }
}
