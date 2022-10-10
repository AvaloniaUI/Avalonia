using System;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Web.Interop;

internal static partial class DomHelper
{
    [JSImport("globalThis.document.getElementById")]
    internal static partial JSObject? GetElementById(string id);

    [JSImport("AvaloniaDOM.createAvaloniaHost", "avalonia")]
    public static partial JSObject CreateAvaloniaHost(JSObject element);

    [JSImport("AvaloniaDOM.addClass", "avalonia")]
    public static partial void AddCssClass(JSObject element, string className);

    [JSImport("SizeWatcher.observe", "avalonia")]
    public static partial JSObject ObserveSize(
        JSObject canvas,
        string? canvasId,
        [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>]
        Action<int, int> onSizeChanged);

    [JSImport("DpiWatcher.start", "avalonia")]
    public static partial double ObserveDpi(
       [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>]
        Action<double, double> onDpiChanged);
}
