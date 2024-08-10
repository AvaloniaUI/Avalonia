using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Avalonia.iOS;

internal class iOSScreen(UIScreen screen) : PlatformScreen(new PlatformHandle(screen.Handle.Handle, nameof(UIScreen)))
{
    public void Refresh()
    {
        IsPrimary = screen.Equals(UIScreen.MainScreen);
        Scaling = screen.NativeScale;
        DisplayName = IsPrimary ? nameof(UIScreen.MainScreen) : null;

        var nativeBounds = screen.NativeBounds;
        var scaledBounds = screen.Bounds;

#if !TVOS && !MACCATALYST
        var uiOrientation = IsPrimary ?
            UIDevice.CurrentDevice.Orientation :
            UIDeviceOrientation.LandscapeLeft;
        CurrentOrientation = uiOrientation switch
        {
            UIDeviceOrientation.Portrait => ScreenOrientation.Portrait,
            UIDeviceOrientation.PortraitUpsideDown => ScreenOrientation.PortraitFlipped,
            UIDeviceOrientation.LandscapeLeft => ScreenOrientation.Landscape,
            UIDeviceOrientation.LandscapeRight => ScreenOrientation.LandscapeFlipped,
            UIDeviceOrientation.FaceUp or UIDeviceOrientation.FaceDown =>
                nativeBounds.Width > nativeBounds.Height ? ScreenOrientation.Landscape : ScreenOrientation.Portrait,
            _ => ScreenOrientation.None
        };
#endif

        // "The bounding rectangle of the physical screen, measured in pixels" - so just cast it to int.
        // "This value does not change as the device rotates." - we need to rotate it to match other platforms.
        // As a reference, scaled bounds are always rotated.
        WorkingArea = Bounds = scaledBounds.Width > scaledBounds.Height && nativeBounds.Width < nativeBounds.Height ?
            new PixelRect((int)nativeBounds.X, (int)nativeBounds.Y, (int)nativeBounds.Height, (int)nativeBounds.Width) :
            new PixelRect((int)nativeBounds.X, (int)nativeBounds.Y, (int)nativeBounds.Width, (int)nativeBounds.Height);
    }
}

internal class iOSScreens : ScreensBase<UIScreen, iOSScreen>
{
    public iOSScreens()
    {
        UIScreen.Notifications.ObserveDidConnect(OnScreenChanged);
        UIScreen.Notifications.ObserveDidDisconnect(OnScreenChanged);
        UIScreen.Notifications.ObserveModeDidChange(OnScreenChanged);
#if !TVOS
        UIDevice.Notifications.ObserveOrientationDidChange(OnScreenChanged);
#endif

        void OnScreenChanged(object? sender, NSNotificationEventArgs e) => OnChanged();
    }

    protected override IReadOnlyList<UIScreen> GetAllScreenKeys() => UIScreen.Screens;

    protected override iOSScreen CreateScreenFromKey(UIScreen key) => new(key);

    protected override void ScreenChanged(iOSScreen screen) => screen.Refresh();

    protected override Screen? ScreenFromPointCore(PixelPoint point) => null;

    protected override Screen? ScreenFromRectCore(PixelRect rect) => null;

    protected override Screen? ScreenFromTopLevelCore(ITopLevelImpl topLevel)
    {
        var uiScreen = (topLevel as AvaloniaView.TopLevelImpl)?.View.Window.Screen;
        return uiScreen is not null && TryGetScreen(uiScreen, out var screen) ? screen : null;
    }
}
