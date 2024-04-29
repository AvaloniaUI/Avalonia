using Avalonia.Metadata;

namespace Avalonia.Platform;

public interface IReadableBitmapImpl
{
    PixelFormat? Format { get; }
    ILockedFramebuffer Lock();
}

//TODO12: Remove me once we can change IReadableBitmapImpl
[Unstable]
public interface IReadableBitmapWithAlphaImpl : IReadableBitmapImpl
{
    AlphaFormat? AlphaFormat { get; }
}
