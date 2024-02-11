using System;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Browser.Interop;

internal static partial class NavigationHelper
{
    [JSImport("NavigationHelper.addBackHandler", AvaloniaModule.MainModuleName)]
    public static partial void AddBackHandler([JSMarshalAs<JSType.Function<JSType.Boolean>>] Func<bool> backHandlerCallback);

    [JSImport("window.open")]
    public static partial JSObject? WindowOpen(string uri, string target);
}
