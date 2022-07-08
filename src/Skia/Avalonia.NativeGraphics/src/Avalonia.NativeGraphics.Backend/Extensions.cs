using Avalonia.Native.Interop;

namespace Avalonia.NativeGraphics.Backend
{
    internal static class Extensions
    {
        public static AvgPixelSize ToAvgPixelSize(this PixelSize s) => new AvgPixelSize
        {
            Width = s.Width,
            Height = s.Height
        };
    }
}