using System;
using System.Threading;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan.Surfaces
{
    public class VulkanSurfaceRenderTarget : IDisposable
    {
        private readonly VulkanPlatformInterface _platformInterface;

        public bool IsCorrupted { get; set; } = true;
        private readonly Format _format;

        public VulkanImage Image { get; private set; }

        public uint MipLevels => Image.MipLevels;

        public VulkanSurfaceRenderTarget(VulkanPlatformInterface platformInterface, VulkanSurface surface)
        {
            _platformInterface = platformInterface;

            Display = VulkanDisplay.CreateDisplay(platformInterface.Instance, platformInterface.Device,
                platformInterface.PhysicalDevice, surface);
            Surface = surface;

            // Skia seems to only create surfaces from images with unorm format

            IsRgba = Display.SurfaceFormat.Format >= Format.R8G8B8A8Unorm &&
                     Display.SurfaceFormat.Format <= Format.R8G8B8A8Srgb;
            
            _format = IsRgba ? Format.R8G8B8A8Unorm : Format.B8G8R8A8Unorm;
        }

        public bool IsRgba { get; }

        public uint ImageFormat => (uint) _format;

        public ulong MemorySize => Image.MemorySize;

        public VulkanDisplay Display { get; }

        public VulkanSurface Surface { get; }

        public uint UsageFlags => Image.UsageFlags;

        public PixelSize Size { get; private set; }

        public void Dispose()
        {
            _platformInterface.Device.WaitIdle();   
            DestroyImage();
            Display?.Dispose();
            Surface?.Dispose();
        }

        public VulkanSurfaceRenderingSession BeginDraw(float scaling)
        {
            var session = new VulkanSurfaceRenderingSession(Display, _platformInterface.Device, this, scaling);

            if (IsCorrupted)
            {
                IsCorrupted = false;
                DestroyImage();
                CreateImage();
            }
            else
            {
                Image.TransitionLayout(ImageLayout.ColorAttachmentOptimal, AccessFlags.AccessNoneKhr);
            }

            return session;
        }

        public void Invalidate()
        {
            IsCorrupted = true;
        }

        private void CreateImage()
        {
            Size = Display.Size;

            Image = new VulkanImage(_platformInterface.Device, _platformInterface.PhysicalDevice, _platformInterface.Device.CommandBufferPool, ImageFormat, Size);
        }

        private void DestroyImage()
        {
            _platformInterface.Device.WaitIdle();
            Image?.Dispose();
        }
    }
}
