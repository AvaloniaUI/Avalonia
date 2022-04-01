using System;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using Avalonia.Vulkan.Skia;
using Silk.NET.Vulkan;
using SkiaSharp;

namespace Avalonia.Vulkan.Controls
{
    public abstract class VulkanControlBase : Control
    {
        private VulkanPlatformInterface _platformInterface;
        private VulkanBitmapAttachment _attachment;
        private VulkanBitmapAttachment _oldAttachment;
        private bool _initialized;

        public sealed override void Render(DrawingContext context)
        {
            if (!EnsureInitialized())
                return;

            lock (_platformInterface.Device.Lock)
            {
                _platformInterface.Device.QueueWaitIdle();
                _oldAttachment?.Dispose();
                _oldAttachment = null;

                EnsureTextureAttachment();

                OnVulkanRender(_platformInterface, new VulkanImageInfo(_attachment.Image as VulkanImage));
                _attachment.Present();
            }

            context.Custom(new VulkanDrawOperation(this, _attachment, new Rect(_attachment.Image.Size.ToSize(VisualRoot.RenderScaling)), Bounds));
            base.Render(context);
        }

        private void EnsureTextureAttachment()
        {
            if (_platformInterface != null)
                if (_attachment == null || _attachment.Image.Size != GetPixelSize())
                {
                    _platformInterface.Device.QueueWaitIdle();
                    _oldAttachment?.Dispose();
                    _oldAttachment = _attachment;
                    _attachment = new VulkanBitmapAttachment(_platformInterface, (uint)Format.B8G8R8A8Unorm, GetPixelSize());
                }

            _attachment.Image.TransitionLayout(ImageLayout.ColorAttachmentOptimal, AccessFlags.AccessColorAttachmentReadBit);
        }

        private void DoCleanup()
        {
            if (_platformInterface != null)
            {
                try
                {
                    if (_initialized)
                    {
                        _initialized = false;
                        OnVulkanDeinit(_platformInterface, new VulkanImageInfo(_attachment.Image as VulkanImage));
                    }
                }
                finally
                {
                    _platformInterface.Device.QueueWaitIdle();
                    _attachment?.Dispose();
                    _oldAttachment?.Dispose();
                    _oldAttachment = null;
                    _attachment = null;
                }
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            DoCleanup();
            base.OnDetachedFromVisualTree(e);
        }

        private bool EnsureInitializedCore()
        {
            _platformInterface = AvaloniaLocator.Current.GetService<VulkanPlatformInterface>();
            if (_platformInterface == null || _platformInterface.Device == null)
            {
                // Device is not ready. Try to initialize on the next render
                Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Render);
                return false;
            }

            lock (_platformInterface.Device.Lock)
            {
                try
                {
                    EnsureTextureAttachment();

                    return true;
                }
                catch (Exception e)
                {
                    Logger.TryGet(LogEventLevel.Error, "Vulkan")?.Log("VulkanControlBase",
                        "Unable to initialize Vulkan Image: {exception}", e);
                    return false;
                }
            }
        }

        private bool EnsureInitialized()
        {
            if (_initialized)
                return true;
            _initialized = EnsureInitializedCore();

            if (!_initialized)
                return false;

            OnVulkanInit(_platformInterface, new VulkanImageInfo(_attachment.Image));

            return true;
        }
        
        private PixelSize GetPixelSize()
        {
            var scaling = VisualRoot.RenderScaling;
            return new PixelSize(Math.Max(1, (int)(Bounds.Width * scaling)),
                Math.Max(1, (int)(Bounds.Height * scaling)));
        }


        protected virtual void OnVulkanInit(VulkanPlatformInterface platformInterface, VulkanImageInfo info)
        {
            
        }

        protected virtual void OnVulkanDeinit(VulkanPlatformInterface platformInterface, VulkanImageInfo info)
        {
            
        }
        
        protected abstract void OnVulkanRender(VulkanPlatformInterface platformInterface, VulkanImageInfo info);

        private class VulkanDrawOperation : ICustomDrawOperation
        {
            private readonly VulkanBitmapAttachment _bitmap;

            public Rect Bounds => _control.Bounds;

            private readonly VulkanControlBase _control;
            private readonly Rect _srcRect;
            private readonly Rect _dstRect;

            public VulkanDrawOperation(VulkanControlBase control, VulkanBitmapAttachment attachment, Rect srcRect, Rect dstRect)
            {
                _control = control;
                _srcRect = srcRect;
                _dstRect = dstRect;
                _bitmap = attachment;
            }

            public void Dispose()
            {
            }

            public bool Equals(ICustomDrawOperation other)
            {
                return other is VulkanDrawOperation && Equals(this, other);
            }

            public bool HitTest(Point p)
            {
                return Bounds.Contains(p);
            }

            public void Render(IDrawingContextImpl context)
            {
                if (_bitmap == null)
                    return;

                if (context is not ISkiaDrawingContextImpl skiaDrawingContextImpl)
                    return;

                using (_bitmap.Lock())
                {
                    _control._platformInterface.Device.QueueWaitIdle();

                    var gpu = AvaloniaLocator.Current.GetService<VulkanSkiaGpu>();

                    var imageInfo = new GRVkImageInfo()
                    {
                        CurrentQueueFamily = _control._platformInterface.PhysicalDevice.QueueFamilyIndex,
                        Format = (uint)_bitmap.Image.Format,
                        Image = _bitmap.Image.Handle,
                        ImageLayout = (uint)_bitmap.Image.CurrentLayout,
                        ImageTiling = (uint)_bitmap.Image.Tiling,
                        ImageUsageFlags = (uint)_bitmap.Image.UsageFlags,
                        LevelCount = _bitmap.Image.MipLevels,
                        SampleCount = 1,
                        Protected = false,
                        Alloc = new GRVkAlloc()
                        {
                            Memory = _bitmap.Image.MemoryHandle,
                            Flags = 0,
                            Offset = 0,
                            Size = _bitmap.Image.MemorySize
                        }
                    };

                    using (var backendTexture = new GRBackendRenderTarget(_bitmap.Image.Size.Width, _bitmap.Image.Size.Height, 1,
                            imageInfo))
                    using (var surface = SKSurface.Create(gpu.GrContext, backendTexture,
                        GRSurfaceOrigin.TopLeft,
                        SKColorType.Bgra8888, SKColorSpace.CreateSrgb()))
                    {
                        // Again, silently ignore, if something went wrong it's not our fault
                        if (surface == null)
                            return;

                        using (var snapshot = surface.Snapshot())
                            skiaDrawingContextImpl.SkCanvas.DrawImage(snapshot, _srcRect.ToSKRect(), _dstRect.ToSKRect(), new SKPaint());
                    }
                }
            }
        }
    }

    public struct VulkanImageInfo
    {
        internal VulkanImageInfo(VulkanImage image)
        {
            Image = image;
        }

        public Format Format => Image.Format;
        public PixelSize PixelSize => Image.Size;
        public VulkanImage Image { get; }
    }
}
