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
        uint mipLevels, uint appQueueFamily, InteropTransferDirection direction)
    {
        var subresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, mipLevels, 0, 1);

        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            SrcAccessMask = sourceAccessMask,
            DstAccessMask = destinationAccessMask,
            OldLayout = sourceLayout,
            NewLayout = destinationLayout,
            SrcQueueFamilyIndex = direction == InteropTransferDirection.None ? Vk.QueueFamilyIgnored
                : direction == InteropTransferDirection.AppToAvalonia ? appQueueFamily : Vk.QueueFamilyExternal,
            DstQueueFamilyIndex = direction == InteropTransferDirection.None ? Vk.QueueFamilyIgnored 
                : direction == InteropTransferDirection.AppToAvalonia ? Vk.QueueFamilyExternal : appQueueFamily,
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
