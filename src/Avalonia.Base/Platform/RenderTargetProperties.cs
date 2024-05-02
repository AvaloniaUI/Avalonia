using Avalonia.Metadata;

namespace Avalonia.Platform;

[PrivateApi]
public struct RenderTargetProperties
{
    /// <summary>
    /// Indicates that render target contents are preserved between CreateDrawingContext calls.
    /// Notable examples are retained CPU-memory framebuffers and
    /// swapchains with DXGI_SWAP_EFFECT_SEQUENTIAL/DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL
    /// </summary>
    public bool RetainsPreviousFrameContents { get; init; }
    
    /// <summary>
    /// Indicates that the render target can be used without CreateLayer
    /// It's currently not true for every render target, since with OpenGL rendering we often use
    /// framebuffers without a stencil attachment that is required for clipping with Skia 
    /// </summary>
    public bool IsSuitableForDirectRendering { get; init; }
}

[PrivateApi]
public struct RenderTargetDrawingContextProperties
{
    /// <summary>
    /// Indicates that the drawing context targets a surface that preserved its contents since the previous frame
    /// </summary>
    public bool PreviousFrameIsRetained { get; init; }
}