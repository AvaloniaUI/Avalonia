using System.IO;
using Avalonia.Platform;
using Avalonia.Vulkan;
using SkiaSharp;

namespace Avalonia.Skia.Vulkan;

internal class VulkanBitmapImpl : IDrawableBitmapImpl
{
    private readonly VulkanSkiaGpu _gpu;
    private readonly VulkanImageInfo _info;

    public VulkanBitmapImpl(VulkanSkiaGpu gpu, VulkanImageInfo info, double scaling)
    {
        _gpu = gpu;
        _info = info;
        Dpi = new Vector(96 * scaling, 96 * scaling);
    }

    public void Dispose()
    {
        
    }

    public Vector Dpi { get; }
    public PixelSize PixelSize => _info.PixelSize;
    public int Version => 1;
    public void Save(string fileName, int? quality = null) => throw new System.NotSupportedException();

    public void Save(Stream stream, int? quality = null) => throw new System.NotSupportedException();
    public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
    {
        _gpu.Vulkan.MainQueueWaitIdle();
        var imageInfo = new GRVkImageInfo
        {
            CurrentQueueFamily = _gpu.Vulkan.Device.GraphicsQueueFamilyIndex,
            Format = _info.Format,
            Image = (ulong)_info.Handle,
            ImageLayout = _info.Layout,
            ImageTiling = _info.Tiling,
            ImageUsageFlags = _info.UsageFlags,
            LevelCount = _info.LevelCount,
            SampleCount = _info.SampleCount,
            Protected = _info.IsProtected,
            Alloc = new GRVkAlloc
            {
                Memory = (ulong)_info.MemoryHandle,
                Size = _info.MemorySize
            }
        };
        using (var backendTexture = new GRBackendRenderTarget(_info.PixelSize.Width,
                   _info.PixelSize.Height, (int)_info.SampleCount, imageInfo))
        using (var surface = SKSurface.Create(_gpu.GrContext, backendTexture, GRSurfaceOrigin.TopLeft,
                   SKColorType.Bgra8888, SKColorSpace.CreateSrgb()))
        {
            if (surface == null)
                return;
            using(var snapshot = surface.Snapshot())
                context.Canvas.DrawImage(snapshot, sourceRect, destRect);
        }
    }
}