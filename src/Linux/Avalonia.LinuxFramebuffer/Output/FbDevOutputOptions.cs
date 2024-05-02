using Avalonia.Platform;

namespace Avalonia.LinuxFramebuffer.Output;

public class FbDevOutputOptions
{
    /// <summary>
    /// The frame buffer device name.
    /// Defaults to the value in environment variable FRAMEBUFFER or /dev/fb0 when FRAMEBUFFER is not set
    /// </summary>
    public string? FileName { get; set; }
    /// <summary>
    /// The required pixel format for the frame buffer.
    /// A null value will leave the frame buffer in the current pixel format.
    /// Otherwise sets the frame buffer to the required format
    /// </summary>
    public PixelFormat? PixelFormat { get; set; }
    /// <summary>
    /// If set to true, double-buffering will be disabled and scene will be composed directly into mmap-ed memory region
    /// While this mode saves a blit, you need to check if it won't cause rendering artifacts your particular device.
    /// </summary>
    public bool RenderDirectlyToMappedMemory { get; set; }

    /// <summary>
    /// The initial scale factor to use
    /// </summary>
    public double Scaling { get; set; } = 1;
}