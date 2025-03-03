using System;
using System.Diagnostics;

// ReSharper disable ExplicitCallerInfoArgument

namespace Avalonia.Diagnostics;

internal static partial class Diagnostic
{
    private static readonly ActivitySource s_diagnostic = new("Avalonia.Diagnostic.Source");

    public static Activity? AttachingStyle() => s_diagnostic
        .StartActivity("Avalonia.AttachingStyle");

    public static Activity? FindingResource() => s_diagnostic
        .StartActivity("Avalonia.FindingResource");

    public static Activity? EvaluatingStyle() => s_diagnostic
        .StartActivity("Avalonia.EvaluatingStyle");

    public static Activity? MeasuringLayoutable() => s_diagnostic
        .StartActivity("Avalonia.MeasuringLayoutable");

    public static Activity? ArrangingLayoutable() => s_diagnostic
        .StartActivity("Avalonia.ArrangingLayoutable");

    public static Activity? PerformingHitTest() => s_diagnostic
        .StartActivity("Avalonia.PerformingHitTest");

    public static Activity? RaisingRoutedEvent() => s_diagnostic
        .StartActivity("Avalonia.RaisingRoutedEvent");
}
