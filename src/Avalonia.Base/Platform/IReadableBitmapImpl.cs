using Avalonia.Metadata;

namespace Avalonia.Platform;

[PrivateApi]
public interface IReadableBitmapImpl : IBitmapImpl
{
    PixelFormat? Format { get; }
    AlphaFormat? AlphaFormat { get; }
    ILockedFramebuffer Lock();
}
