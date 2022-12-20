using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using Avalonia.Vulkan.Interop;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan;

public abstract class VulkanControlBase : Control
{
    private Task<bool>? _initialization;
    private VulkanControlContext? _currentContext;

    private bool EnsureInitialized()
    {
        if (_initialization != null)
        {
            // Check if we've previously failed to initialize on this platform
            if (_initialization is { IsCompleted: true, Result: false } ||
                _initialization?.IsFaulted == true)
                return false;

            // Check if we are still waiting for init to complete
            if (_initialization is { IsCompleted: false })
                return false;

            return true;
        }

        _initialization = InitializeAsync();
        return false;
    }

    async Task<bool> InitializeAsync()
    {
        var feature = (IVulkanSharedDeviceGraphicsContextFeature?)
            await this.GetVisualRoot()!.Renderer.TryGetRenderInterfaceFeature(
                typeof(IVulkanSharedDeviceGraphicsContextFeature));
        if (feature?.SharedDevice == null)
        {
            Logger.TryGet(LogEventLevel.Error, "OpenGL")?.Log("VulkanControlBase",
                "Unable to obtain Vulkan device from the renderer");
            return false;
        }
        _currentContext = new VulkanControlContext(this, feature);
        OnVulkanInit(feature.SharedDevice);
        
        InvalidateVisual();
        return true;
    }


    protected virtual void OnVulkanInit(IVulkanSharedDevice device)
    {
        
    }
    
    protected virtual void OnVulkanDeInit(IVulkanSharedDevice device)
    {
        
    }
    
    protected virtual void OnVulkanRender(IVulkanSharedDevice device, VulkanImageInfo image)
    {
        
    }

    public sealed override void Render(DrawingContext context)
    {
        if(!EnsureInitialized())
            return;
        _currentContext!.Render(context);
    }

    void DoCleanup()
    {
        if (_currentContext != null)
        {
            OnVulkanDeInit(_currentContext.SharedDevice);
            _currentContext.Dispose();
            _currentContext = null;
            _initialization = null;
        }
    }
    
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DoCleanup();
        base.OnDetachedFromVisualTree(e);
    }


    class VulkanControlContext : IDisposable
    {
        private readonly VulkanControlBase _parent;
        private readonly IVulkanSharedDeviceGraphicsContextFeature _feature;
        private readonly VulkanContext _ctx;
        private VulkanCommandBufferPool _pool;
        private VulkanImage? _frontBuffer;

        
        public IVulkanSharedDevice SharedDevice { get; }

        public VulkanControlContext(VulkanControlBase parent, IVulkanSharedDeviceGraphicsContextFeature feature)
        {
            _parent = parent;
            _feature = feature;
            SharedDevice = feature.SharedDevice!;
            using (SharedDevice.Device.Lock())
            {
                _ctx = new VulkanContext(SharedDevice.Device, new());
                _pool = new VulkanCommandBufferPool(_ctx);
            }
        }


        public void Render(DrawingContext drawingContext)
        {
            var pixelSize = _parent.GetPixelSize();
            if (pixelSize.Width < 1 || pixelSize.Height < 1)
                return;
            using (_ctx.Device.Lock())
            {
                // TODO: replace this with a swapchain of sorts
                _ctx.DeviceApi.DeviceWaitIdle(_ctx.DeviceHandle);

                if (_frontBuffer == null || _frontBuffer.Size != pixelSize)
                {
                    _frontBuffer?.Dispose();
                    _frontBuffer = null;
                    _frontBuffer = new VulkanImage(_ctx, _pool, VkFormat.VK_FORMAT_B8G8R8A8_UNORM,
                        pixelSize,
                        1);
                    
                }
                _frontBuffer.TransitionLayout(VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
                    VkAccessFlags.VK_ACCESS_COLOR_ATTACHMENT_READ_BIT);
            }
            
            // User code is expected to take their own device locks (right now for _everything_)
            _parent.OnVulkanRender(_feature.SharedDevice!, _frontBuffer.ImageInfo);
            
            using (_ctx.Device.Lock())
            {
                _frontBuffer.TransitionLayout(VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                    VkAccessFlags.VK_ACCESS_NONE);
                using var wrapped =
                    new Bitmap(RefCountable.Create(_feature.CreateBitmapFromVulkanImage(_frontBuffer.ImageInfo, 1)));
                
                drawingContext.DrawImage(wrapped, new Rect(_parent.Bounds.Size));
            }
        }

        public void Dispose()
        {
            using (_ctx.Device.Lock())
            {
                _ctx.DeviceApi.DeviceWaitIdle(_ctx.DeviceHandle);
                _frontBuffer?.Dispose();
                _pool.Dispose();
            }
        }
    }

    private PixelSize GetPixelSize()
    {
        var scaling = this.GetVisualRoot()!.RenderScaling;
        return new PixelSize(Math.Max(1, (int)(Bounds.Width * scaling)),
            Math.Max(1, (int)(Bounds.Height * scaling)));
    }
}