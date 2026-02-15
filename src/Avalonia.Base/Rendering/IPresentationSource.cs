using System;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Rendering;

// This interface serves two purposes:
// 1) User-facing API (public members)
// 2) A way to provide PresentationSource APIs to Avalonia.Base from Avalonia.Controls
//    without cyclic references (internal members)
/// <summary>
/// Represents the host of the visual tree. On desktop platforms this is typically backed by a native window.
/// </summary>
[NotClientImplementable]
public interface IPresentationSource
{
    /// <summary>
    /// The current root of the visual tree
    /// </summary>
    public Visual? RootVisual { get; }
    
    /// <summary>
    /// The scaling factor to use in rendering.
    /// </summary>
    public double RenderScaling { get; }
    
    internal IPlatformSettings? PlatformSettings { get; }
    
    internal IRenderer Renderer { get; }
    
    internal IHitTester HitTester { get; }
    
    internal IInputRoot InputRoot { get; }

    internal ILayoutRoot LayoutRoot { get; }
    
    /// <summary>
    /// Gets the client size of the window.
    /// </summary>
    internal Size ClientSize { get; }

    internal PixelPoint PointToScreen(Point point);
    internal Point PointToClient(PixelPoint point);
}
