namespace Avalonia.Platform;

public interface IReadableBitmapImpl
{
    PixelFormat? Format { get; }
    ILockedFramebuffer Lock();
}