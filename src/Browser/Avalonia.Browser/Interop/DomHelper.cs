using System;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Browser.Interop;

internal static partial class DomHelper
{
    [JSImport("globalThis.document.getElementById")]
    internal static partial JSObject? GetElementById(string id);

    [JSImport("AvaloniaDOM.getFirstElementByClassName", AvaloniaModule.MainModuleName)]
    internal static partial JSObject? GetElementsByClassName(string className, JSObject? parent);

    [JSImport("AvaloniaDOM.createAvaloniaHost", AvaloniaModule.MainModuleName)]
    public static partial JSObject CreateAvaloniaHost(JSObject element);

    [JSImport("AvaloniaDOM.isFullscreen", AvaloniaModule.MainModuleName)]
    public static partial bool IsFullscreen();

    [JSImport("AvaloniaDOM.setFullscreen", AvaloniaModule.MainModuleName)]
    public static partial JSObject SetFullscreen(bool isFullscreen);

    [JSImport("AvaloniaDOM.getSafeAreaPadding", AvaloniaModule.MainModuleName)]
    public static partial double[] GetSafeAreaPadding();

    [JSImport("AvaloniaDOM.initSafeAreaPadding", AvaloniaModule.MainModuleName)]
    public static partial void InitSafeAreaPadding();

    [JSImport("AvaloniaDOM.addClass", AvaloniaModule.MainModuleName)]
    public static partial void AddCssClass(JSObject element, string className);

    [JSImport("AvaloniaDOM.observeDarkMode", AvaloniaModule.MainModuleName)]
    public static partial JSObject ObserveDarkMode(
        [JSMarshalAs<JSType.Function<JSType.Boolean, JSType.Boolean>>]
        Action<bool, bool> observer);
}
