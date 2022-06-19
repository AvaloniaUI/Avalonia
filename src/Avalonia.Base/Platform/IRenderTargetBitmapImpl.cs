using Avalonia.Metadata;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for a
    /// <see cref="Avalonia.Media.Imaging.RenderTargetBitmap"/>.
    /// </summary>
    [Unstable]
    public interface IRenderTargetBitmapImpl : IBitmapImpl, IRenderTarget
    {
    }
}
