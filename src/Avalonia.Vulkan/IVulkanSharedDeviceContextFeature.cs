using System;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;

namespace Avalonia.Vulkan;
public interface IVulkanSharedDeviceGraphicsContextFeature
{
    public IVulkanSharedDevice? SharedDevice { get; }
    [Unstable]
    public IBitmapImpl CreateBitmapFromVulkanImage(IVulkanBitmapSourceImage image);
}

[Unstable, NotClientImplementable]
public interface IVulkanBitmapSourceImage : IDisposable
{
    VulkanImageInfo Info { get; }
    double Scaling { get; }
}

[Unstable, NotClientImplementable]
public interface IVulkanSharedDevice
{
    IVulkanDevice Device { get; }
}

public class VulkanBitmap : Bitmap
{
    private readonly IBitmapImpl _impl;
    public VulkanBitmap(IBitmapImpl impl) : base(impl)
    {
        _impl = impl;
    }
}