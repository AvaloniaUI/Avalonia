using Tizen.System;
using ElmSharp;

namespace Avalonia.Tizen.Platform;
public static class ScalingInfo
{
    private static readonly Lazy<string> profile = new Lazy<string>(() => Elementary.GetProfile());

    private static readonly Lazy<int> dpi = new Lazy<int>(() =>
    {
        // TV has fixed DPI value (72)
        if (Profile == "tv")
            return 72;

#pragma warning disable CS0618 // Type or member is obsolete
        SystemInfo.TryGetValue("http://tizen.org/feature/screen.dpi", out int dpi);
#pragma warning restore CS0618 // Type or member is obsolete
        return dpi;
    });

    // allows to convert pixels to Android-style device-independent pixels
    private static readonly Lazy<double> scalingFactor = new Lazy<double>(() => dpi.Value / 160.0);

    private static double? scalingFactorOverride;

    public static string Profile => profile.Value;

    public static int Dpi => dpi.Value;

    public static double ScalingFactor => scalingFactorOverride ?? scalingFactor.Value;

    public static double FromPixel(double v) => v / ScalingFactor;

    public static double ToPixel(double v) => v * ScalingFactor;

    public static void SetScalingFactor(double? scalingFactor)
    {
        scalingFactorOverride = scalingFactor;
    }
}
