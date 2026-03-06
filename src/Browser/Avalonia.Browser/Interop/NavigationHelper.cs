using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Browser.Interop;

internal static partial class NavigationHelper
{
    [JSImport("NavigationHelper.addBackHandler", AvaloniaModule.MainModuleName)]
    public static partial void AddBackHandler([JSMarshalAs<JSType.Function<JSType.Boolean>>] Func<bool> backHandlerCallback);

    public static Task<bool> OnBackRequested()
    {
        var handled = (AvaloniaLocator.Current.GetService<ISystemNavigationManagerImpl>() as BrowserSystemNavigationManagerImpl)?
            .OnBackRequested() ?? false;
        return Task.FromResult(handled);
    }

    [JSImport("NavigationHelper.openUri", AvaloniaModule.MainModuleName)]
    public static partial bool WindowOpen(string uri, string target);
}
