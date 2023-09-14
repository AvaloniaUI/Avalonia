using System;
using System.Linq;
using System.Threading;
using Avalonia.Vulkan.UnmanagedInterop;
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace Avalonia.Vulkan.Interop;

internal class VulkanDisplay : IDisposable
{
    private IVulkanPlatformGraphicsContext _context;
    private VulkanSemaphorePair _semaphorePair;
    private uint _nextImage;
    private readonly VulkanKhrSurface _surface;
    private VkSurfaceFormatKHR _surfaceFormat;
    private VkSwapchainKHR _swapchain;
    private VkExtent2D _swapchainExtent;
    private VkImage[] _swapchainImages = Array.Empty<VkImage>();
    private VkImageView[] _swapchainImageViews = Array.Empty<VkImageView>();
    public VulkanCommandBufferPool CommandBufferPool { get; private set; }
    public PixelSize Size { get; private set; }
    
    private VulkanDisplay(IVulkanPlatformGraphicsContext context, VulkanKhrSurface surface, VkSwapchainKHR swapchain,
        VkExtent2D swapchainExtent)
    {
        _context = context;
        _surface = surface;
        _swapchain = swapchain;
        _swapchainExtent = swapchainExtent;
        _semaphorePair = new VulkanSemaphorePair(_context);
        CommandBufferPool = new VulkanCommandBufferPool(_context);
        CreateSwapchainImages();
    }

    internal VkSurfaceFormatKHR SurfaceFormat
    {
        get
        {
            if (_surfaceFormat.format == VkFormat.VK_FORMAT_UNDEFINED)
                _surfaceFormat = _surface.GetSurfaceFormat();
            return _surfaceFormat;
        }
    }

    private static unsafe VkSwapchainKHR CreateSwapchain(IVulkanPlatformGraphicsContext context,
        VulkanKhrSurface surface, out VkExtent2D swapchainExtent, VulkanDisplay? oldDisplay = null)
    {
        while (!surface.CanSurfacePresent())
            Thread.Sleep(16);
        context.InstanceApi.GetPhysicalDeviceSurfaceCapabilitiesKHR(context.PhysicalDeviceHandle,
                surface.Handle, out var capabilities)
            .ThrowOnError("vkGetPhysicalDeviceSurfaceCapabilitiesKHR");
        uint presentModesCount = 0;
        context.InstanceApi.GetPhysicalDeviceSurfacePresentModesKHR(context.PhysicalDeviceHandle,
                surface.Handle, ref presentModesCount, null)
            .ThrowOnError("vkGetPhysicalDeviceSurfacePresentModesKHR");

        var modes = new VkPresentModeKHR[(int)presentModesCount];
        fixed (VkPresentModeKHR* pModes = modes)
            context.InstanceApi.GetPhysicalDeviceSurfacePresentModesKHR(context.PhysicalDeviceHandle,
                    surface.Handle, ref presentModesCount, pModes)
                .ThrowOnError("vkGetPhysicalDeviceSurfacePresentModesKHR");
        
        var imageCount = capabilities.minImageCount + 1;
        if (capabilities.maxImageCount > 0 && imageCount > capabilities.maxImageCount)
            imageCount = capabilities.maxImageCount;
        
        var surfaceFormat = surface.GetSurfaceFormat();

        bool supportsIdentityTransform = capabilities.supportedTransforms.HasAllFlags(
            VkSurfaceTransformFlagsKHR.VK_SURFACE_TRANSFORM_IDENTITY_BIT_KHR);

        bool isRotated =
            capabilities.currentTransform.HasAllFlags(VkSurfaceTransformFlagsKHR.VK_SURFACE_TRANSFORM_ROTATE_90_BIT_KHR)
            || capabilities.currentTransform.HasAllFlags(VkSurfaceTransformFlagsKHR
                .VK_SURFACE_TRANSFORM_ROTATE_270_BIT_KHR);

        if (capabilities.currentExtent.width != uint.MaxValue) 
            swapchainExtent = capabilities.currentExtent;
        else
        {
            var surfaceSize = surface.Size;

            var width = Math.Max(capabilities.minImageExtent.width,
                Math.Min(capabilities.maxImageExtent.width, (uint)surfaceSize.Width));
            var height = Math.Max(capabilities.minImageExtent.height,
                Math.Min(capabilities.maxImageExtent.height, (uint)surfaceSize.Height));

            swapchainExtent = new VkExtent2D
            {
                width = width,
                height = height
            };
        }
        VkPresentModeKHR presentMode;
        if (modes.Contains(VkPresentModeKHR.VK_PRESENT_MODE_MAILBOX_KHR))
            presentMode = VkPresentModeKHR.VK_PRESENT_MODE_MAILBOX_KHR;
        else if (modes.Contains(VkPresentModeKHR.VK_PRESENT_MODE_FIFO_KHR))
            presentMode = VkPresentModeKHR.VK_PRESENT_MODE_FIFO_KHR;
        else
            presentMode = VkPresentModeKHR.VK_PRESENT_MODE_IMMEDIATE_KHR;

        var swapchainCreateInfo = new VkSwapchainCreateInfoKHR
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR,
            surface = surface.Handle,
            minImageCount = imageCount,
            imageFormat = surfaceFormat.format,
            imageColorSpace = surfaceFormat.colorSpace,
            imageExtent = swapchainExtent,
            imageUsage = VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT |
                         VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT,
            imageSharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE,
            imageArrayLayers = 1,
            preTransform = supportsIdentityTransform && isRotated
                ? VkSurfaceTransformFlagsKHR.VK_SURFACE_TRANSFORM_IDENTITY_BIT_KHR
                : capabilities.currentTransform,
            compositeAlpha = VkCompositeAlphaFlagsKHR.VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR,
            presentMode = presentMode,
            clipped = 1,
            oldSwapchain = oldDisplay?._swapchain ?? default
        };
        context.DeviceApi.CreateSwapchainKHR(context.DeviceHandle, ref swapchainCreateInfo, IntPtr.Zero,
            out var swapchain).ThrowOnError("vkCreateSwapchainKHR");
        oldDisplay?.DestroySwapchain();
        return swapchain;
    }

    private void DestroySwapchain()
    {
        if(_swapchain.Handle != 0)
            _context.DeviceApi.DestroySwapchainKHR(_context.DeviceHandle, _swapchain, IntPtr.Zero);
        _swapchain = default;
    }

    internal static VulkanDisplay CreateDisplay(IVulkanPlatformGraphicsContext context, VulkanKhrSurface surface)
    {
        var swapchain = CreateSwapchain(context, surface, out var extent);
        return new VulkanDisplay(context, surface, swapchain, extent);
    }

    private void DestroyCurrentImageViews()
    {
        if (_swapchainImageViews.Length <= 0) 
            return;
        foreach (var imageView in _swapchainImageViews)
            _context.DeviceApi.DestroyImageView(_context.DeviceHandle, imageView, IntPtr.Zero);

        _swapchainImageViews = Array.Empty<VkImageView>();
        
    }
    
    private unsafe void CreateSwapchainImages()
    {
        DestroyCurrentImageViews();
        Size = new PixelSize((int)_swapchainExtent.width, (int)_swapchainExtent.height);
        uint imageCount = 0;
        _context.DeviceApi.GetSwapchainImagesKHR(_context.DeviceHandle, _swapchain, ref imageCount, null)
            .ThrowOnError("vkGetSwapchainImagesKHR");
        _swapchainImages = new VkImage[imageCount];
        fixed (VkImage* pImages = _swapchainImages)
            _context.DeviceApi.GetSwapchainImagesKHR(_context.DeviceHandle, _swapchain, ref imageCount,
                pImages).ThrowOnError("vkGetSwapchainImagesKHR");
        _swapchainImageViews = new VkImageView[imageCount];
        for (var c = 0; c < imageCount; c++)
            _swapchainImageViews[c] = CreateSwapchainImageView(_swapchainImages[c], SurfaceFormat.format);
    }

    private VkImageView CreateSwapchainImageView(VkImage swapchainImage, VkFormat format)
    {
        var imageViewCreateInfo = new VkImageViewCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
            subresourceRange =
            {
                aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                levelCount = 1,
                layerCount = 1
            },
            format = format,
            image = swapchainImage,
            viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D,
        };
        _context.DeviceApi.CreateImageView(_context.DeviceHandle, ref imageViewCreateInfo,
            IntPtr.Zero, out var imageView).ThrowOnError("vkCreateImageView");
        return imageView;
    }
    
    private void Recreate()
    {
        _context.DeviceApi.DeviceWaitIdle(_context.DeviceHandle);
        _swapchain = CreateSwapchain(_context, _surface, out var extent, this);
        _swapchainExtent = extent;
        CreateSwapchainImages();
    }
    
    public bool EnsureSwapchainAvailable()
    {
        if (Size != _surface.Size)
        {
            Recreate();
            return true;
        }
        return false;
    }

    public VulkanCommandBuffer StartPresentation()
    {
        _nextImage = 0;
        while (true)
        {
            var acquireResult = _context.DeviceApi.AcquireNextImageKHR(
                _context.DeviceHandle,
                _swapchain,
                ulong.MaxValue,
                _semaphorePair.ImageAvailableSemaphore.Handle,
                default, out _nextImage);
            if (acquireResult is VkResult.VK_ERROR_OUT_OF_DATE_KHR or VkResult.VK_SUBOPTIMAL_KHR)
                Recreate();
            else
            {
                acquireResult.ThrowOnError("vkAcquireNextImageKHR");
                break;
            }
        }

        var commandBuffer = CommandBufferPool.CreateCommandBuffer();
        commandBuffer.BeginRecording();
        VulkanMemoryHelper.TransitionLayout(_context, commandBuffer,
            _swapchainImages[_nextImage], VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            VkAccessFlags.VK_ACCESS_NONE, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
            VkAccessFlags.VK_ACCESS_TRANSFER_WRITE_BIT, 1);
        return commandBuffer;
    }

    internal unsafe void BlitImageToCurrentImage(VulkanCommandBuffer commandBuffer, VulkanImage image)
    {
        VulkanMemoryHelper.TransitionLayout(_context, commandBuffer,
            image.Handle, image.CurrentLayout, VkAccessFlags.VK_ACCESS_NONE,
            VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
            VkAccessFlags.VK_ACCESS_TRANSFER_READ_BIT,
            image.MipLevels);

        var srcBlitRegion = new VkImageBlit
        {
            srcOffsets2 =
            {
                x = image.Size.Width,
                y = image.Size.Height,
                z = 1
            },
            dstOffsets2 =
            {
                x = Size.Width,
                y = Size.Height,
                z = 1
            },
            srcSubresource =
            {
                aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                layerCount = 1
            },
            dstSubresource =
            {
                aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                layerCount = 1
            }
        };
        
        _context.DeviceApi.CmdBlitImage(commandBuffer.Handle, image.Handle,
            VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
            _swapchainImages[_nextImage],
            VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
            1, &srcBlitRegion, VkFilter.VK_FILTER_LINEAR);

        VulkanMemoryHelper.TransitionLayout(_context, commandBuffer,
            image.Handle, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
            VkAccessFlags.VK_ACCESS_TRANSFER_READ_BIT,
            image.CurrentLayout, VkAccessFlags.VK_ACCESS_NONE, image.MipLevels);
    }

    internal unsafe void EndPresentation(VulkanCommandBuffer commandBuffer)
    {
        VulkanMemoryHelper.TransitionLayout(_context, commandBuffer,
            _swapchainImages[_nextImage],
            VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
            VkAccessFlags.VK_ACCESS_NONE,
            VkImageLayout.VK_IMAGE_LAYOUT_PRESENT_SRC_KHR,
            VkAccessFlags.VK_ACCESS_NONE,
            1);
        commandBuffer.Submit(new[] { _semaphorePair.ImageAvailableSemaphore },
            new[] { VkPipelineStageFlags.VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT },
            new[] { _semaphorePair.RenderFinishedSemaphore });
        
        var semaphore = _semaphorePair.RenderFinishedSemaphore.Handle;
        var swapchain = _swapchain;
        var nextImage = _nextImage;

        VkResult result;
        var presentInfo = new VkPresentInfoKHR
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PRESENT_INFO_KHR,
            waitSemaphoreCount = 1,
            pWaitSemaphores = &semaphore,
            swapchainCount = 1,
            pSwapchains = &swapchain,
            pImageIndices = &nextImage,
            pResults = &result
        };
        
        _context.DeviceApi.vkQueuePresentKHR(_context.MainQueueHandle, ref presentInfo)
            .ThrowOnError("vkQueuePresentKHR");
        result.ThrowOnError("vkQueuePresentKHR");
    }
    
    public void Dispose()
    {
        _context.DeviceApi.DeviceWaitIdle(_context.DeviceHandle);
        _semaphorePair?.Dispose();
        DestroyCurrentImageViews();
        DestroySwapchain();
        CommandBufferPool?.Dispose();
        CommandBufferPool = null!;
    }

}
