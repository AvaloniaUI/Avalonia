using System;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan.Interop;

internal static class VulkanMemoryHelper
{
    internal static int FindSuitableMemoryTypeIndex(IVulkanPlatformGraphicsContext context, uint memoryTypeBits,
        VkMemoryPropertyFlags flags)
    {
        context.InstanceApi.GetPhysicalDeviceMemoryProperties(context.PhysicalDeviceHandle,
            out var properties);
        for (var i = 0; i < properties.memoryTypeCount; i++)
        {
            var type = properties.memoryTypes[i];

            if ((memoryTypeBits & (1 << i)) != 0 && type.propertyFlags.HasAllFlags(flags)) 
                return i;
        }

        return -1;
    }



    internal static unsafe void TransitionLayout(IVulkanPlatformGraphicsContext context,
        VulkanCommandBuffer commandBuffer,
        VkImage image,
        VkImageLayout sourceLayout,
        VkAccessFlags sourceAccessMask,
        VkImageLayout destinationLayout,
        VkAccessFlags destinationAccessMask,
        uint mipLevels)
    {
        var subresourceRange = new VkImageSubresourceRange
        {
            aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
            levelCount = mipLevels,
            layerCount = 1
        };

        var barrier = new VkImageMemoryBarrier
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER,
            srcAccessMask = sourceAccessMask,
            dstAccessMask = destinationAccessMask,
            oldLayout = sourceLayout,
            newLayout = destinationLayout,
            srcQueueFamilyIndex = VulkanHelpers.QueueFamilyIgnored,
            dstQueueFamilyIndex = VulkanHelpers.QueueFamilyIgnored,
            image = image,
            subresourceRange = subresourceRange
        };

        context.DeviceApi.CmdPipelineBarrier(
            commandBuffer.Handle,
            VkPipelineStageFlags.VK_PIPELINE_STAGE_ALL_COMMANDS_BIT,
            VkPipelineStageFlags.VK_PIPELINE_STAGE_ALL_COMMANDS_BIT,
            0,
            0,
            IntPtr.Zero,
            0,
            IntPtr.Zero,
            1,
            &barrier);
    }
}