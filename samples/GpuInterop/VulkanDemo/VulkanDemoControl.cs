using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Rendering.Composition;

namespace GpuInterop.VulkanDemo;

public class VulkanDemoControl : DrawingSurfaceDemoBase
{
    class VulkanResources : IAsyncDisposable
    {
        public VulkanContext Context { get; }
        public VulkanSwapchain Swapchain { get; }
        public VulkanContent Content { get; }

        public VulkanResources(VulkanContext context, VulkanSwapchain swapchain, VulkanContent content)
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
        var (context, info) = VulkanContext.TryCreate(gpuInterop);
        if (context == null)
            return (false, info);
        try
        {
            var content = new VulkanContent(context);
            _resources = new VulkanResources(context,
                new VulkanSwapchain(context, gpuInterop, compositionDrawingSurface), content);
            return (true, info);
        }
        catch(Exception e)
        {
            return (false, e.ToString());
        }
    }

    protected override void FreeGraphicsResources()
    {
        _resources?.DisposeAsync();
        _resources = null;
    }

    protected override unsafe void RenderFrame(PixelSize pixelSize)
    {
        if (_resources == null)
            return;
        using (_resources.Swapchain.BeginDraw(pixelSize, out var image))
        {
            /*
            var commandBuffer = _resources.Context.Pool.CreateCommandBuffer();
            commandBuffer.BeginRecording();
            image.TransitionLayout(commandBuffer.InternalHandle, ImageLayout.TransferDstOptimal, AccessFlags.None);

            var range = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                LayerCount = 1,
                LevelCount = 1,
                BaseArrayLayer = 0,
                BaseMipLevel = 0
            };
            var color = new ClearColorValue
            {
                Float32_0 = 1, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1
            };
            _resources.Context.Api.CmdClearColorImage(commandBuffer.InternalHandle, image.InternalHandle.Value, ImageLayout.TransferDstOptimal,
                &color, 1, &range);
            commandBuffer.Submit();*/
            _resources.Content.Render(image, Yaw, Pitch, Roll, Disco);
        }
    }
}
