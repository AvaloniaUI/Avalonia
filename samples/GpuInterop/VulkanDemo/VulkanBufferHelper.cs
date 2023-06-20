using System;
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using SilkNetDemo;
using Buffer = Silk.NET.Vulkan.Buffer;
using SystemBuffer = System.Buffer;

namespace GpuInterop.VulkanDemo;

static class VulkanBufferHelper
{
    public unsafe static void AllocateBuffer<T>(VulkanContext vk,
        BufferUsageFlags bufferUsageFlags,
        out Buffer buffer, out DeviceMemory memory,
        Span<T> initialData) where T:unmanaged
    {
        var api = vk.Api;
        var device = vk.Device;

        var size = Unsafe.SizeOf<T>() * initialData.Length;
        var bufferInfo = new BufferCreateInfo()
        {
            SType = StructureType.BufferCreateInfo,
            Size = (ulong)size,
            Usage = bufferUsageFlags,
            SharingMode = SharingMode.Exclusive
        };
        api.CreateBuffer(device, bufferInfo, null, out buffer).ThrowOnError();

        api.GetBufferMemoryRequirements(device, buffer, out var memoryRequirements);

        var physicalDevice = vk.PhysicalDevice;

        var memoryAllocateInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memoryRequirements.Size,
            MemoryTypeIndex = (uint)FindSuitableMemoryTypeIndex(api,
                physicalDevice,
                memoryRequirements.MemoryTypeBits,
                MemoryPropertyFlags.HostCoherentBit |
                MemoryPropertyFlags.HostVisibleBit)
        };

        api.AllocateMemory(device, memoryAllocateInfo, null, out memory).ThrowOnError();
        api.BindBufferMemory(device, buffer, memory, 0);
        UpdateBufferMemory(vk, memory, initialData);
    }

    public static unsafe void UpdateBufferMemory<T>(VulkanContext vk, DeviceMemory memory,
        Span<T> data) where T : unmanaged
    {
        var api = vk.Api;
        var device = vk.Device;

        var size = data.Length * Unsafe.SizeOf<T>();
        void* pointer = null;
        api.MapMemory(device, memory, 0, (ulong)size, 0, ref pointer);

        data.CopyTo(new Span<T>(pointer, size));
        
        api.UnmapMemory(device, memory);

    }

    private static int FindSuitableMemoryTypeIndex(Vk api, PhysicalDevice physicalDevice, uint memoryTypeBits,
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
}
