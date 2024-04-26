using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Skia;
using Avalonia.Rendering.Composition;

namespace Avalonia.Browser.Rendering;

internal class RenderTargetBrowserSurface : BrowserSurface
{
    public RenderTargetBrowserSurface(JSObject jsSurface, Compositor compositor) : base(jsSurface, compositor)
    {
    }
}