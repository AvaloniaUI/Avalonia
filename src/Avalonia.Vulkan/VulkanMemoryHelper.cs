using Silk.NET.Vulkan;

namespace Avalonia.Vulkan
{
    internal static class VulkanMemoryHelper
    {
        internal static int FindSuitableMemoryTypeIndex(VulkanPhysicalDevice physicalDevice, uint memoryTypeBits,
            MemoryPropertyFlags flags)
        {
            physicalDevice.Api.GetPhysicalDeviceMemoryProperties(physicalDevice.InternalHandle, out var properties);

            for (var i = 0; i < properties.MemoryTypeCount; i++)
            {
                var type = properties.MemoryTypes[i];

                if ((memoryTypeBits & (1 << i)) != 0 && type.PropertyFlags.HasFlag(flags)) return i;
            }

            return -1;
        }

        internal static unsafe void TransitionLayout(VulkanDevice device,
            CommandBuffer commandBuffer,
            Image image,
            ImageLayout sourceLayout,
            AccessFlags sourceAccessMask,
            ImageLayout destinationLayout,
            AccessFlags destinationAccessMask,
            uint mipLevels)
        {
            var subresourceRange = new ImageSubresourceRange(ImageAspectFlags.ImageAspectColorBit, 0, mipLevels, 0, 1);

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

            device.Api.CmdPipelineBarrier(
                commandBuffer,
                PipelineStageFlags.PipelineStageAllCommandsBit,
                PipelineStageFlags.PipelineStageAllCommandsBit,
                0,
                0,
                null,
                0,
                null,
                1,
                barrier);
        }
    }
}
