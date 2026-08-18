using System;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Win32.WinRT;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32;

internal class Win32PlatformSettings : DefaultPlatformSettings
{
    private static readonly Lazy<bool> s_uiSettingsSupported = new(() =>
        WinRTApiInformation.IsTypePresent("Windows.UI.ViewManagement.UISettings")
        && WinRTApiInformation.IsTypePresent("Windows.UI.ViewManagement.AccessibilitySettings"));

    private string? _lastLanguage;

    public override Size GetTapSize(PointerType type)
    {
        return type switch
        {
            PointerType.Mouse => new(GetSystemMetrics(SystemMetric.SM_CXDRAG), GetSystemMetrics(SystemMetric.SM_CYDRAG)),
            _ => base.GetTapSize(type)
        };
    }

    public override Size GetDoubleTapSize(PointerType type)
    {
        return type switch
        {
            PointerType.Mouse => new(GetSystemMetrics(SystemMetric.SM_CXDOUBLECLK), GetSystemMetrics(SystemMetric.SM_CYDOUBLECLK)),
            _ => base.GetDoubleTapSize(type)
        };
    }

    public override TimeSpan GetDoubleTapTime(PointerType type) => TimeSpan.FromMilliseconds(GetDoubleClickTime());

    public override string PreferredApplicationLanguage =>
        _lastLanguage ??= QueryPreferredApplicationLanguage();

    public override PlatformColorValues GetColorValues()
    {
        if (!s_uiSettingsSupported.Value)
        {
            return base.GetColorValues();
        }

        var uiSettings = NativeWinRTMethods.CreateInstance<IUISettings3>("Windows.UI.ViewManagement.UISettings");
        var accent = uiSettings.GetColorValue(UIColorType.Accent).ToAvalonia();

        var accessibilitySettings = NativeWinRTMethods.CreateInstance<IAccessibilitySettings>("Windows.UI.ViewManagement.AccessibilitySettings");
        if (accessibilitySettings.HighContrast == 1)
        {
            // Windows 11 has 4 different high contrast schemes:
            // - Aquatic - High Contrast Black
            // - Desert - High Contrast White
            // - Dusk - High Contrast #1
            // - Night sky - High Contrast #2
            // Only "Desert" one can be considered a "light" preference. 
            using var highContrastScheme = new HStringInterop(accessibilitySettings.HighContrastScheme);
            return new PlatformColorValues
            {
                ThemeVariant = highContrastScheme.Value?.Contains("White") == true ?
                    PlatformThemeVariant.Light :
                    PlatformThemeVariant.Dark,
                ContrastPreference = ColorContrastPreference.High,
                // Windows provides more than one accent color for the HighContrast themes, but with no API for that (at least not in the WinRT)
                AccentColor1 = accent
            };
        }
        else
        {
            var background = uiSettings.GetColorValue(UIColorType.Background).ToAvalonia();
            return new PlatformColorValues
            {
                ThemeVariant = background.R + background.G + background.B < (255 * 3 - background.R - background.G - background.B) ?
                    PlatformThemeVariant.Dark :
                    PlatformThemeVariant.Light,
                ContrastPreference = ColorContrastPreference.NoPreference,
                AccentColor1 = accent
            };   
        }
    }
    
    internal void OnColorValuesChanged()
    {
        OnColorValuesChanged(GetColorValues());
    }

    internal void OnLanguageChanged()
    {
        var oldLanguage = _lastLanguage;
        _lastLanguage = null;
        var newLanguage = PreferredApplicationLanguage;

        if (oldLanguage != newLanguage)
        {
            OnPreferredApplicationLanguageChanged();
        }
    }

    private unsafe string QueryPreferredApplicationLanguage()
    {
        // A copy of https://github.com/dotnet/runtime/blob/6550ccf7827cdc363035f8927f68a34edb01bf22/src/libraries/System.Private.CoreLib/src/System/Globalization/CultureInfo.Windows.cs#L20
        // .NET BCL already reads GetUserPreferredUILanguages as a default value of CultureInfo.CurrentUICulture.
        // Unfortunately, CultureInfo.CurrentUICulture is mutable, and BCL doesn't expose static default value.

        const uint MUI_LANGUAGE_NAME = 0x8;    // Use ISO language (culture) name convention
        uint langCount = 0;
        uint bufLen = 0;

        if (GetUserPreferredUILanguages(MUI_LANGUAGE_NAME, &langCount, null, &bufLen))
        {
            var languages = bufLen <= 256 ? stackalloc char[(int)bufLen] : new char[bufLen];
            fixed (char* pLanguages = languages)
            {
                if (GetUserPreferredUILanguages(MUI_LANGUAGE_NAME, &langCount, pLanguages, &bufLen))
                {
                    return languages.ToString();
                }
            }
        }

        return base.PreferredApplicationLanguage;
    }
}
