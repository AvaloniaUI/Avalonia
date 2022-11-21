using System;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Browser.Interop;

internal static partial class NativeControlHostHelper
{
    [JSImport("NativeControlHost.createDefaultChild", AvaloniaModule.MainModuleName)]
    internal static partial JSObject CreateDefaultChild(JSObject? parent);

    [JSImport("NativeControlHost.createAttachment", AvaloniaModule.MainModuleName)]
    internal static partial JSObject CreateAttachment();

    [JSImport("NativeControlHost.initializeWithChildHandle", AvaloniaModule.MainModuleName)]
    internal static partial void InitializeWithChildHandle(JSObject element, JSObject child);

    [JSImport("NativeControlHost.attachTo", AvaloniaModule.MainModuleName)]
    internal static partial void AttachTo(JSObject element, JSObject? host);

    [JSImport("NativeControlHost.showInBounds", AvaloniaModule.MainModuleName)]
    internal static partial void ShowInBounds(JSObject element, double x, double y, double width, double height);

    [JSImport("NativeControlHost.hideWithSize", AvaloniaModule.MainModuleName)]
    internal static partial void HideWithSize(JSObject element, double width, double height);

    [JSImport("NativeControlHost.releaseChild", AvaloniaModule.MainModuleName)]
    internal static partial void ReleaseChild(JSObject element);
}
