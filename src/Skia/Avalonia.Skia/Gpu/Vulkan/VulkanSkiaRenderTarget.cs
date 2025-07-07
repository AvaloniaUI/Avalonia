using System;
using Avalonia.Skia.Helpers;
using Avalonia.Vulkan;
using SkiaSharp;

namespace Avalonia.Skia.Vulkan;

class VulkanSkiaRenderTarget : ISkiaGpuRenderTarget
{
    private readonly VulkanSkiaGpu _gpu;
    private readonly IVulkanRenderTarget _target;

    public VulkanSkiaRenderTarget(VulkanSkiaGpu gpu, IVulkanRenderTarget target)
    {
        _gpu = gpu;
        _target = target;
    }

    public void Dispose()
    {
        _target.Dispose();
    }

    public ISkiaGpuRenderSession BeginRenderingSession()
    {
        var session = _target.BeginDraw();
        bool success = false;
        try
        {
            var size = session.Size;
            var scaling = session.Scaling;
            if (size.Width <= 0 || size.Height <= 0 || scaling < 0)
            {
                session.Dispose();
                throw new InvalidOperationException(
                    $"Can't create drawing context for surface with {size} size and {scaling} scaling");
            }
            _gpu.GrContext.ResetContext();
            var sessionImageInfo = session.ImageInfo;
            var imageInfo = new GRVkImageInfo
            {
                CurrentQueueFamily = _gpu.Vulkan.Device.GraphicsQueueFamilyIndex,
                Format = sessionImageInfo.Format,
                Image = (ulong)sessionImageInfo.Handle,
                ImageLayout = sessionImageInfo.Layout,
                ImageTiling = sessionImageInfo.Tiling,
                ImageUsageFlags = sessionImageInfo.UsageFlags,
                LevelCount = sessionImageInfo.LevelCount,
                SampleCount = sessionImageInfo.SampleCount,
                Protected = sessionImageInfo.IsProtected,
                Alloc = new GRVkAlloc
                {
                    Memory = (ulong)sessionImageInfo.MemoryHandle,
                    Size = sessionImageInfo.MemorySize
                }
            };
            using var renderTarget = new GRBackendRenderTarget(size.Width, size.Height, imageInfo);
            var surface = SKSurface.Create(_gpu.GrContext, renderTarget,
                session.IsYFlipped ? GRSurfaceOrigin.TopLeft : GRSurfaceOrigin.BottomLeft,
                session.IsRgba ? SKColorType.Rgba8888 : SKColorType.Bgra8888, SKColorSpace.CreateSrgb());
            
            if (surface == null)
                throw new InvalidOperationException(
                    $"Surface can't be created with the provided render target");
            success = true;
            return new VulkanSkiaRenderSession(_gpu.GrContext, surface, session);
        }
        finally
        {
            if(!success)
                session.Dispose();
        }
    }
   
    public bool IsCorrupted => false;


    internal class VulkanSkiaRenderSession : ISkiaGpuRenderSession
    {
        private readonly IVulkanRenderSession _vulkanSession;

        public VulkanSkiaRenderSession(GRContext grContext,
            SKSurface surface,
            IVulkanRenderSession vulkanSession)
        {
            GrContext = grContext;
            SkSurface = surface;
            _vulkanSession = vulkanSession;
            SurfaceOrigin = vulkanSession.IsYFlipped ? GRSurfaceOrigin.TopLeft : GRSurfaceOrigin.BottomLeft;
        }

        public void Dispose()
        {
            SkSurface.Canvas.Flush();
            SkSurface.Dispose();
            GrContext.Flush();
            _vulkanSession.Dispose();
        }

        public GRContext GrContext { get; }
        public SKSurface SkSurface { get; }
        public double ScaleFactor => _vulkanSession.Scaling;
        public GRSurfaceOrigin SurfaceOrigin { get; }
    }
}
