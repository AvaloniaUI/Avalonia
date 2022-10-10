using System;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Web.Interop;

internal static partial class NativeControlHostHelper
{
    [JSImport("NativeControlHost.createDefaultChild", "avalonia")]
    internal static partial JSObject CreateDefaultChild(JSObject? parent);

    [JSImport("NativeControlHost.createAttachment", "avalonia")]
    internal static partial JSObject CreateAttachment();

    [JSImport("NativeControlHost.initializeWithChildHandle", "avalonia")]
    internal static partial void InitializeWithChildHandle(JSObject element, JSObject child);

    [JSImport("NativeControlHost.attachTo", "avalonia")]
    internal static partial void AttachTo(JSObject element, JSObject? host);

    [JSImport("NativeControlHost.showInBounds", "avalonia")]
    internal static partial void ShowInBounds(JSObject element, double x, double y, double width, double height);

    [JSImport("NativeControlHost.hideWithSize", "avalonia")]
    internal static partial void HideWithSize(JSObject element, double width, double height);

    [JSImport("NativeControlHost.releaseChild", "avalonia")]
    internal static partial void ReleaseChild(JSObject element);
}
