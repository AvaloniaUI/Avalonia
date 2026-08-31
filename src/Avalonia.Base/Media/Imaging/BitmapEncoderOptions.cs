using System.IO;

namespace Avalonia.Media.Imaging;

/// <summary>
/// Represents the options used while saving a bitmap using <see cref="Bitmap.Save(Stream, BitmapEncoderOptions)"/>.
/// Common implementations are <see cref="PngBitmapEncoderOptions"/>, <see cref="JpegBitmapEncoderOptions"/>.
/// </summary>
public abstract class BitmapEncoderOptions
{
    internal BitmapEncoderOptions()
    {
    }
}
