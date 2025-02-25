using System;
using System.Diagnostics;

// ReSharper disable ExplicitCallerInfoArgument

namespace Avalonia.Diagnostics;

internal static partial class Diagnostic
{
    private static readonly ActivitySource s_diagnostic = new("Avalonia.Diagnostic.Source");
}
