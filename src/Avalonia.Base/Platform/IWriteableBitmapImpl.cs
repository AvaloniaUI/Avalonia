using Avalonia.Metadata;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for a <see cref="Avalonia.Media.Imaging.WriteableBitmap"/>.
    /// </summary>
    [Unstable]
    public interface IWriteableBitmapImpl : IBitmapImpl, IReadableBitmapWithAlphaImpl
    {
    }
}
