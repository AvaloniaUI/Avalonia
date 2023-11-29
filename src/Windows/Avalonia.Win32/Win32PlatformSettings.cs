using System;
using System.Runtime.InteropServices;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Win32.WinRT;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32;

internal class Win32PlatformSettings : DefaultPlatformSettings
{
    private PlatformColorValues? _lastColorValues;
    private PlatformThemeVariant? _lastThemeVariant;

    public override Size GetTapSize(PointerType type)
    {
        return type switch
        {
            PointerType.Touch => new(10, 10),
            _ => new(GetSystemMetrics(SystemMetric.SM_CXDRAG), GetSystemMetrics(SystemMetric.SM_CYDRAG)),
        };
    }

    public override Size GetDoubleTapSize(PointerType type)
    {
        return type switch
        {
            PointerType.Touch => new(16, 16),
            _ => new(GetSystemMetrics(SystemMetric.SM_CXDOUBLECLK), GetSystemMetrics(SystemMetric.SM_CYDOUBLECLK)),
        };
    }

    public override TimeSpan GetDoubleTapTime(PointerType type) => TimeSpan.FromMilliseconds(GetDoubleClickTime());
    
    public override PlatformColorValues GetColorValues()
    {
        if (Win32Platform.WindowsVersion.Major < 10)
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
        var lastThemeVariant = _lastThemeVariant;
        _lastThemeVariant = null; // clear cached registry value

        if (ThemeVariant != lastThemeVariant)
        {
            OnThemeVariantChanged();
        }

        var oldColorValues = _lastColorValues;
        var colorValues = GetColorValues();

        if (oldColorValues != colorValues)
        {
            OnColorValuesChanged(colorValues);
        }
    }

    public static DwmWindowAttribute? DarkModeAttribute { get; } = Win32Platform.WindowsVersion.Build switch
    {
        >= 18985 => DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE,
        >= 17763 => DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE_PRE_20H1,
        _ => null,
    };

    public override PlatformThemeVariant ThemeVariant
    {
        get
        {
            if (_lastThemeVariant is { } cached)
            {
                return cached;
            }

            if (DarkModeAttribute == null || RegOpenKeyEx(HKEY_CURRENT_USER, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", 0, SecurityAccessModel.KEY_READ, out var key) != 0)
            {
                return PlatformThemeVariant.Dark;
            }
            try
            {
                uint type = 0;
                var size = sizeof(int);
                var ptr = Marshal.AllocHGlobal(size);
                try
                {
                    if (RegQueryValueEx(key, "SystemUsesLightTheme", 0, ref type, ptr, ref size) != 0)
                    {
                        return PlatformThemeVariant.Dark;
                    }

                    _lastThemeVariant = Marshal.ReadInt32(ptr) == 0 ? PlatformThemeVariant.Dark : PlatformThemeVariant.Light;
                    return _lastThemeVariant.Value;
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            finally
            {
                RegCloseKey(key);
            }
        }
    }
}
