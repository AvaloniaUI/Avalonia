using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Silk.NET.Vulkan;

namespace GpuInterop.VulkanDemo;

// Presents Vulkan-rendered frames to the EGL/OpenGL compositor by exporting them as Linux dma-bufs.
// The compositor imports them through EglExternalObjectsFeature, which advertises "Automatic"
// synchronization for dma-buf, so we don't import any semaphores: instead we make sure the GPU has
// finished writing each frame (vkQueueWaitIdle) before handing the buffer over for sampling.
class VulkanDmaBufSwapchain : SwapchainBase<VulkanDmaBufSwapchainImage>
{
    private readonly VulkanContext _vk;

    public VulkanDmaBufSwapchain(VulkanContext vk, ICompositionGpuInterop interop, CompositionDrawingSurface target)
        : base(interop, target)
    {
        _vk = vk;
    }

    protected override VulkanDmaBufSwapchainImage CreateImage(PixelSize size) =>
        new(_vk, size, Interop, Target);

    public IDisposable BeginDraw(PixelSize size, out VulkanImage image)
    {
        _vk.Pool.FreeUsedCommandBuffers();
        var rv = BeginDrawCore(size, out var swapchainImage);
        image = swapchainImage.Image;
        return rv;
    }
}

class VulkanDmaBufSwapchainImage : ISwapchainImage
{
    private readonly VulkanContext _vk;
    private readonly ICompositionGpuInterop _interop;
    private readonly CompositionDrawingSurface _target;
    private readonly VulkanImage _image;
    private ICompositionImportedGpuImage? _importedImage;
    private Task? _lastPresent;

    public VulkanImage Image => _image;

    public VulkanDmaBufSwapchainImage(VulkanContext vk, PixelSize size, ICompositionGpuInterop interop,
        CompositionDrawingSurface target)
    {
        _vk = vk;
        _interop = interop;
        _target = target;
        Size = size;
        // The EGL importer reads back as R8G8B8A8 (ABGR8888 in DRM terms); VulkanImage maps the format.
        _image = new VulkanImage(vk, (uint)Format.R8G8B8A8Unorm, size, true, interop.SupportedImageHandleTypes,
            dmaBuf: true);
    }

    public PixelSize Size { get; }

    public Task? LastPresent => _lastPresent;

    public void BeginDraw()
    {
        // Nothing to acquire: synchronization is handled by draining the queue in Present(). The image
        // is reused by SwapchainBase only once its previous present task has completed.
    }

    public unsafe void Present()
    {
        // Move the freshly rendered image into a layout the external consumer can read, then make sure
        // all GPU work targeting it has finished before the compositor samples from the dma-buf.
        var buffer = _vk.Pool.CreateCommandBuffer();
        buffer.BeginRecording();
        _image.TransitionLayout(buffer.InternalHandle, ImageLayout.General, AccessFlags.None);
        buffer.Submit();
        _vk.Api.QueueWaitIdle(_vk.Queue);

        if (_importedImage == null)
        {
            var (handle, properties, fds) = _image.ExportDmaBuf();
            var imported = _interop.ImportImage(handle, properties);
            // EGL dups the file descriptors during import, so release ours once the import has run.
            imported.ImportCompleted.ContinueWith(_ =>
            {
                foreach (var fd in fds)
                    NativeMethods.close(fd);
            });
            _importedImage = imported;
        }

        _lastPresent = _target.UpdateAsync(_importedImage);
    }

    public async ValueTask DisposeAsync()
    {
        if (_lastPresent != null)
            await _lastPresent;
        if (_importedImage != null)
            await _importedImage.DisposeAsync();
        _image.Dispose();
    }
}
