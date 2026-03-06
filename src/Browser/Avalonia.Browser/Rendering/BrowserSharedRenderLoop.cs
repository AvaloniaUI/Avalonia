using System;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;

namespace Avalonia.Browser.Rendering;

internal static class BrowserSharedRenderLoop
{
    private static BrowserRenderTimer? s_browserUiRenderTimer;
    public static BrowserRenderTimer RenderTimer => s_browserUiRenderTimer ??= new BrowserRenderTimer(false);
    public static Lazy<RenderLoop> RenderLoop = new(() => new RenderLoop(RenderTimer), true);
}
