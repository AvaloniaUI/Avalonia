using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Platform;
using Foundation;
using UIKit;

namespace Avalonia.iOS;

// TODO: ideally should be created per view/activity.
internal class PlatformSettings : DefaultPlatformSettings
{
    private PlatformColorValues _lastColorValues;

    public override PlatformColorValues GetColorValues()
    {
        var themeVariant = UITraitCollection.CurrentTraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark ?
            PlatformThemeVariant.Dark :
            PlatformThemeVariant.Light;


        var contrastPreference = UITraitCollection.CurrentTraitCollection.AccessibilityContrast == UIAccessibilityContrast.High ?
            ColorContrastPreference.High :
            ColorContrastPreference.NoPreference;
        
        return _lastColorValues = new PlatformColorValues
        {
            ThemeVariant = themeVariant,
            ContrastPreference = contrastPreference
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
