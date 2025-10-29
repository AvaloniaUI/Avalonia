using System;
using System.Collections.Generic;
using Android.Content;
using Android.Hardware.Display;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.Core.View;
using AndroidX.Window.Layout;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Platform;
using AndroidOrientation = global::Android.Content.Res.Orientation;

namespace Avalonia.Android.Platform;

internal class AndroidScreen(Display display) : PlatformScreen(new PlatformHandle(new IntPtr(display.DisplayId), "DisplayId"))
{
    public void Refresh(Context context)
    {
        DisplayName = display.Name;

        var naturalOrientation = ScreenOrientation.Portrait;
        var rotation = display.Rotation;
        IsPrimary = display.DisplayId == Display.DefaultDisplay;

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var metricsCalc = WindowMetricsCalculator.Companion.OrCreate;
            // a display context is guaranteed to be created for a display on API 30 and above, but we fallback to the app context if OEM messes up
            var displayContext = context.CreateDisplayContext(display) ?? context;
            // Get the bounds of the display
            var metrics = metricsCalc.ComputeMaximumWindowMetrics(displayContext);
            Bounds = new(metrics.Bounds.Left, metrics.Bounds.Top, metrics.Bounds.Width(), metrics.Bounds.Height());

            var windowInsets = new WindowInsetsCompat.Builder().Build();
            if (windowInsets?.GetInsets(WindowInsetsCompat.Type.SystemBars()) is { } inset)
            {
                WorkingArea = new(Bounds.X + inset.Left,
                    Bounds.Y + inset.Top,
                    Bounds.Width - (inset.Left + inset.Right),
                    Bounds.Height - (inset.Top + inset.Bottom));
            }
            else
            {
                WorkingArea = Bounds;
            }

            if (context.Resources?.Configuration is { } config)
            {
                Scaling = config.DensityDpi / (float)global::Android.Util.DisplayMetricsDensity.Default;
            }

            var orientation = displayContext.Resources?.Configuration?.Orientation;
            if (orientation == AndroidOrientation.Square)
                naturalOrientation = ScreenOrientation.None;
            else if (rotation is SurfaceOrientation.Rotation0 or SurfaceOrientation.Rotation180)
                naturalOrientation = orientation == AndroidOrientation.Landscape ?
                    ScreenOrientation.Landscape :
                    ScreenOrientation.Portrait;
            else
                naturalOrientation = orientation == AndroidOrientation.Portrait ?
                    ScreenOrientation.Landscape :
                    ScreenOrientation.Portrait;
        }
        else
        {
            var displayMetrics = new DisplayMetrics();
            display.GetRealMetrics(displayMetrics);
            Scaling = displayMetrics.Density;
            Bounds = WorkingArea = new(0, 0, displayMetrics.WidthPixels, displayMetrics.HeightPixels);
        }

        CurrentOrientation = (display.Rotation, naturalOrientation) switch
        {
            (_, ScreenOrientation.None) => ScreenOrientation.None,
            (SurfaceOrientation.Rotation0, ScreenOrientation.Landscape) => ScreenOrientation.Landscape,
            (SurfaceOrientation.Rotation90, ScreenOrientation.Landscape) => ScreenOrientation.Portrait,
            (SurfaceOrientation.Rotation180, ScreenOrientation.Landscape) => ScreenOrientation.LandscapeFlipped,
            (SurfaceOrientation.Rotation270, ScreenOrientation.Landscape) => ScreenOrientation.PortraitFlipped,
            (SurfaceOrientation.Rotation0, _) => ScreenOrientation.Portrait,
            (SurfaceOrientation.Rotation90, _) => ScreenOrientation.Landscape,
            (SurfaceOrientation.Rotation180, _) => ScreenOrientation.PortraitFlipped,
            (SurfaceOrientation.Rotation270, _) => ScreenOrientation.LandscapeFlipped,
            _ => ScreenOrientation.Portrait
        };
    }
}

internal sealed class AndroidScreens : ScreensBase<Display, AndroidScreen>, IDisposable
{
    private readonly Context _context;
    private readonly DisplayManager? _displayService;
    private readonly DisplayListener? _listener;

    public AndroidScreens(Context context) : base(new DisplayComparer())
    {
        _context = context;
        _displayService = context.GetSystemService(Context.DisplayService).JavaCast<DisplayManager>();
        if (_displayService is not null)
        {
            _listener = new DisplayListener(this);
            _displayService.RegisterDisplayListener(_listener, null);
        }
    }

    protected override IReadOnlyList<Display> GetAllScreenKeys()
    {
        if (_displayService?.GetDisplays() is { } displays)
        {
            return displays;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(30) && _context.Display is { } defaultDisplay)
        {
            return [defaultDisplay];
        }

        return Array.Empty<Display>();
    }

    protected override AndroidScreen CreateScreenFromKey(Display display) => new(display);

    protected override void ScreenChanged(AndroidScreen screen) => screen.Refresh(_context);

    protected override Screen? ScreenFromTopLevelCore(ITopLevelImpl topLevel)
    {
        var display = ((TopLevelImpl)topLevel).View.Display;
        return display is not null && TryGetScreen(display, out var screen) ? screen : null;
    }

    protected override Screen? ScreenFromPointCore(PixelPoint point) => null;
    protected override Screen? ScreenFromRectCore(PixelRect rect) => null;

    public void Dispose()
    {
        _displayService?.UnregisterDisplayListener(_listener);
        _displayService?.Dispose();
        _listener?.Dispose();
    }

    private class DisplayListener(AndroidScreens screens) : Java.Lang.Object, DisplayManager.IDisplayListener
    {
        public void OnDisplayAdded(int displayId) => screens.OnChanged();
        public void OnDisplayChanged(int displayId) => screens.OnChanged();
        public void OnDisplayRemoved(int displayId) => screens.OnChanged();
    }

    private class DisplayComparer : IEqualityComparer<Display>
    {
        public bool Equals(Display? x, Display? y) => x?.DisplayId == y?.DisplayId;
        public int GetHashCode(Display obj) => obj.DisplayId;
    }
}
