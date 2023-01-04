using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
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
            Logger.TryGet(LogEventLevel.Error, "Vulkan")?.Log("VulkanControlBase",
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
    
    protected virtual void OnVulkanRender(IVulkanSharedDevice device, ISwapchain image)
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
        private VulkanBitmapChain? _swapchain;

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
            var (pixelSize, scaling) = _parent.GetSizeInfo();
            if (pixelSize.Width < 1 || pixelSize.Height < 1)
                return;
            
            using (_ctx.Device.Lock())
            {
                // Free command buffers from the last frame
                _pool.FreeUsedCommandBuffers();
                
                // Recreate our makeshift swapchain
                if (_swapchain == null || _swapchain.Size != pixelSize)
                {
                    _swapchain?.Dispose();
                    _swapchain = null;
                    _swapchain = new VulkanBitmapChain(_ctx, _feature, _pool, pixelSize, scaling);
                }
                
                _swapchain.NextProducerImage();
            }
            
            // User code is expected to take their own device locks (right now for _everything_)
            _parent.OnVulkanRender(_feature.SharedDevice!, _swapchain);
            
            using (_ctx.Device.Lock())
            {
                _swapchain.SubmitProducerImage();
                drawingContext.Custom(new DrawOp(_swapchain, _parent.Bounds.Size));
            }
        }

        public void Dispose()
        {
            using (_ctx.Device.Lock())
            {
                _ctx.DeviceApi.DeviceWaitIdle(_ctx.DeviceHandle);
                _swapchain?.Dispose();
                _pool.Dispose();
            }
        }
    }

    private (PixelSize size, double scaling) GetSizeInfo()
    {
        var scaling = this.GetVisualRoot()!.RenderScaling;
        return (new PixelSize(Math.Max(1, (int)(Bounds.Width * scaling)),
            Math.Max(1, (int)(Bounds.Height * scaling))), scaling);
    }

    class DrawOp : ICustomDrawOperation
    {
        private readonly VulkanBitmapChain _chain;

        public DrawOp(VulkanBitmapChain chain, Size size)
        {
            _chain = chain;
            Bounds = new Rect(size);
        }
        
        public void Dispose()
        {
        }

        public Rect Bounds { get; }
        public bool HitTest(Point p) => false;

        public void Render(IDrawingContextImpl context)
        {
            var bitmap = _chain.GetConsumerBitmap();
            if (bitmap == null)
                return;

            context.DrawBitmap(source: RefCountable.CreateUnownedNotClonable(bitmap), 
                               opacity: 1, 
                               sourceRect: new Rect(0, 0, _chain.Size.Width, _chain.Size.Height), 
                               destRect: Bounds);
        }

        public bool Equals(ICustomDrawOperation? other) => false;
    }

    protected interface ISwapchain
    {
        public int ImageCount { get; }
        public int CurrentImageIndex { get; }
        public PixelSize Size { get; }
        public VulkanImageInfo GetImage(int index);
    }

    class SourceImage : VulkanImage, IVulkanBitmapSourceImage
    {
        public SourceImage(IVulkanPlatformGraphicsContext context, VulkanCommandBufferPool commandBufferPool, VkFormat format, PixelSize size, double scaling) 
            : base(context, commandBufferPool, format, size, 1)
        {
            Scaling = scaling;
        }

        public VulkanImageInfo Info => ImageInfo;
        public double Scaling { get; }
    }
    
    class VulkanBitmapChain : ISwapchain, IDisposable
    {
        private readonly IVulkanPlatformGraphicsContext _context;
        private readonly IVulkanSharedDeviceGraphicsContextFeature _feature;
        private readonly SourceImage[] _images;
        private readonly IBitmapImpl[] _bitmaps;
        private int? _lastUsedBitmap, _lastSubmittedBitmap, _lastObtainedProducerBitmap;
        private object _lock = new();
        public PixelSize Size { get; }

        
        
        public VulkanBitmapChain(IVulkanPlatformGraphicsContext context, 
            IVulkanSharedDeviceGraphicsContextFeature feature,
            VulkanCommandBufferPool pool, PixelSize size, double scaling)
        {
            _context = context;
            _feature = feature;
            Size = size;
            _images = Enumerable.Range(0, 3)
                .Select(_ => new SourceImage(context, pool, VkFormat.VK_FORMAT_B8G8R8A8_UNORM, size, scaling))
                .ToArray();
            _bitmaps = _images.Select(i => _feature.CreateBitmapFromVulkanImage(i))
                .ToArray();

        }

        public IBitmapImpl? GetConsumerBitmap()
        {
            lock (_lock)
            {
                var index = _lastSubmittedBitmap ?? _lastUsedBitmap;
                if (index == null)
                    return null;
                var image = _images[index.Value];
                if (image.CurrentLayout != VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL)
                    return null;
                image.TransitionLayout(VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL, VkAccessFlags.VK_ACCESS_NONE);
                _lastUsedBitmap = index;
                _lastSubmittedBitmap = null;
                return _bitmaps[index.Value];
            }
        }

        public void NextProducerImage()
        {
            lock (_lock)
            {
                for (var c = 0; c < _bitmaps.Length; c++)
                {
                    if (c != _lastSubmittedBitmap && c != _lastUsedBitmap)
                    {
                        _lastObtainedProducerBitmap = c;
                        var image = _images[c];
                        
                        if (image.CurrentLayout != VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL)
                            image.TransitionLayout(VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
                                VkAccessFlags.VK_ACCESS_COLOR_ATTACHMENT_READ_BIT);
                        return;
                    }
                }

                throw new InvalidOperationException();
            }
        }

        public void SubmitProducerImage()
        {
            lock (_lock)
            {
                if (_lastObtainedProducerBitmap == null)
                    return;

                var image = _images[_lastObtainedProducerBitmap.Value];
                if (image.CurrentLayout != VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL)
                    image.TransitionLayout(VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                        VkAccessFlags.VK_ACCESS_NONE);
                
                _lastSubmittedBitmap = _lastObtainedProducerBitmap;
                _lastObtainedProducerBitmap = null;
            }
        }

        public void Dispose()
        {
            using (_context.Device.Lock())
                _context.DeviceApi.DeviceWaitIdle(_context.DeviceHandle);
        }

        public int ImageCount => _images.Length;

        public int CurrentImageIndex => _lastObtainedProducerBitmap!.Value;

        public VulkanImageInfo GetImage(int index)
        {
            if (index < 0 || index >= _images.Length)
                throw new IndexOutOfRangeException();
            return _images[index].ImageInfo;
        }
    }
}
