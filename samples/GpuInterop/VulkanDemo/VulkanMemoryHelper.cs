using Silk.NET.Vulkan;

namespace GpuInterop.VulkanDemo;

internal static class VulkanMemoryHelper
{
    internal static int FindSuitableMemoryTypeIndex(Vk api, PhysicalDevice physicalDevice, uint memoryTypeBits,
        MemoryPropertyFlags flags)
    {
        api.GetPhysicalDeviceMemoryProperties(physicalDevice, out var properties);

        for (var i = 0; i < properties.MemoryTypeCount; i++)
        {
            var type = properties.MemoryTypes[i];

            if ((memoryTypeBits & (1 << i)) != 0 && type.PropertyFlags.HasFlag(flags)) return i;
        }

        return -1;
    }

    internal static unsafe void TransitionLayout(
        Vk api,
        CommandBuffer commandBuffer,
        Image image,
        ImageLayout sourceLayout,
        AccessFlags sourceAccessMask,
        ImageLayout destinationLayout,
        AccessFlags destinationAccessMask,
        uint mipLevels)
    {
        var subresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, mipLevels, 0, 1);

        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            SrcAccessMask = sourceAccessMask,
            DstAccessMask = destinationAccessMask,
            OldLayout = sourceLayout,
            NewLayout = destinationLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange = subresourceRange
        };

        api.CmdPipelineBarrier(
            commandBuffer,
            PipelineStageFlags.AllCommandsBit,
            PipelineStageFlags.AllCommandsBit,
            0,
            0,
            null,
            0,
            null,
            1,
            barrier);
    }
}
