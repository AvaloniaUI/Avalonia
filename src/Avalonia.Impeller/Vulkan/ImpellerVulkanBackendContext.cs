using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Vulkan;
using NImpeller;

namespace Avalonia.Impeller;

/// <summary>
/// An Impeller context over Vulkan. Impeller creates its own instance and device, which are wrapped in an
/// Avalonia Vulkan context so the platform can create window surfaces on them.
/// </summary>
internal sealed class ImpellerVulkanBackendContext : ImpellerBackendContext
{
    private readonly Func<bool> _isLost;

    private ImpellerVulkanBackendContext(ImpellerContext impellerContext, IVulkanPlatformGraphicsContext context,
        Func<bool> isLost) : base(impellerContext)
    {
        VulkanContext = context;
        _isLost = isLost;
    }

    public static ImpellerVulkanBackendContext Create(IVulkanPlatformGraphicsContext platformContext,
        ImpellerOptions options) =>
        Create(platformContext.Instance,
            platformContext.TryGetFeature(typeof(IVulkanKhrSurfacePlatformSurfaceFactory)),
            () => platformContext.IsLost, options);

    // The loader only has to provide vkGetInstanceProcAddr.
    internal static ImpellerVulkanBackendContext Create(IVulkanInstance loader, object? surfaceFactory,
        Func<bool> isLost, ImpellerOptions options)
    {
        // Impeller creates swapchains at 1x1 and only resizes them when presenting reports them out of date,
        // which this platform never does.
        if (OperatingSystem.IsAndroid())
            throw new PlatformNotSupportedException(
                "Impeller can't size Vulkan swapchains on this platform. Use OpenGL ES rendering instead.");

        var impellerContext = ImpellerContext.CreateVulkanNew(
                (nint instance, string name) => loader.GetInstanceProcAddress(instance, name),
                options.EnableVulkanValidation)
            ?? throw new PlatformNotSupportedException("Failed to create an Impeller Vulkan context.");
        try
        {
            var info = impellerContext.GetVulkanInfo()
                ?? throw new PlatformNotSupportedException("Impeller didn't report its Vulkan device.");
            var device = new ImpellerVulkanDevice(info, new ImpellerVulkanInstance(info.Vk_instance, loader));

            var features = new Dictionary<Type, object>();
            if (surfaceFactory is not null)
                features[typeof(IVulkanKhrSurfacePlatformSurfaceFactory)] = surfaceFactory;

            return new ImpellerVulkanBackendContext(impellerContext,
                new Avalonia.Vulkan.VulkanContext(device, features), isLost);
        }
        catch
        {
            impellerContext.Dispose();
            throw;
        }
    }

    internal IVulkanPlatformGraphicsContext VulkanContext { get; }

    internal override IImpellerOffscreenLayer CreateTextureLayer(PixelSize size, Vector dpi) =>
        new ImpellerSkiaRasterLayer(size, dpi);

    public override IDrawingContextLayerImpl CreateOffscreenRenderTarget(PixelSize pixelSize, Vector scaling,
        bool enableTextAntialiasing) => new ImpellerRecordedLayer(pixelSize, scaling * 96);

    public override IRenderTarget CreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces)
    {
        var factory = (IVulkanKhrSurfacePlatformSurfaceFactory?)VulkanContext.TryGetFeature(
            typeof(IVulkanKhrSurfacePlatformSurfaceFactory));
        foreach (var surface in surfaces)
        {
            if (surface is IVulkanKhrSurfacePlatformSurface khr)
                return new ImpellerVulkanRenderTarget(this, khr, ownsSurface: false);
            if (factory?.CanRenderToSurface(VulkanContext, surface) == true)
                return new ImpellerVulkanRenderTarget(this, factory.CreateSurface(VulkanContext, surface),
                    ownsSurface: true);
        }

        throw new NotSupportedException("The Impeller Vulkan backend can't render to these surfaces.");
    }

    public override bool IsReadyToCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces) => true;

    public override bool IsLost => _isLost();
}
