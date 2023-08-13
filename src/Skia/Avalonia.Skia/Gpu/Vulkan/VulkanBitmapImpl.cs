using System;
using System.IO;
using Avalonia.Platform;
using Avalonia.Vulkan;
using SkiaSharp;

namespace Avalonia.Skia.Vulkan;

internal class VulkanBitmapImpl : IDrawableBitmapImpl
{
    private readonly VulkanSkiaGpu _gpu;
    private readonly IVulkanBitmapSourceImage _image;

    public VulkanBitmapImpl(VulkanSkiaGpu gpu, IVulkanBitmapSourceImage image)
    {
        _gpu = gpu;
        _image = image;
    }

    public void Dispose()
    {
        _image.Dispose();
    }

    public Vector Dpi => new Vector(96 * _image.Scaling, 96 * _image.Scaling);
    public PixelSize PixelSize => _image.Info.PixelSize;
    public int Version => 1;
    public void Save(string fileName, int? quality = null) => throw new System.NotSupportedException();

    public void Save(Stream stream, int? quality = null) => throw new System.NotSupportedException();
    public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
    {
        var info = _image.Info;
        if (info.Handle == 0)
            return;
        var imageInfo = new GRVkImageInfo
        {
            CurrentQueueFamily = _gpu.Vulkan.Device.GraphicsQueueFamilyIndex,
            Format = info.Format,
            Image = (ulong)info.Handle,
            ImageLayout = info.Layout,
            ImageTiling = info.Tiling,
            ImageUsageFlags = info.UsageFlags,
            LevelCount = info.LevelCount,
            SampleCount = info.SampleCount,
            Protected = info.IsProtected,
            Alloc = new GRVkAlloc
            {
                Memory = (ulong)info.MemoryHandle,
                Size = info.MemorySize
            }
        };
        using (var backendTexture = new GRBackendRenderTarget(info.PixelSize.Width,
                   info.PixelSize.Height, (int)info.SampleCount, imageInfo))
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