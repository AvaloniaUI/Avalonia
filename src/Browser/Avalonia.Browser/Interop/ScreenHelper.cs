using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Avalonia.Browser.Interop;

internal static partial class ScreenHelper
{
    [JSImport("ScreenHelper.subscribeOnChanged", AvaloniaModule.MainModuleName)]
    public static partial void SubscribeOnChanged(JSObject globalThis);
    
    [JSImport("ScreenHelper.checkPermissions", AvaloniaModule.MainModuleName)]
    public static partial void CheckPermissions(JSObject globalThis);

    [JSImport("ScreenHelper.getAllScreens", AvaloniaModule.MainModuleName)]
    public static partial JSObject[] GetAllScreens(JSObject globalThis);

    [JSImport("ScreenHelper.requestDetailedScreens", AvaloniaModule.MainModuleName)]
    [return: JSMarshalAs<JSType.Promise<JSType.Boolean>>]
    public static partial Task<bool> RequestDetailedScreens(JSObject globalThis);

    [JSImport("ScreenHelper.getDisplayName", AvaloniaModule.MainModuleName)]
    public static partial string GetDisplayName(JSObject screen);

    [JSImport("ScreenHelper.getScaling", AvaloniaModule.MainModuleName)]
    public static partial double GetScaling(JSObject screen);

    [JSImport("ScreenHelper.getBounds", AvaloniaModule.MainModuleName)]
    public static partial double[] GetBounds(JSObject screen);

    [JSImport("ScreenHelper.getWorkingArea", AvaloniaModule.MainModuleName)]
    public static partial double[] GetWorkingArea(JSObject screen);

    [JSImport("ScreenHelper.isCurrent", AvaloniaModule.MainModuleName)]
    public static partial bool IsCurrent(JSObject screen);

    [JSImport("ScreenHelper.isPrimary", AvaloniaModule.MainModuleName)]
    public static partial bool IsPrimary(JSObject screen);

    [JSImport("ScreenHelper.getCurrentOrientation", AvaloniaModule.MainModuleName)]
    public static partial int GetCurrentOrientation(JSObject screen);
}
