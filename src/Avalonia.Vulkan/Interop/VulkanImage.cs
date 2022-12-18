using System;
using Avalonia.Vulkan.Interop;
using Avalonia.Vulkan.UnmanagedInterop;

namespace Avalonia.Vulkan;

internal class VulkanImage : IDisposable
{
    private IVulkanPlatformGraphicsContext _context;

    private VkAccessFlags _currentAccessFlags;
    public VkImageUsageFlags UsageFlags { get; }
    private IntPtr _imageView;
    private IntPtr _imageMemory;
    private IntPtr _handle;
    private VulkanCommandBufferPool _commandBufferPool;
    internal IntPtr Handle => _handle;
    internal VkFormat Format { get; }
    internal VkImageAspectFlags AspectFlags { get; private set; }
    public uint MipLevels { get; private set; }
    public PixelSize Size { get; }
    public ulong MemorySize { get; private set; }
    public VkImageLayout CurrentLayout { get; private set; }
    public IntPtr MemoryHandle => _imageMemory;

    public VkImageTiling Tiling => VkImageTiling.VK_IMAGE_TILING_OPTIMAL;


    public VulkanImage(IVulkanPlatformGraphicsContext context,
        VulkanCommandBufferPool commandBufferPool,
        VkFormat format, PixelSize size, uint mipLevels = 0)
    {
        Format = format;
        Size = size;
        MipLevels = mipLevels;
        _context = context;
        _commandBufferPool = commandBufferPool;
        UsageFlags = VkImageUsageFlags.VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT
                           | VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT
                           | VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_SRC_BIT
                           | VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT;
        Initialize();
    }

    public unsafe void Initialize()
    {
        if (Handle != IntPtr.Zero)
            return;
        MipLevels = MipLevels != 0 ? MipLevels : (uint)Math.Floor(Math.Log(Math.Max(Size.Width, Size.Height), 2));
        var createInfo = new VkImageCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
            imageType = VkImageType.VK_IMAGE_TYPE_2D,
            format = Format,
            extent = new VkExtent3D
            {
                depth = 1,
                width = (uint)Size.Width,
                height = (uint)Size.Height
            },
            mipLevels = MipLevels,
            arrayLayers = 1,
            samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
            tiling = Tiling,
            usage = UsageFlags,
            sharingMode = VkSharingMode.VK_SHARING_MODE_EXCLUSIVE,
            initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED,
            flags = VkImageCreateFlags.VK_IMAGE_CREATE_MUTABLE_FORMAT_BIT
        };
        
        _context.DeviceApi.CreateImage(_context.Device.Handle, ref createInfo, IntPtr.Zero, out _handle)
            .ThrowOnError("vkCreateImage");

        _context.DeviceApi.GetImageMemoryRequirements(_context.Device.Handle, _handle, out var memoryRequirements);
        var memoryAllocateInfo = new VkMemoryAllocateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO,
            allocationSize = memoryRequirements.size,
            memoryTypeIndex = (uint)VulkanMemoryHelper.FindSuitableMemoryTypeIndex(_context,
                memoryRequirements.memoryTypeBits,
                VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT),
        };

        _context.DeviceApi.AllocateMemory(_context.Device.Handle, ref memoryAllocateInfo, IntPtr.Zero,
            out _imageMemory).ThrowOnError("vkAllocateMemory");

        _context.DeviceApi.BindImageMemory(_context.Device.Handle, _handle, _imageMemory, 0)
            .ThrowOnError("vkBindImageMemory");

        MemorySize = memoryRequirements.size;
        AspectFlags = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT;

        var imageViewCreateInfo = new VkImageViewCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_VIEW_CREATE_INFO,
            components = new(),
            subresourceRange = new()
            {
                aspectMask = AspectFlags,
                levelCount = MipLevels,
                layerCount = 1
            },
            format = Format,
            image = _handle,
            viewType = VkImageViewType.VK_IMAGE_VIEW_TYPE_2D
        };
        _context.DeviceApi.CreateImageView(_context.Device.Handle, ref imageViewCreateInfo,
            IntPtr.Zero, out _imageView).ThrowOnError("vkCreateImageView");
        CurrentLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED;

        TransitionLayout(VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL, VkAccessFlags.VK_ACCESS_NONE_KHR);
    }

    internal void TransitionLayout(VkImageLayout destinationLayout, VkAccessFlags destinationAccessFlags)
    {
        var commandBuffer = _commandBufferPool!.CreateCommandBuffer();
        commandBuffer.BeginRecording();
        VulkanMemoryHelper.TransitionLayout(_context, commandBuffer, Handle,
            CurrentLayout, _currentAccessFlags, destinationLayout, destinationAccessFlags,
            MipLevels);
        commandBuffer.EndRecording();
        commandBuffer.Submit();
        CurrentLayout = destinationLayout;
        _currentAccessFlags = destinationAccessFlags;
    }
    
    public void TransitionLayout(uint destinationLayout, uint destinationAccessFlags)
    {
        TransitionLayout((VkImageLayout)destinationLayout, (VkAccessFlags)destinationAccessFlags);
    }

    public void Dispose()
    {
        var api = _context.DeviceApi;
        var d = _context.Device.Handle;
        if (_imageView != IntPtr.Zero)
        {
            api.DestroyImageView(d, _imageView, IntPtr.Zero);
            _imageView = IntPtr.Zero;
        }
        if (_handle != IntPtr.Zero)
        {
            api.DestroyImage(d, _handle, IntPtr.Zero);
            _handle = IntPtr.Zero;
        }
        if (_imageMemory != IntPtr.Zero)
        {
            api.FreeMemory(d, _imageMemory, IntPtr.Zero);
            _imageMemory = IntPtr.Zero;
        }
    }
}