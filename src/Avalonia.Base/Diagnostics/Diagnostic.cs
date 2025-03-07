using System;

namespace Avalonia.Diagnostics;

internal static partial class Diagnostic
{
    public static bool IsEnabled { get; }

    private static bool InitializeIsEnabled() => AppContext.TryGetSwitch("Avalonia.Diagnostics.Diagnostic.IsEnabled", out var isEnabled) && isEnabled;

    static Diagnostic()
    {
        IsEnabled = InitializeIsEnabled();
        if (!IsEnabled)
        {
            return;
        }

        InitActivitySource();
        InitMetrics();
    }
}
