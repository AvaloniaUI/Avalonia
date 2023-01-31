using System;

namespace Avalonia.Rendering;

/// <summary>
/// Represents the various types of overlays that can be drawn by a renderer.
/// </summary>
[Flags]
public enum RendererDebugOverlays
{
    /// <summary>
    /// Do not draw any overlay.
    /// </summary>
    None = 0,

    /// <summary>
    /// Draw a FPS counter.
    /// </summary>
    Fps = 1 << 0,

    /// <summary>
    /// Draw invalidated rectangles each frame.
    /// </summary>
    DirtyRects = 1 << 1,

    /// <summary>
    /// Draw a graph of past layout times.
    /// </summary>
    LayoutTimeGraph = 1 << 2,

    /// <summary>
    /// Draw a graph of past render times.
    /// </summary>
    RenderTimeGraph = 1 << 3
}
