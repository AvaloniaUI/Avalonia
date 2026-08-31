namespace Avalonia.Media.Imaging;

/// <summary>
/// Represents the options used to save a bitmap in the JPEG format.
/// </summary>
public sealed class JpegBitmapEncoderOptions : BitmapEncoderOptions
{
    /// <summary>
    /// Gets the default JPEG encoder options.
    /// </summary>
    public static JpegBitmapEncoderOptions Default { get; } = new();
    
    /// <summary>
    /// Gets or sets the quality to use, from 0 (lowest) to 100 (highest).
    /// Defaults to 100.
    /// </summary>
    public int Quality { get; init; } = 100;
}
