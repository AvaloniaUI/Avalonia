using System;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Win32.WinRT;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32;

internal class Win32PlatformSettings : DefaultPlatformSettings
{
    private PlatformColorValues? _lastColorValues;
    private double _textScaleFactor = s_uiSettings2?.TextScaleFactor ?? 1;

    private static readonly IUISettings2? s_uiSettings2;
    private static readonly bool s_uiSettingsSupported;

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

    public override PlatformColorValues GetColorValues()
    {
        if (!s_uiSettingsSupported)
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
            return _lastColorValues = new PlatformColorValues
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
            return _lastColorValues = new PlatformColorValues
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
        var oldColorValues = _lastColorValues;
        var colorValues = GetColorValues();

        if (oldColorValues != colorValues)
        {
            OnColorValuesChanged(colorValues);
        }

        var newTextScaleFactor = s_uiSettings2?.TextScaleFactor ?? 1;
        if (newTextScaleFactor != _textScaleFactor)
        {
            _textScaleFactor = newTextScaleFactor;
            OnTextScaleChanged();
        }
    }

    /// <summary>
    /// The algorithm used is undocumented, but <see href="https://github.com/microsoft/microsoft-ui-xaml/blob/5788dee271452753d5b2c70179f976c3e96a45c7/src/dxaml/xcp/core/text/common/TextFormatting.cpp#L181-L204">defined in Microsoft's source code</see>.
    /// </summary>
    public override double GetScaledFontSize(double baseFontSize)
    {
        if (baseFontSize <= 0)
        {
            return baseFontSize;
        }

        return Math.Max(1, baseFontSize) + Math.Max(-Math.Exp(1) * Math.Log(baseFontSize) + 18, 0.0) * (_textScaleFactor - 1);
    }
}
