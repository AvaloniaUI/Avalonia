using System;

namespace Avalonia.X11.Selections;

internal static class SelectionHelper
{
    public static TimeSpan Timeout { get; } = TimeSpan.FromSeconds(5);
}
