using Avalonia.OpenGL.Imaging;

namespace Avalonia.OpenGL
{
    public interface IOpenGlAwarePlatformRenderInterface
    {
        IOpenGlTextureBitmapImpl CreateOpenGlTextureBitmap();
    }
}
