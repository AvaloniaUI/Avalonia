using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;

namespace Avalonia.Browser.Interop;

internal static partial class DomHelper
{
    [JSImport("AvaloniaDOM.getGlobalThis", AvaloniaModule.MainModuleName)]
    internal static partial JSObject GetGlobalThis();

    [JSImport("AvaloniaDOM.getFirstElementById", AvaloniaModule.MainModuleName)]
    internal static partial JSObject? GetElementById(string id, JSObject parent);

    [JSImport("AvaloniaDOM.getFirstElementByClassName", AvaloniaModule.MainModuleName)]
    internal static partial JSObject? GetElementsByClassName(string className, JSObject parent);

    [JSImport("AvaloniaDOM.createAvaloniaHost", AvaloniaModule.MainModuleName)]
    public static partial JSObject CreateAvaloniaHost(JSObject element);

    [JSImport("AvaloniaDOM.isFullscreen", AvaloniaModule.MainModuleName)]
    public static partial bool IsFullscreen(JSObject globalThis);

    [JSImport("AvaloniaDOM.setFullscreen", AvaloniaModule.MainModuleName)]
    public static partial Task SetFullscreen(JSObject globalThis, bool isFullscreen);

    [JSImport("AvaloniaDOM.getSafeAreaPadding", AvaloniaModule.MainModuleName)]
    public static partial double[] GetSafeAreaPadding(JSObject globalThis);

    [JSImport("AvaloniaDOM.getDarkMode", AvaloniaModule.MainModuleName)]
    public static partial int[] GetDarkMode(JSObject globalThis);

    [JSImport("AvaloniaDOM.addClass", AvaloniaModule.MainModuleName)]
    public static partial void AddCssClass(JSObject element, string className);

    [JSImport("AvaloniaDOM.initGlobalDomEvents", AvaloniaModule.MainModuleName)]
    public static partial void InitGlobalDomEvents(JSObject globalThis);

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

    [JSExport]
    public static Task ScreensChanged()
    {
        (AvaloniaLocator.Current.GetService<IScreenImpl>() as BrowserScreens)?.OnChanged();
        return Task.CompletedTask;
    }
}
