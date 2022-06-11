using System;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.OpenGL.Imaging
{
    [Unstable]
    public interface IOpenGlBitmapImpl : IBitmapImpl
    {
        IOpenGlBitmapAttachment CreateFramebufferAttachment(IGlContext context, Action presentCallback);
        bool SupportsContext(IGlContext context);
    }

    [Unstable]
    public interface IOpenGlBitmapAttachment : IDisposable
    {
        void Present();
    }
}
