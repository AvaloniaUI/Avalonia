using Avalonia.Visuals.Media.Imaging;

namespace Avalonia.Media.Imaging
{
    public struct BitmapDecodeOptions
    {
        public PixelSize DecodePixelSize { get; set; }

        public BitmapInterpolationMode InterpolationMode { get; set; }
    }
}
