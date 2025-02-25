using System;
using System.Diagnostics;

// ReSharper disable ExplicitCallerInfoArgument

namespace Avalonia.Diagnostics;

internal static partial class Diagnostic
{
    private static readonly ActivitySource s_diagnostic = new("Avalonia.Diagnostic.Source");

    public static Activity? FindingResourceActivity() => s_diagnostic
        .StartActivity("Avalonia.Controls.ResourceNode.FindResource");

}
