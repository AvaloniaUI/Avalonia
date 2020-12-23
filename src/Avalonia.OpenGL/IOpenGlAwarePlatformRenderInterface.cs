using Avalonia.OpenGL.Imaging;

namespace Avalonia.OpenGL
{
    public interface IOpenGlAwarePlatformRenderInterface
    {
        IOpenGlBitmapImpl CreateOpenGlBitmap(PixelSize size, Vector dpi);
    }
}
