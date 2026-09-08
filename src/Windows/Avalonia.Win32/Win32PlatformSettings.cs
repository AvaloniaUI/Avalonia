using System;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Win32.WinRT;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32;

internal class Win32PlatformSettings : DefaultPlatformSettings
{
    private double _textScaleFactor = s_uiSettings2?.TextScaleFactor ?? 1;
    private PlatformColorValues? _colorValues;
    private string? _lastLanguage;

    private static readonly IUISettings2? s_uiSettings2;
    private static readonly bool s_uiSettingsSupported;
    private static readonly Lazy<bool> s_globalizationSupported = new(() =>
        WinRTApiInformation.IsTypePresent("Windows.System.UserProfile.GlobalizationPreferences"));

    static Win32PlatformSettings()
    {
        s_uiSettingsSupported = WinRTApiInformation.IsTypePresent("Windows.UI.ViewManagement.UISettings")
        && WinRTApiInformation.IsTypePresent("Windows.UI.ViewManagement.AccessibilitySettings");

        if (s_uiSettingsSupported)
        {
            s_uiSettings2 = NativeWinRTMethods.CreateInstance<IUISettings2>("Windows.UI.ViewManagement.UISettings");
        }
    }

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
        => _colorValues ??= GetUncachedColorValues();

    private PlatformColorValues GetUncachedColorValues()
    {
        if (!s_uiSettingsSupported)
        {
            return new PlatformColorValues
            {
                ThemeVariant = PlatformThemeVariant.Light
            };
        }

        using var uiSettings = NativeWinRTMethods.CreateInstance<IUISettings3>("Windows.UI.ViewManagement.UISettings");
        var accent = uiSettings.GetColorValue(UIColorType.Accent).ToAvalonia();

        using var accessibilitySettings = NativeWinRTMethods.CreateInstance<IAccessibilitySettings>("Windows.UI.ViewManagement.AccessibilitySettings");
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
        var oldColorValues = _colorValues;
        var colorValues = GetUncachedColorValues();

        if (oldColorValues != colorValues)
        {
            _colorValues = colorValues;
            OnColorValuesChanged(colorValues);
        }

        var newTextScaleFactor = s_uiSettings2?.TextScaleFactor ?? 1;
        if (newTextScaleFactor != _textScaleFactor)
        {
            _textScaleFactor = newTextScaleFactor;
            OnTextScaleChanged();
        }
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

    private string QueryPreferredApplicationLanguage()
    {
        // `GetUserPreferredUILanguages`win32 API doesn't seem to respect Win11 "Preferred Languages" setting.
        // While GlobalizationPreferences works fine.
        if (s_globalizationSupported.Value)
        {
            using var globalizationPreferences = NativeWinRTMethods.CreateActivationFactory<IGlobalizationPreferencesStatics>("Windows.System.UserProfile.GlobalizationPreferences");
            var languages = globalizationPreferences.Languages;
            if (languages.Size > 0)
            {
                using var languageHString = new HStringInterop(languages.GetAt(0));
                if (languageHString.Value is { } language)
                {
                    return language;
                }
            }
        }

        return base.PreferredApplicationLanguage;
    }

    /// <summary>
    /// The algorithm used is undocumented, but <see href="https://github.com/microsoft/microsoft-ui-xaml/blob/5788dee271452753d5b2c70179f976c3e96a45c7/src/dxaml/xcp/core/text/common/TextFormatting.cpp#L181-L204">defined in Microsoft's source code</see>.
    /// </summary>
    public override double GetScaledFontSize(Visual target, double baseFontSize)
    {
        if (baseFontSize <= 0 || _textScaleFactor == 1)
        {
            return baseFontSize;
        }

        return Math.Max(1, baseFontSize) + Math.Max(-Math.Exp(1) * Math.Log(baseFontSize) + 18, 0.0) * (_textScaleFactor - 1);
    }
}
