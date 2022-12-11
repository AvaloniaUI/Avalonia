using System.Collections.Generic;
using Avalonia.OpenGL.Imaging;
using Avalonia.Platform;

namespace Avalonia.OpenGL
{
    public interface IOpenGlTextureSharingRenderInterfaceContextFeature
    {
        bool CanCreateSharedContext { get; }
        IGlContext CreateSharedContext(IEnumerable<GlVersion> preferredVersions = null);
        IOpenGlBitmapImpl CreateOpenGlBitmap(PixelSize size, Vector dpi);
    }
}
