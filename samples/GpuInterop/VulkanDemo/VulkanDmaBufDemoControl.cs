using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Rendering.Composition;

namespace GpuInterop.VulkanDemo;

// Renders the spinning teapot with Vulkan and presents it to the EGL/OpenGL compositor as a Linux
// dma-buf, exercising Avalonia.OpenGL's EglExternalObjectsFeature (EGL_EXT_image_dma_buf_import).
// Run with `--dmabuf` so the X11 backend is configured to use the EGL compositor.
public class VulkanDmaBufDemoControl : DrawingSurfaceDemoBase
{
    class VulkanResources : IAsyncDisposable
    {
        public VulkanContext Context { get; }
        public VulkanDmaBufSwapchain Swapchain { get; }
        public VulkanContent Content { get; }

        public VulkanResources(VulkanContext context, VulkanDmaBufSwapchain swapchain, VulkanContent content)
        {
            Context = context;
            Swapchain = swapchain;
            Content = content;
        }

        public async ValueTask DisposeAsync()
        {
            Context.Pool.FreeUsedCommandBuffers();
            Content.Dispose();
            await Swapchain.DisposeAsync();
            Context.Dispose();
        }
    }

    protected override bool SupportsDisco => true;

    private VulkanResources? _resources;

    protected override (bool success, string info) InitializeGraphicsResources(Compositor compositor,
        CompositionDrawingSurface compositionDrawingSurface, ICompositionGpuInterop gpuInterop)
    {
        if (!OperatingSystem.IsLinux())
            return (false, "The dma-buf demo is only supported on Linux");

        var (context, info) = VulkanContext.TryCreate(gpuInterop, dmaBuf: true);
        if (context == null)
            return (false, info);
        try
        {
            var content = new VulkanContent(context);
            _resources = new VulkanResources(context,
                new VulkanDmaBufSwapchain(context, gpuInterop, compositionDrawingSurface), content);
            return (true, "dma-buf import via EglExternalObjectsFeature\n" + info);
        }
        catch (Exception e)
        {
            return (false, e.ToString());
        }
    }

    protected override void FreeGraphicsResources()
    {
        _resources?.DisposeAsync();
        _resources = null;
    }

    protected override void RenderFrame(PixelSize pixelSize)
    {
        if (_resources == null)
            return;
        using (_resources.Swapchain.BeginDraw(pixelSize, out var image))
            _resources.Content.Render(image, Yaw, Pitch, Roll, Disco);
    }
}
