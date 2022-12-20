using System;
using Avalonia.Vulkan.Interop;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan;


internal class VulkanKhrRenderTarget : IVulkanRenderTarget
{
    private VulkanKhrSurface _khrSurface;
    private readonly IVulkanPlatformGraphicsContext _context;
    private VulkanDisplay _display;
    private VulkanImage? _image;
    private readonly IVulkanKhrSurfacePlatformSurface _platformSurface;
    public VkFormat Format { get; }
    public bool IsRgba { get; }

    public VulkanKhrRenderTarget(IVulkanKhrSurfacePlatformSurface surface, IVulkanPlatformGraphicsContext context)
    {
        _platformSurface = surface;
        _khrSurface = new(context, surface);
        _display = VulkanDisplay.CreateDisplay(context, _khrSurface);
        _context = context;
        IsRgba = _display.SurfaceFormat.format >= VkFormat.VK_FORMAT_R8G8B8A8_UNORM &&
                 _display.SurfaceFormat.format <= VkFormat.VK_FORMAT_R8G8B8A8_SRGB;

        // Skia seems to only create surfaces from images with unorm format
        Format = IsRgba ? VkFormat.VK_FORMAT_R8G8B8A8_UNORM : VkFormat.VK_FORMAT_B8G8R8A8_UNORM;
    }

    private void CreateImage()
    {
        _image = new VulkanImage(_context, _display.CommandBufferPool, Format, _display.Size);
    }

    private void DestroyImage()
    {
        _context.DeviceApi.DeviceWaitIdle(_context.DeviceHandle);
        _image?.Dispose();
        _image = null;
    }

    public void Dispose()
    {
        _context.DeviceApi.DeviceWaitIdle(_context.DeviceHandle);
        DestroyImage();
        _display?.Dispose();
        _display = null!;
        _khrSurface?.Dispose();
        _khrSurface = null!;
    }


    public IVulkanRenderSession BeginDraw()
    {
        var l = _context.EnsureCurrent();
        _display.CommandBufferPool.FreeUsedCommandBuffers();
        if (_display.EnsureSwapchainAvailable() || _image == null)
        {
            DestroyImage();
            CreateImage();
        }
        else
            _image.TransitionLayout(VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
                VkAccessFlags.VK_ACCESS_NONE);

        return new RenderingSession(_display, _image!, IsRgba, _platformSurface.Scaling, l);
    }

    public class RenderingSession : IVulkanRenderSession
    {
        private readonly VulkanImage _image;
        private readonly IDisposable _dispose;

        public RenderingSession(VulkanDisplay display, VulkanImage image, bool isRgba, double scaling,
            IDisposable dispose)
        {
            _image = image;
            _dispose = dispose;
            Display = display;
            IsRgba = isRgba;
            Scaling = scaling;
        }

        public VulkanDisplay Display { get; }
        public PixelSize Size => _image.Size;
        public double Scaling { get; }
        public bool IsYFlipped => true;
        public VulkanImageInfo ImageInfo => _image.ImageInfo;

        public bool IsRgba { get; }

        public void Dispose()
        {
            try
            {
                var commandBuffer = Display.StartPresentation();
                Display.BlitImageToCurrentImage(commandBuffer, _image);
                Display.EndPresentation(commandBuffer);
            }
            finally
            {
                _dispose.Dispose();
            }
        }
    }
}