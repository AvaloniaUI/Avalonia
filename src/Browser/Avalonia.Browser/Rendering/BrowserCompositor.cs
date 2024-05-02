using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;

namespace Avalonia.Browser.Rendering;

/// <summary>
/// We want to reuse timer/compositor instances per each AvaloniaView.
/// But at the same time, we want to keep possiblity of having different rendering modes (both software and webgl) at the same time.
/// For example, WebGL contexts number might exceed maximum allowed, or we might want to keep popups in software renderer. 
/// </summary>
internal static class BrowserCompositor
{
    private static BrowserRenderTimer? s_browserUiRenderTimer;
    private static BrowserRenderTimer BrowserUiRenderTimer => s_browserUiRenderTimer ??= new BrowserRenderTimer(false);

    private static Compositor? s_webGlUiCompositor, s_softwareUiCompositor;

    internal static Compositor WebGlUiCompositor => s_webGlUiCompositor ??= new Compositor(
        new RenderLoop(BrowserUiRenderTimer), AvaloniaLocator.Current.GetRequiredService<IPlatformGraphics>());

    internal static Compositor SoftwareUiCompositor => s_softwareUiCompositor ??= new Compositor(
        new RenderLoop(BrowserUiRenderTimer), null);
}
