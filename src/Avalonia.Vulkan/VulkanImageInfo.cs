namespace Avalonia.Vulkan;

public record struct VulkanImageInfo
{
    public uint Format { get; set; }
    public PixelSize PixelSize { get; set; }
    public ulong Handle { get; set; }
    public uint Layout { get; set; }
    public uint Tiling { get; set; }
    public uint UsageFlags { get; set; }
    public uint LevelCount { get; set; }
    public uint SampleCount { get; set; }
    public ulong MemoryHandle { get; set; }
    public ulong ViewHandle { get; set; }
    public ulong MemorySize { get; set; }
    public bool IsProtected { get; set; }
}
