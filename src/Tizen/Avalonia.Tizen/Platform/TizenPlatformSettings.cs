using System;
using Avalonia.Platform;
using Tizen.NUI;

namespace Avalonia.Tizen.Platform;

internal class TizenPlatformSettings : DefaultPlatformSettings
{
    //private PlatformColorValues _latestValues;

    //public TizenPlatformSettings()
    //{
    //    _latestValues = base.GetColorValues();
    //}

    //public override PlatformColorValues GetColorValues()
    //{
    //    return _latestValues;
    //}

    //internal void OnViewConfigurationChanged(Application application)
    //{
     
    //    var systemTheme = Tizen.NUI.ThemeManager.GetTheme().ThemeMode;
    //    var accent1 = Color.Cyan;
    //    var accent2 = Color.Default;
    //    var accent3 = Color.Default;

    //    // Set the systemTheme, accent1, accent2, and accent3 values based on the Tizen-specific logic

    //    _latestValues = new PlatformColorValues
    //    {
    //        ThemeVariant = systemTheme == ThemeMode.Day ? PlatformThemeVariant.Light : PlatformThemeVariant.Dark,
    //        ContrastPreference = IsHighContrast(application),
    //        AccentColor1 = accent1,
    //        AccentColor2 = accent2,
    //        AccentColor3 = accent3,
    //    };

    //    OnColorValuesChanged(_latestValues);
    //}

    //private static ColorContrastPreference IsHighContrast(Application application)
    //{
    //    try
    //    {
    //        var contrastPreference = AccessibilityManager.GetHighContrastMode();
    //        return contrastPreference ? ColorContrastPreference.High : ColorContrastPreference.NoPreference;
    //    }
    //    catch
    //    {
    //        return ColorContrastPreference.NoPreference;
    //    }
    //}
}
