using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.OpenGL.Imaging
{
    public interface IOpenGlBitmapImpl : IBitmapImpl
    {
        IOpenGlBitmapAttachment CreateFramebufferAttachment(IGlContext context, Action presentCallback);
        bool SupportsContext(IGlContext context);
    }

    public interface IOpenGlBitmapAttachment : IDisposable
    {
        void Present();
    }
}
