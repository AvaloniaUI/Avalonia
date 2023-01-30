﻿using System;
using System.Runtime.InteropServices;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using Avalonia.Win32.WinRT;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32;

internal class Win32PlatformSettings : DefaultPlatformSettings
{
    private PlatformColorValues _lastColorValues;

    public override Size GetTapSize(PointerType type)
    {
        return type switch
        {
            PointerType.Touch => new(10, 10),
            _ => new(GetSystemMetrics(UnmanagedMethods.SystemMetric.SM_CXDRAG), GetSystemMetrics(UnmanagedMethods.SystemMetric.SM_CYDRAG)),
        };
    }

    public override Size GetDoubleTapSize(PointerType type)
    {
        return type switch
        {
            PointerType.Touch => new(16, 16),
            _ => new(GetSystemMetrics(UnmanagedMethods.SystemMetric.SM_CXDOUBLECLK), GetSystemMetrics(UnmanagedMethods.SystemMetric.SM_CYDOUBLECLK)),
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
        var oldColorValues = _lastColorValues;
        var colorValues = GetColorValues();

        if (oldColorValues != colorValues)
        {
            OnColorValuesChanged(colorValues);
        }
    }
}
