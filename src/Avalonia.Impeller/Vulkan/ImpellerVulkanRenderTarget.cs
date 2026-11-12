using System;
using Avalonia.Platform;
using Avalonia.Vulkan;
using NImpeller;

namespace Avalonia.Impeller;

/// <summary>Presents through an Impeller swapchain over a window's VkSurfaceKHR.</summary>
internal sealed class ImpellerVulkanRenderTarget : IRenderTarget
{
    private readonly ImpellerVulkanBackendContext _context;
    private readonly IVulkanKhrSurfacePlatformSurface _surface;
    private readonly bool _ownsSurface;
    private ImpellerVulkanSwapchain? _swapchain;
    private PixelSize _swapchainSize;

    public ImpellerVulkanRenderTarget(ImpellerVulkanBackendContext context, IVulkanKhrSurfacePlatformSurface surface,
        bool ownsSurface)
    {
        _context = context;
        _surface = surface;
        _ownsSurface = ownsSurface;
    }

    public RenderTargetProperties Properties => new()
    {
        // Swapchain images don't keep their contents, so render directly and redraw everything each frame.
        RetainsPreviousFrameContents = true,
        IsSuitableForDirectRendering = true
    };

    public PlatformRenderTargetState PlatformRenderTargetState =>
        _surface.Size is { Width: > 0, Height: > 0 } ? PlatformRenderTargetState.Ready
            : PlatformRenderTargetState.NotReadyTryLater;

    public IDrawingContextImpl CreateDrawingContext(IRenderTarget.RenderTargetSceneInfo sceneInfo,
        out RenderTargetDrawingContextProperties properties)
    {
        properties = new RenderTargetDrawingContextProperties { PreviousFrameIsRetained = false };

        var size = _surface.Size;
        if (size.Width <= 0 || size.Height <= 0)
            throw new InvalidOperationException($"Can't create an Impeller drawing context for a {size} surface.");

        // The swapchain owns its surface and takes its size from it, so a resize needs a new surface.
        if (_swapchain is null || _swapchainSize != size)
        {
            _swapchain?.Dispose();
            _swapchain = null;
            var vkSurface = _surface.CreateSurface(_context.VulkanContext);
            _swapchain = _context.ImpellerContext.VulkanSwapchainCreateNew((nint)vkSurface)
                ?? throw new InvalidOperationException("Failed to create an Impeller Vulkan swapchain.");
            _swapchainSize = size;
        }

        var impellerSurface = _swapchain.AcquireNextSurfaceNew()
            ?? throw new InvalidOperationException("Failed to acquire an Impeller Vulkan swapchain image.");

        return new ImpellerDrawingContext(_context, impellerSurface, NoSession.Instance, size, isYFlipped: false,
            presentsSurface: true);
    }

    public void Dispose()
    {
        _swapchain?.Dispose();
        _swapchain = null;
        if (_ownsSurface)
            _surface.Dispose();
    }

    private sealed class NoSession : IDisposable
    {
        public static readonly NoSession Instance = new();

        public void Dispose()
        {
        }
    }
}
