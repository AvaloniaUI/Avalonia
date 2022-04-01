using System;
using Silk.NET.Vulkan;

namespace Avalonia.Vulkan
{
    public class VulkanQueue
    {
        public VulkanQueue(VulkanDevice device, Queue apiHandle)
        {
            InternalHandle = apiHandle;
        }
        
        public IntPtr Handle => InternalHandle.Handle;
        internal Queue InternalHandle { get; }
    }
}
