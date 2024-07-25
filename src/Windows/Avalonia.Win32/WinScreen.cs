using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Avalonia.Platform;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32;

internal sealed unsafe class WinScreen(IntPtr hMonitor) : PlatformScreen(new PlatformHandle(hMonitor, "HMonitor"))
{
    private static readonly Lazy<bool> s_hasGetDpiForMonitor = new(() =>
    {
        var shcore = LoadLibrary("shcore.dll");
        var method = GetProcAddress(shcore, nameof(GetDpiForMonitor));
        return method != IntPtr.Zero;
    });

    internal int Frequency { get; private set; }

    public void Refresh()
    {
        var info = MONITORINFOEX.Create();
        PInvoke.GetMonitorInfo(new HMONITOR(hMonitor), (MONITORINFO*)&info);

        IsPrimary = info.Base.dwFlags == 1;
        Bounds = info.Base.rcMonitor.ToPixelRect();
        WorkingArea = info.Base.rcWork.ToPixelRect();
        Scaling = GetScaling();
        DisplayName ??= GetDisplayName(ref info);

        var deviceMode = new DEVMODEW
        {
            dmFields = DEVMODE_FIELD_FLAGS.DM_DISPLAYORIENTATION | DEVMODE_FIELD_FLAGS.DM_DISPLAYFREQUENCY,
            dmSize = (ushort)Marshal.SizeOf<DEVMODEW>()
        };
        PInvoke.EnumDisplaySettings(info.szDevice.ToString(), ENUM_DISPLAY_SETTINGS_MODE.ENUM_CURRENT_SETTINGS,
            ref deviceMode);

        Frequency = (int)deviceMode.dmDisplayFrequency;
        CurrentOrientation = deviceMode.Anonymous1.Anonymous2.dmDisplayOrientation switch
        {
            DEVMODE_DISPLAY_ORIENTATION.DMDO_DEFAULT => ScreenOrientation.Landscape,
            DEVMODE_DISPLAY_ORIENTATION.DMDO_90 => ScreenOrientation.Portrait,
            DEVMODE_DISPLAY_ORIENTATION.DMDO_180 => ScreenOrientation.LandscapeFlipped,
            DEVMODE_DISPLAY_ORIENTATION.DMDO_270 => ScreenOrientation.PortraitFlipped,
            _ => ScreenOrientation.None
        };
    }

    private string? GetDisplayName(ref MONITORINFOEX monitorinfo)
    {
        var deviceName = monitorinfo.szDevice;
        if (Win32Platform.WindowsVersion >= PlatformConstants.Windows7)
        {
            if (PInvoke.GetDisplayConfigBufferSizes(
                    QUERY_DISPLAY_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS,
                    out var numPathInfo, out var numModeInfo) != WIN32_ERROR.NO_ERROR)
                return null;

            var paths = stackalloc DISPLAYCONFIG_PATH_INFO[(int)numPathInfo];
            var modes = stackalloc DISPLAYCONFIG_MODE_INFO[(int)numModeInfo];

            if (PInvoke.QueryDisplayConfig(
                    QUERY_DISPLAY_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS, ref numPathInfo, paths, ref numModeInfo, modes,
                    default) != WIN32_ERROR.NO_ERROR)
                return null;

            var sourceName = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
            sourceName.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
            sourceName.header.size = (uint)sizeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME);
            var targetName = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
            targetName.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
            targetName.header.size = (uint)sizeof(DISPLAYCONFIG_TARGET_DEVICE_NAME);

            for (var i = 0; i < numPathInfo; i++)
            {
                sourceName.header.adapterId = paths[i].targetInfo.adapterId;
                sourceName.header.id = paths[i].sourceInfo.id;

                targetName.header.adapterId = paths[i].targetInfo.adapterId;
                targetName.header.id = paths[i].targetInfo.id;

                if (PInvoke.DisplayConfigGetDeviceInfo(ref sourceName.header) != 0)
                    break;

                if (!sourceName.viewGdiDeviceName.Equals(deviceName.ToString()))
                    continue;

                if (PInvoke.DisplayConfigGetDeviceInfo(ref targetName.header) != 0)
                    break;

                return targetName.monitorFriendlyDeviceName.ToString();
            }
        }

        // Fallback to MONITORINFOEX - \\DISPLAY1.
        return deviceName.ToString();
    }

    private double GetScaling()
    {
        double dpi;

        if (s_hasGetDpiForMonitor.Value)
        {
            GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var x, out _);
            dpi = x;
        }
        else
        {
            var hdc = GetDC(IntPtr.Zero);

            double virtW = GetDeviceCaps(hdc, DEVICECAP.HORZRES);
            double physW = GetDeviceCaps(hdc, DEVICECAP.DESKTOPHORZRES);

            dpi = (96d * physW / virtW);

            ReleaseDC(IntPtr.Zero, hdc);
        }

        return dpi / 96d;
    }
}
