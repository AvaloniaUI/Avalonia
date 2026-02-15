using System;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform;

namespace Avalonia.Rendering;

public interface IPresentationSource
{
    internal IPlatformSettings? PlatformSettings { get; }
    
    internal IRenderer Renderer { get; }
    
    internal IHitTester HitTester { get; }
    
    internal IInputRoot InputRoot { get; }

    internal ILayoutRoot LayoutRoot { get; }
    
    public Visual? RootVisual { get; }
    
    /// <summary>
    /// The scaling factor to use in rendering.
    /// </summary>
    double RenderScaling { get; }
    
    /// <summary>
    /// Gets the client size of the window.
    /// </summary>
    internal Size ClientSize { get; }

    internal PixelPoint PointToScreen(Point point);
    internal Point PointToClient(PixelPoint point);
}
