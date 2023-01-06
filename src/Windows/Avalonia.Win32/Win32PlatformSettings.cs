using System;
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

        var settings = NativeWinRTMethods.CreateInstance<IUISettings3>("Windows.UI.ViewManagement.UISettings");
        var accent = settings.GetColorValue(UIColorType.Accent).ToAvalonia();
        var background = settings.GetColorValue(UIColorType.Background).ToAvalonia();

        return _lastColorValues = new PlatformColorValues(
            background.R + background.G + background.B < (255 * 3 - background.R - background.G - background.B)
                ? PlatformThemeVariant.Dark
                : PlatformThemeVariant.Light,
            accent, accent, accent);
    }
    
    internal void OnColorValuesChanged(IntPtr handle)
    {
        var oldColorValues = _lastColorValues;
        var colorValues = GetColorValues();

        if (oldColorValues != colorValues)
        {
            OnColorValuesChanged(colorValues);
        }
    }
}
