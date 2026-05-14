using System.Diagnostics.CodeAnalysis;
using Avalonia.Metadata;

namespace Avalonia.Platform;

/// <summary>
/// Describes the current readiness state of a platform render target.
/// Flows through the entire rendering pipeline from platform to compositor.
/// </summary>
[PrivateApi]
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Private API, not meant to be compared")]
public readonly struct PlatformRenderTargetState
{
    /// <summary>
    /// Indicates if the render target is currently ready to be rendered to.
    /// </summary>
    public bool IsReady { get; init; }
    
    /// <summary>
    /// Indicates if the render target is no longer usable and needs to be recreated
    /// </summary>
    public bool IsCorrupted { get; init; }
    
    /// <summary>
    /// A readiness state indicating the target is ready to render.
    /// </summary>
    public static PlatformRenderTargetState Ready => new() { IsReady = true };

    public static PlatformRenderTargetState NotReadyTryLater => default;
    
    public static PlatformRenderTargetState Corrupted => new() { IsCorrupted = true, IsReady = true};

    public static PlatformRenderTargetState Disposed => new() { IsCorrupted = true};
}

[PrivateApi]
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Private API, not meant to be compared")]
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
[SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Private API, not meant to be compared")]
public struct RenderTargetDrawingContextProperties
{
    /// <summary>
    /// Indicates that the drawing context targets a surface that preserved its contents since the previous frame
    /// </summary>
    public bool PreviousFrameIsRetained { get; init; }
}
