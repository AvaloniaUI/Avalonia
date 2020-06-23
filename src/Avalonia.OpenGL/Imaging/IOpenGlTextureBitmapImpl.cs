using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.OpenGL.Imaging
{
    public interface IOpenGlTextureBitmapImpl : IBitmapImpl
    {
        IDisposable Lock();
        void SetBackBuffer(int textureId, int internalFormat, PixelSize pixelSize, double dpiScaling);
        void SetDirty();
    }
}
