using System;
using System.Linq;
using System.Threading;
using Avalonia.Vulkan.Surfaces;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Avalonia.Vulkan
{
    public class VulkanDisplay : IDisposable
    {
        private static KhrSwapchain _swapchainExtension;
        private readonly VulkanInstance _instance;
        private readonly VulkanPhysicalDevice _physicalDevice;
        private readonly VulkanSemaphorePair _semaphorePair;
        private uint _nextImage;
        private readonly VulkanSurface _surface;
        private SurfaceFormatKHR _surfaceFormat;
        private SwapchainKHR _swapchain;
        private Extent2D _swapchainExtent;
        private Image[] _swapchainImages;
        private VulkanDevice _device { get; }
        private ImageView[] _swapchainImageViews = new ImageView[0];

        public VulkanCommandBufferPool CommandBufferPool { get; set; }

        public object Lock => _device.Lock;

        private VulkanDisplay(VulkanInstance instance, VulkanDevice device,
            VulkanPhysicalDevice physicalDevice, VulkanSurface surface, SwapchainKHR swapchain,
            Extent2D swapchainExtent)
        {
            _instance = instance;
            _device = device;
            _physicalDevice = physicalDevice;
            _swapchain = swapchain;
            _swapchainExtent = swapchainExtent;
            _surface = surface;

            CreateSwapchainImages();

            _semaphorePair = new VulkanSemaphorePair(_device);

            CommandBufferPool = new VulkanCommandBufferPool(device, physicalDevice);
        }

        public PixelSize Size { get; private set; }
        public uint QueueFamilyIndex => _physicalDevice.QueueFamilyIndex;

        internal SurfaceFormatKHR SurfaceFormat
        {
            get
            {
                if (_surfaceFormat.Format == Format.Undefined)
                    _surfaceFormat = _surface.GetSurfaceFormat(_physicalDevice);

                return _surfaceFormat;
            }
        }

        public unsafe void Dispose()
        {
            _device.WaitIdle();
            _semaphorePair?.Dispose();
            DestroyCurrentImageViews();
            _swapchainExtension.DestroySwapchain(_device.InternalHandle, _swapchain, null);
            CommandBufferPool.Dispose();
        }

        private static unsafe SwapchainKHR CreateSwapchain(VulkanInstance instance, VulkanDevice device,
            VulkanPhysicalDevice physicalDevice, VulkanSurface surface, out Extent2D swapchainExtent,
            VulkanDisplay oldDisplay = null)
        {
            if (_swapchainExtension == null)
            {
                instance.Api.TryGetDeviceExtension(instance.InternalHandle, device.InternalHandle, out KhrSwapchain extension);

                _swapchainExtension = extension;
            }

            while (!surface.CanSurfacePresent(physicalDevice))
            {
                Thread.Sleep(16);
            }

            VulkanSurface.SurfaceExtension.GetPhysicalDeviceSurfaceCapabilities(physicalDevice.InternalHandle,
                surface.ApiHandle, out var capabilities);

            uint presentModesCount;

            VulkanSurface.SurfaceExtension.GetPhysicalDeviceSurfacePresentModes(physicalDevice.InternalHandle,
                surface.ApiHandle,
                &presentModesCount, null);

            var presentModes = new PresentModeKHR[presentModesCount];

            fixed (PresentModeKHR* pPresentModes = presentModes)
            {
                VulkanSurface.SurfaceExtension.GetPhysicalDeviceSurfacePresentModes(physicalDevice.InternalHandle,
                    surface.ApiHandle, &presentModesCount, pPresentModes);
            }

            var imageCount = capabilities.MinImageCount + 1;
            if (capabilities.MaxImageCount > 0 && imageCount > capabilities.MaxImageCount)
                imageCount = capabilities.MaxImageCount;

            var surfaceFormat = surface.GetSurfaceFormat(physicalDevice);

            bool supportsIdentityTransform = capabilities.SupportedTransforms.HasFlag(SurfaceTransformFlagsKHR.SurfaceTransformIdentityBitKhr);
            bool isRotated = capabilities.CurrentTransform.HasFlag(SurfaceTransformFlagsKHR.SurfaceTransformRotate90BitKhr) || 
                capabilities.CurrentTransform.HasFlag(SurfaceTransformFlagsKHR.SurfaceTransformRotate270BitKhr);

            if (capabilities.CurrentExtent.Width != uint.MaxValue)
            {
                swapchainExtent = capabilities.CurrentExtent;
            }
            else
            {
                var surfaceSize = surface.SurfaceSize;

                var width = Math.Max(capabilities.MinImageExtent.Width,
                    Math.Min(capabilities.MaxImageExtent.Width, (uint)surfaceSize.Width));
                var height = Math.Max(capabilities.MinImageExtent.Height,
                    Math.Min(capabilities.MaxImageExtent.Height, (uint)surfaceSize.Height));

                swapchainExtent = new Extent2D(width, height);
            }

            PresentModeKHR presentMode;
            var modes = presentModes.ToList();

            if (modes.Contains(PresentModeKHR.PresentModeMailboxKhr))
                presentMode = PresentModeKHR.PresentModeMailboxKhr;
            else if (modes.Contains(PresentModeKHR.PresentModeFifoKhr))
                presentMode = PresentModeKHR.PresentModeFifoKhr;
            else
                presentMode = PresentModeKHR.PresentModeImmediateKhr;

            var compositeAlphaFlags = CompositeAlphaFlagsKHR.CompositeAlphaOpaqueBitKhr;

            var swapchainCreateInfo = new SwapchainCreateInfoKHR
            {
                SType = StructureType.SwapchainCreateInfoKhr,
                Surface = surface.ApiHandle,
                MinImageCount = imageCount,
                ImageFormat = surfaceFormat.Format,
                ImageColorSpace = surfaceFormat.ColorSpace,
                ImageExtent = swapchainExtent,
                ImageUsage =
                    ImageUsageFlags.ImageUsageColorAttachmentBit | ImageUsageFlags.ImageUsageTransferDstBit,
                ImageSharingMode = SharingMode.Exclusive,
                ImageArrayLayers = 1,
                PreTransform = supportsIdentityTransform && isRotated ? SurfaceTransformFlagsKHR.SurfaceTransformIdentityBitKhr : capabilities.CurrentTransform,
                CompositeAlpha = compositeAlphaFlags,
                PresentMode = presentMode,
                Clipped = true,
                OldSwapchain = oldDisplay?._swapchain ?? new SwapchainKHR()
            };

            if (oldDisplay != null)
            {
                _swapchainExtension.DestroySwapchain(device.InternalHandle, oldDisplay._swapchain, null);
            }

            _swapchainExtension.CreateSwapchain(device.InternalHandle, swapchainCreateInfo, null, out var swapchain)
                .ThrowOnError();

            return swapchain;
        }


        internal static VulkanDisplay CreateDisplay(VulkanInstance instance, VulkanDevice device,
            VulkanPhysicalDevice physicalDevice, VulkanSurface surface)
        {
            var swapchain = CreateSwapchain(instance, device, physicalDevice, surface, out var extent);

            return new VulkanDisplay(instance, device, physicalDevice, surface, swapchain, extent);
        }

        private unsafe void CreateSwapchainImages()
        {
            DestroyCurrentImageViews();

            Size = new PixelSize((int)_swapchainExtent.Width, (int)_swapchainExtent.Height);

            uint imageCount = 0;

            _swapchainExtension.GetSwapchainImages(_device.InternalHandle, _swapchain, &imageCount, null);

            _swapchainImages = new Image[imageCount];

            fixed (Image* pSwapchainImages = _swapchainImages)
            {
                _swapchainExtension.GetSwapchainImages(_device.InternalHandle, _swapchain, &imageCount, pSwapchainImages);
            }

            _swapchainImageViews = new ImageView[imageCount];

            var surfaceFormat = SurfaceFormat;

            for (var i = 0; i < imageCount; i++)
                _swapchainImageViews[i] = CreateSwapchainImageView(_swapchainImages[i], surfaceFormat.Format);
        }

        private unsafe void DestroyCurrentImageViews()
        {
            if (_swapchainImageViews.Length > 0)
                for (var i = 0; i < _swapchainImageViews.Length; i++)
                    _instance.Api.DestroyImageView(_device.InternalHandle, _swapchainImageViews[i], null);
        }

        private void Recreate()
        {
            _device.WaitIdle();
            _swapchain = CreateSwapchain(_instance, _device, _physicalDevice, _surface, out var extent, this);

            _swapchainExtent = extent;

            CreateSwapchainImages();
        }

        private unsafe ImageView CreateSwapchainImageView(Image swapchainImage, Format format)
        {
            var componentMapping = new ComponentMapping(
                ComponentSwizzle.Identity,
                ComponentSwizzle.Identity,
                ComponentSwizzle.Identity,
                ComponentSwizzle.Identity);

            var aspectFlags = ImageAspectFlags.ImageAspectColorBit;

            var subresourceRange = new ImageSubresourceRange(aspectFlags, 0, 1, 0, 1);

            var imageCreateInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = swapchainImage,
                ViewType = ImageViewType.ImageViewType2D,
                Format = format,
                Components = componentMapping,
                SubresourceRange = subresourceRange
            };

            _instance.Api.CreateImageView(_device.InternalHandle, imageCreateInfo, null, out var imageView).ThrowOnError();
            return imageView;
        }

        public bool EnsureSwapchainAvailable()
        {
            if (Size != _surface.SurfaceSize)
            {
                Recreate();

                return false;
            }

            return true;
        }

        internal VulkanCommandBufferPool.VulkanCommandBuffer StartPresentation(VulkanSurfaceRenderTarget renderTarget)
        {
            _nextImage = 0;
            while (true)
            {
                var acquireResult = _swapchainExtension.AcquireNextImage(
                    _device.InternalHandle,
                    _swapchain,
                    ulong.MaxValue,
                    _semaphorePair.ImageAvailableSemaphore,
                    new Fence(),
                    ref _nextImage);

                if (acquireResult == Result.ErrorOutOfDateKhr ||
                    acquireResult == Result.SuboptimalKhr)
                {
                    Recreate();
                }
                else
                {
                    acquireResult.ThrowOnError();
                    break;
                }
            }

            var commandBuffer = CommandBufferPool.CreateCommandBuffer();
            commandBuffer.BeginRecording();

            VulkanMemoryHelper.TransitionLayout(_device, commandBuffer.InternalHandle,
                _swapchainImages[_nextImage], ImageLayout.Undefined,
                AccessFlags.AccessNoneKhr,
                ImageLayout.TransferDstOptimal,
                AccessFlags.AccessTransferWriteBit,
                1);

            return commandBuffer;
        }

        internal void BlitImageToCurrentImage(VulkanSurfaceRenderTarget renderTarget, CommandBuffer commandBuffer)
        {
            VulkanMemoryHelper.TransitionLayout(_device, commandBuffer,
                renderTarget.Image.InternalHandle.Value, (ImageLayout)renderTarget.Image.CurrentLayout,
                AccessFlags.AccessNoneKhr,
                ImageLayout.TransferSrcOptimal,
                AccessFlags.AccessTransferReadBit,
                renderTarget.MipLevels);

            var srcBlitRegion = new ImageBlit
            {
                SrcOffsets = new ImageBlit.SrcOffsetsBuffer
                {
                    Element0 = new Offset3D(0, 0, 0),
                    Element1 = new Offset3D(renderTarget.Size.Width, renderTarget.Size.Height, 1),
                },
                DstOffsets = new ImageBlit.DstOffsetsBuffer
                {
                    Element0 = new Offset3D(0, 0, 0),
                    Element1 = new Offset3D(Size.Width, Size.Height, 1),
                },
                SrcSubresource =
                    new ImageSubresourceLayers
                    {
                        AspectMask = ImageAspectFlags.ImageAspectColorBit,
                        BaseArrayLayer = 0,
                        LayerCount = 1,
                        MipLevel = 0
                    },
                DstSubresource = new ImageSubresourceLayers
                {
                    AspectMask = ImageAspectFlags.ImageAspectColorBit,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                    MipLevel = 0
                }
            };

            _device.Api.CmdBlitImage(commandBuffer, renderTarget.Image.InternalHandle.Value,
                ImageLayout.TransferSrcOptimal,
                _swapchainImages[_nextImage],
                ImageLayout.TransferDstOptimal,
                1,
                srcBlitRegion,
                Filter.Linear
                );

            VulkanMemoryHelper.TransitionLayout(_device, commandBuffer,
                renderTarget.Image.InternalHandle.Value, ImageLayout.TransferSrcOptimal,
                AccessFlags.AccessTransferReadBit,
                (ImageLayout) renderTarget.Image.CurrentLayout,
                AccessFlags.AccessNoneKhr,
                renderTarget.MipLevels);
        }

        internal unsafe void EndPresentation(VulkanCommandBufferPool.VulkanCommandBuffer commandBuffer)
        {
            VulkanMemoryHelper.TransitionLayout(_device, commandBuffer.InternalHandle,
                _swapchainImages[_nextImage], ImageLayout.TransferDstOptimal,
                AccessFlags.AccessNoneKhr,
                ImageLayout.PresentSrcKhr,
                AccessFlags.AccessNoneKhr,
                1);

            commandBuffer.Submit(
                new[] { _semaphorePair.ImageAvailableSemaphore },
                new[] { PipelineStageFlags.PipelineStageColorAttachmentOutputBit },
                new[] { _semaphorePair.RenderFinishedSemaphore });

            var semaphore = _semaphorePair.RenderFinishedSemaphore;
            var swapchain = _swapchain;
            var nextImage = _nextImage;

            Result result;

            var presentInfo = new PresentInfoKHR
            {
                SType = StructureType.PresentInfoKhr,
                WaitSemaphoreCount = 1,
                PWaitSemaphores = &semaphore,
                SwapchainCount = 1,
                PSwapchains = &swapchain,
                PImageIndices = &nextImage,
                PResults = &result
            };

            lock (_device.Lock)
            {
                _swapchainExtension.QueuePresent(_device.PresentQueue.InternalHandle, presentInfo);
            }
            
            CommandBufferPool.FreeUsedCommandBuffers();
        }
    }
}
