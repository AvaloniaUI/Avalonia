using System.Diagnostics;

// ReSharper disable ExplicitCallerInfoArgument

namespace Avalonia.Diagnostics;

internal static partial class Diagnostic
{
    private static ActivitySource? s_activitySource;

    public static void InitActivitySource()
    {
        s_activitySource = new("Avalonia.Diagnostic.Source");
    }

    private static Activity? StartActivity(string name) => s_activitySource?.StartActivity(name);

    public static Activity? AttachingStyle() => StartActivity("Avalonia.AttachingStyle");
    public static Activity? FindingResource() => StartActivity("Avalonia.FindingResource");
    public static Activity? EvaluatingStyle() => StartActivity("Avalonia.EvaluatingStyle");
    public static Activity? MeasuringLayoutable() => StartActivity("Avalonia.MeasuringLayoutable");
    public static Activity? ArrangingLayoutable() => StartActivity("Avalonia.ArrangingLayoutable");
    public static Activity? PerformingHitTest() => StartActivity("Avalonia.PerformingHitTest");
    public static Activity? RaisingRoutedEvent() => StartActivity("Avalonia.RaisingRoutedEvent");
}
