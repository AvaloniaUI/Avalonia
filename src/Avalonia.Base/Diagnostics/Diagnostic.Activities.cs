using System;
using System.Diagnostics;

// ReSharper disable ExplicitCallerInfoArgument

namespace Avalonia.Diagnostics;

internal static partial class Diagnostic
{
    private static readonly ActivitySource s_diagnostic = new("Avalonia.Diagnostic.Source");

    public static Activity? AttachingStyleActivity() => s_diagnostic
        .StartActivity("Avalonia.Styling.Style.Attach");

    public static Activity? FindingResourceActivity() => s_diagnostic
        .StartActivity("Avalonia.Controls.ResourceNode.FindResource");

    public static Activity? EvaluatingStyleActivator() => s_diagnostic
        .StartActivity("Avalonia.Styling.Activators.StyleActivatorBase.EvaluateIsActive");

    public static Activity? MeasuingLayoutable() => s_diagnostic
        .StartActivity("Avalonia.Layout.Layoutable.Measure");

    public static Activity? ArrangingLayoutable() => s_diagnostic
        .StartActivity("Avalonia.Layout.Layoutable.Arrange");

    public static Activity? PerformingHitTest() => s_diagnostic
        .StartActivity("Avalonia.Rendering.HitTest");
}
