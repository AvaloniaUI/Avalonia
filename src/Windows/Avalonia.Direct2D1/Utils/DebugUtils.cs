using Avalonia.Direct2D1.Media.Imaging;

namespace Avalonia.Direct2D1.Utils
{
    internal static class DebugUtils
    {
        public static void Save(SharpDX.Direct2D1.BitmapRenderTarget bitmap, string filename)
        {
            var rtb = new D2DRenderTargetBitmapImpl(bitmap);
            rtb.Save(filename);
        }
    }
}
