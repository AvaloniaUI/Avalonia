using System;
using Avalonia.SourceGenerator;
using Avalonia.Vulkan.Interop;
using uint32_t = System.UInt32;
using uint64_t = System.UInt64;
using VkBool32 = System.UInt32;
using VkDeviceSize = System.UInt64;

namespace Avalonia.Vulkan.UnmanagedInterop;


internal unsafe partial class VulkanDeviceApi
{
    public VulkanDeviceApi(IVulkanDevice device)
    {
        Initialize(name =>
        {
            var addr = device.Instance.GetDeviceProcAddress(device.Handle, name);
            if (addr != IntPtr.Zero)
                return addr;
            return device.Instance.GetInstanceProcAddress(device.Instance.Handle, name);
        });

    }

    [GetProcAddress("vkCreateFence")]
    public partial VkResult CreateFence(IntPtr device,
        ref VkFenceCreateInfo pCreateInfo,
        IntPtr pAllocator,
        out IntPtr pFence);

    [GetProcAddress("vkDestroyFence")]
    public partial void DestroyFence(IntPtr device, IntPtr fence, IntPtr pAllocator);

    [GetProcAddress("vkCreateCommandPool")]
    public partial VkResult CreateCommandPool(IntPtr device, ref VkCommandPoolCreateInfo pCreateInfo,
        IntPtr pAllocator, out IntPtr pCommandPool);

    [GetProcAddress("vkDestroyCommandPool")]
    public partial void DestroyCommandPool(IntPtr device, IntPtr pool, IntPtr pAllocator);

    [GetProcAddress("vkAllocateCommandBuffers")]
    public partial VkResult AllocateCommandBuffers(IntPtr device,
        ref VkCommandBufferAllocateInfo pAllocateInfo, IntPtr* pCommandBuffers);

    [GetProcAddress("vkFreeCommandBuffers")]
    public partial void FreeCommandBuffers(IntPtr device, IntPtr commandPool, uint32_t commandBufferCount,
        IntPtr* pCommandBuffers);

    [GetProcAddress("vkWaitForFences")]
    public partial VkResult WaitForFences(IntPtr device, uint32_t fenceCount, IntPtr* pFences, VkBool32 waitAll,
        uint64_t timeout);

    [GetProcAddress("vkBeginCommandBuffer")]
    public partial VkResult BeginCommandBuffer(IntPtr commandBuffer, ref VkCommandBufferBeginInfo pBeginInfo);

    [GetProcAddress("vkEndCommandBuffer")]
    public partial VkResult EndCommandBuffer(IntPtr commandBuffer);

    [GetProcAddress("vkCreateSemaphore")]
    public partial VkResult CreateSemaphore(IntPtr device, ref VkSemaphoreCreateInfo pCreateInfo,
        IntPtr pAllocator, out IntPtr pSemaphore);

    [GetProcAddress("vkDestroySemaphore")]
    public partial void DestroySemaphore(IntPtr device, IntPtr semaphore, IntPtr pAllocator);

    [GetProcAddress("vkResetFences")]
    public partial VkResult ResetFences(IntPtr device, uint32_t fenceCount, IntPtr* pFences);

    [GetProcAddress("vkQueueSubmit")]
    public partial VkResult QueueSubmit(IntPtr queue, uint32_t submitCount, VkSubmitInfo* pSubmits,
        IntPtr fence);

    [GetProcAddress("vkCreateImage")]
    public partial VkResult CreateImage(IntPtr device, ref VkImageCreateInfo pCreateInfo, IntPtr pAllocator,
        out IntPtr pImage);
    
    [GetProcAddress("vkDestroyImage")]
    public partial void DestroyImage(IntPtr device, IntPtr image, IntPtr pAllocator);

    [GetProcAddress("vkGetImageMemoryRequirements")]
    public partial void GetImageMemoryRequirements(IntPtr device, IntPtr image,
        out VkMemoryRequirements pMemoryRequirements);

    [GetProcAddress("vkAllocateMemory")]
    public partial VkResult AllocateMemory(IntPtr device, ref VkMemoryAllocateInfo pAllocateInfo, IntPtr pAllocator,
        out IntPtr pMemory);
    
    [GetProcAddress("vkFreeMemory")]
    public partial void FreeMemory(IntPtr device, IntPtr memory, IntPtr pAllocator);

    [GetProcAddress("vkBindImageMemory")]
    public partial VkResult BindImageMemory(IntPtr device, IntPtr image, IntPtr memory, VkDeviceSize memoryOffset);

    [GetProcAddress("vkCreateImageView")]
    public partial VkResult CreateImageView(IntPtr device, ref VkImageViewCreateInfo pCreateInfo, IntPtr pAllocator,
        out IntPtr pView);
    
    [GetProcAddress("vkDestroyImageView")]
    public partial void DestroyImageView(IntPtr device, IntPtr imageView, IntPtr pAllocator);
    

    [GetProcAddress("vkCmdPipelineBarrier")]
    public partial void CmdPipelineBarrier(IntPtr commandBuffer,
        VkPipelineStageFlags srcStageMask,
        VkPipelineStageFlags dstStageMask,
        VkDependencyFlags dependencyFlags,
        uint32_t memoryBarrierCount,
        IntPtr pMemoryBarriers,
        uint32_t bufferMemoryBarrierCount,
        IntPtr pBufferMemoryBarriers,
        uint32_t imageMemoryBarrierCount,
        VkImageMemoryBarrier* pImageMemoryBarriers);
    
    [GetProcAddress("vkCreateSwapchainKHR")]
    public partial VkResult CreateSwapchainKHR(IntPtr device, ref VkSwapchainCreateInfoKHR pCreateInfo,
        IntPtr pAllocator, out IntPtr pSwapchain);
    
    [GetProcAddress("vkDestroySwapchainKHR")]
    public partial void DestroySwapchainKHR(IntPtr device, IntPtr swapchain, IntPtr pAllocator);

    [GetProcAddress("vkGetSwapchainImagesKHR")]
    public partial VkResult GetSwapchainImagesKHR(IntPtr device, IntPtr swapchain, ref uint32_t pSwapchainImageCount,
        IntPtr* pSwapchainImages);

    [GetProcAddress("vkDeviceWaitIdle")]
    public partial VkResult DeviceWaitIdle(IntPtr device);

    [GetProcAddress("vkAcquireNextImageKHR")]
    public partial VkResult AcquireNextImageKHR(VkDevice device, VkSwapchainKHR swapchain, uint64_t timeout,
        VkSemaphore semaphore, VkFence fence, out uint32_t pImageIndex);

    [GetProcAddress("vkCmdBlitImage")]
    public partial void CmdBlitImage(VkCommandBuffer commandBuffer, VkImage srcImage, VkImageLayout srcImageLayout,
        VkImage dstImage, VkImageLayout dstImageLayout, uint32_t regionCount, VkImageBlit* pRegions, VkFilter filter);

    [GetProcAddress("vkQueuePresentKHR")]
    public partial VkResult vkQueuePresentKHR(VkQueue queue, ref VkPresentInfoKHR pPresentInfo);
}