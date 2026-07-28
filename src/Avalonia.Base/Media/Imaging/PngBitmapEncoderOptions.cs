using System.IO.Compression;

namespace Avalonia.Media.Imaging;

/// <summary>
/// Represents the options used to save a bitmap in the PNG format.
/// </summary>
public sealed class PngBitmapEncoderOptions : BitmapEncoderOptions
{
    /// <summary>
    /// Gets the default PNG encoder options.
    /// </summary>
    public static PngBitmapEncoderOptions Default { get; } = new();
    
    /// <summary>
    /// Gets or sets the compression level to use.
    /// Defaults to <see cref="System.IO.Compression.CompressionLevel.Optimal"/>.
    /// </summary>
    public CompressionLevel CompressionLevel { get; init; } = CompressionLevel.Optimal;
}
