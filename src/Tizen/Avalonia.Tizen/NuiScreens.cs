using Avalonia.Platform;
using Tizen.Applications;
using Tizen.Multimedia;
using Tizen.NUI;
using Tizen.System;

namespace Avalonia.Tizen;

internal class NuiScreens : ScreensBase<int, SingleTizenScreen>
{
    // See https://github.com/dotnet/maui/blob/8.0.70/src/Essentials/src/DeviceDisplay/DeviceDisplay.tizen.cs
    internal const float BaseLogicalDpi = 160.0f;

    internal static DeviceOrientation LastDeviceOrientation { get; private set; }

    internal static int DisplayWidth =>
        Information.TryGetValue<int>("http://tizen.org/feature/screen.width", out var value) ? value : 0;

    internal static int DisplayHeight =>
        Information.TryGetValue<int>("http://tizen.org/feature/screen.height", out var value) ? value : 0;

    internal static int DisplayDpi => TizenRuntimePlatform.Info.Value.IsTV ? 72 :
        Information.TryGetValue<int>("http://tizen.org/feature/screen.dpi", out var value) ? value : 72;

    public NuiScreens()
    {
        ((CoreApplication)global::Tizen.Applications.Application.Current).DeviceOrientationChanged += (sender, args) =>
        {
            LastDeviceOrientation = args.DeviceOrientation;
            OnChanged();
        };
    }

    protected override int GetScreenCount() => 1;

    protected override IReadOnlyList<int> GetAllScreenKeys() => [1];

    protected override SingleTizenScreen CreateScreenFromKey(int key)
    {
        var screen = new SingleTizenScreen(key);
        screen.Refresh();
        return screen;
    }

    protected override void ScreenChanged(SingleTizenScreen screen) => screen.Refresh();
}

internal class SingleTizenScreen(int index) : PlatformScreen(new PlatformHandle(new IntPtr(index), nameof(SingleTizenScreen)))
{
    public void Refresh()
    {
        IsPrimary = index == 1;
        if (IsPrimary)
        {
            Bounds = WorkingArea = new PixelRect(0, 0, NuiScreens.DisplayWidth, NuiScreens.DisplayHeight);
            Scaling = NuiScreens.DisplayDpi / NuiScreens.BaseLogicalDpi;

            var isNaturalLandscape = Bounds.Width > Bounds.Height;
            CurrentOrientation = (isNaturalLandscape, NuiScreens.LastDeviceOrientation) switch
            {
                (true, DeviceOrientation.Orientation_0) => ScreenOrientation.Landscape,
                (true, DeviceOrientation.Orientation_90) => ScreenOrientation.Portrait,
                (true, DeviceOrientation.Orientation_180) => ScreenOrientation.LandscapeFlipped,
                (true, DeviceOrientation.Orientation_270) => ScreenOrientation.PortraitFlipped,
                (false, DeviceOrientation.Orientation_0) => ScreenOrientation.Portrait,
                (false, DeviceOrientation.Orientation_90) => ScreenOrientation.Landscape,
                (false, DeviceOrientation.Orientation_180) => ScreenOrientation.PortraitFlipped,
                (false, DeviceOrientation.Orientation_270) => ScreenOrientation.LandscapeFlipped,
                _ => ScreenOrientation.None
            };
        }
    }
}
