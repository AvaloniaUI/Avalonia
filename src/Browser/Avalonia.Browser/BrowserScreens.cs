using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Browser.Interop;
using Avalonia.Logging;
using Avalonia.Platform;
using BrowserScreenHelper = Avalonia.Browser.Interop.ScreenHelper;

namespace Avalonia.Browser;

internal sealed class BrowserScreen(JSObject screen) : PlatformScreen(new JSObjectPlatformHandle(screen))
{
    internal bool IsCurrent { get; set; }

    public void Refresh()
    {
        IsCurrent = BrowserScreenHelper.IsCurrent(screen);
        DisplayName = BrowserScreenHelper.GetDisplayName(screen);
        Scaling = BrowserScreenHelper.GetScaling(screen);
        IsPrimary = BrowserScreenHelper.IsPrimary(screen);
        CurrentOrientation = (ScreenOrientation)BrowserScreenHelper.GetCurrentOrientation(screen);
        Bounds = BrowserScreenHelper.GetBounds(screen) is { } boundsArr ?
            new PixelRect((int)boundsArr[0], (int)boundsArr[1], (int)boundsArr[2], (int)boundsArr[3]) :
            new PixelRect();
        WorkingArea = BrowserScreenHelper.GetWorkingArea(screen) is { } workingAreaArr ?
            new PixelRect((int)workingAreaArr[0], (int)workingAreaArr[1], (int)workingAreaArr[2],
                (int)workingAreaArr[3]) :
            new PixelRect();
    }
}

internal sealed class BrowserScreens : ScreensBase<JSObject, BrowserScreen>
{
    private bool _isExtended;

    public BrowserScreens()
    {
        BrowserScreenHelper.SubscribeOnChanged(BrowserWindowingPlatform.GlobalThis);
        BrowserScreenHelper.CheckPermissions(BrowserWindowingPlatform.GlobalThis);
    }

    protected override IReadOnlyList<JSObject> GetAllScreenKeys() =>
        BrowserScreenHelper.GetAllScreens(BrowserWindowingPlatform.GlobalThis);

    protected override BrowserScreen CreateScreenFromKey(JSObject key) => new(key);
    protected override void ScreenChanged(BrowserScreen screen) => screen.Refresh();

    protected override Screen? ScreenFromTopLevelCore(ITopLevelImpl topLevel) =>
        AllScreens.OfType<BrowserScreen>().FirstOrDefault(s => s.IsCurrent);

    protected override Screen? ScreenFromPointCore(PixelPoint point) =>
        _isExtended ? base.ScreenFromPointCore(point) : null;

    protected override Screen? ScreenFromRectCore(PixelRect rect) => _isExtended ? base.ScreenFromRectCore(rect) : null;

    protected override async Task<bool> RequestScreenDetailsCore()
    {
        try
        {
            return _isExtended = await BrowserScreenHelper.RequestDetailedScreens(BrowserWindowingPlatform.GlobalThis);
        }
        catch (Exception e)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.BrowserPlatform)?
                .Log(this, "Failed to get extended screen details: {Exception}", e);
            return false;
        }
    }
}
