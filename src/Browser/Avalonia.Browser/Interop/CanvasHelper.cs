using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Browser.Interop;

internal record GLInfo(int ContextId, uint FboId, int Stencils, int Samples, int Depth);

internal static partial class CanvasHelper
{
    [JSImport("CanvasSurface.onSizeChanged", AvaloniaModule.MainModuleName)]
    public static partial void OnSizeChanged(
        JSObject canvasSurface,
        [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number, JSType.Number>>]
        // TODO: this callback should be <int, int, double>. Revert after next .NET 9 preview.  
        Action<double, double, double> onSizeChanged);

    [JSImport("CanvasSurface.create", AvaloniaModule.MainModuleName)]
    public static partial JSObject CreateRenderTargetSurface(JSObject canvasSurface, int[] modes, int threadId);

    [JSImport("CanvasSurface.destroy", AvaloniaModule.MainModuleName)]
    public static partial void Destroy(JSObject canvasSurface);
}
