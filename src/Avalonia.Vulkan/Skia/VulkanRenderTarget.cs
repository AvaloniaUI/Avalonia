using System;
using System.Threading;
using Avalonia.Skia;
using Avalonia.Vulkan.Surfaces;
using SkiaSharp;

namespace Avalonia.Vulkan.Skia
{
    internal class VulkanRenderTarget : ISkiaGpuRenderTarget
    {
        internal GRContext GrContext { get; set; }
        
        private readonly VulkanSurfaceRenderTarget _surface;
        private readonly IVulkanPlatformSurface _vulkanPlatformSurface;

        public VulkanRenderTarget(VulkanPlatformInterface vulkanPlatformInterface,
            IVulkanPlatformSurface vulkanPlatformSurface)
        {
            _surface = vulkanPlatformInterface.CreateRenderTarget(vulkanPlatformSurface);
            _vulkanPlatformSurface = vulkanPlatformSurface;
        }

        public void Dispose()
        {
            _surface.Dispose();
        }

        public ISkiaGpuRenderSession BeginRenderingSession()
        {
            var session = _surface.BeginDraw(_vulkanPlatformSurface.Scaling);
            bool success = false;
            try
            {
                var disp = session.Display;
                var api = session.Api;

                var size = session.Size;
                var scaling = session.Scaling;
                if (size.Width <= 0 || size.Height <= 0 || scaling < 0)
                {
                    session.Dispose();
                    throw new InvalidOperationException(
                        $"Can't create drawing context for surface with {size} size and {scaling} scaling");
                }

                lock (GrContext)
                {
                    GrContext.ResetContext();

                    var imageInfo = new GRVkImageInfo()
                    {
                        CurrentQueueFamily = disp.QueueFamilyIndex,
                        Format = _surface.ImageFormat,
                        Image = _surface.Image.Handle,
                        ImageLayout = (uint)_surface.Image.CurrentLayout,
                        ImageTiling = (uint)_surface.Image.Tiling,
                        ImageUsageFlags = _surface.UsageFlags,
                        LevelCount = _surface.MipLevels,
                        SampleCount = 1,
                        Protected = false,
                        Alloc = new GRVkAlloc()
                        {
                            Memory = _surface.Image.MemoryHandle,
                            Flags = 0,
                            Offset = 0,
                            Size = _surface.MemorySize
                        }
                    };

                    var renderTarget =
                        new GRBackendRenderTarget((int)size.Width, (int)size.Height, 1,
                            imageInfo);
                    var surface = SKSurface.Create(GrContext, renderTarget,
                        session.IsYFlipped ? GRSurfaceOrigin.TopLeft : GRSurfaceOrigin.BottomLeft,
                        _surface.IsRgba ? SKColorType.Rgba8888 : SKColorType.Bgra8888, SKColorSpace.CreateSrgb());

                    if (surface == null)
                        throw new InvalidOperationException(
                            $"Surface can't be created with the provided render target");

                    success = true;

                    return new VulkanGpuSession(GrContext, renderTarget, surface, session);
                }
            }
            finally
            {
                if (!success)
                    session.Dispose();
            }
        }

        public bool IsCorrupted { get; }

        internal class VulkanGpuSession : ISkiaGpuRenderSession
        {
            private readonly GRBackendRenderTarget _backendRenderTarget;
            private readonly VulkanSurfaceRenderingSession _vulkanSession;

            public VulkanGpuSession(GRContext grContext,
                GRBackendRenderTarget backendRenderTarget,
                SKSurface surface,
                VulkanSurfaceRenderingSession vulkanSession)
            {
                GrContext = grContext;
                _backendRenderTarget = backendRenderTarget;
                SkSurface = surface;
                _vulkanSession = vulkanSession;

                SurfaceOrigin = vulkanSession.IsYFlipped ? GRSurfaceOrigin.TopLeft : GRSurfaceOrigin.BottomLeft;

                Monitor.Enter(_vulkanSession.Display.Lock);
            }

            public void Dispose()
            {
                SkSurface.Canvas.Flush();

                SkSurface.Dispose();
                _backendRenderTarget.Dispose();
                GrContext.Flush();

                _vulkanSession.Dispose();

                Monitor.Exit(_vulkanSession.Display.Lock);
            }

            public GRContext GrContext { get; }
            public SKSurface SkSurface { get; }
            public double ScaleFactor => _vulkanSession.Scaling;
            public GRSurfaceOrigin SurfaceOrigin { get; }
        }
    }
}
