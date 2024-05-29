﻿using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;

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

    [JSImport("AvaloniaDOM.getDarkMode", AvaloniaModule.MainModuleName)]
    public static partial int[] GetDarkMode();

    [JSImport("AvaloniaDOM.addClass", AvaloniaModule.MainModuleName)]
    public static partial void AddCssClass(JSObject element, string className);

    [JSImport("globalThis.document.visibilityState")]
    public static partial string GetCurrentDocumentVisibility();

    [JSImport("AvaloniaDOM.initGlobalDomEvents", AvaloniaModule.MainModuleName)]
    public static partial void InitGlobalDomEvents();

    [JSExport]
    public static Task DarkModeChanged(bool isDarkMode, bool isHighContrast)
    {
        (AvaloniaLocator.Current.GetService<IPlatformSettings>() as BrowserPlatformSettings)?.OnValuesChanged(isDarkMode, isHighContrast);
        return Task.CompletedTask;
    }

    [JSExport]
    public static Task DocumentVisibilityChanged(string visibilityState)
    {
        (AvaloniaLocator.Current.GetService<IActivatableLifetime>() as BrowserActivatableLifetime)?.OnVisibilityStateChanged(visibilityState);
        return Task.CompletedTask;
    }
}
