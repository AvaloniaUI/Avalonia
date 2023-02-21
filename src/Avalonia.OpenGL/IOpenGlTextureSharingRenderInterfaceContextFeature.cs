using System.Collections.Generic;
using Avalonia.Rendering.Composition;

namespace Avalonia.OpenGL
{
    public interface IOpenGlTextureSharingRenderInterfaceContextFeature
    {
        bool CanCreateSharedContext { get; }
        IGlContext? CreateSharedContext(IEnumerable<GlVersion>? preferredVersions = null);
        ICompositionImportableOpenGlSharedTexture CreateSharedTextureForComposition(IGlContext context, PixelSize size);
    }

    public interface ICompositionImportableOpenGlSharedTexture : ICompositionImportableSharedGpuContextImage
    {
        int TextureId { get; }
        int InternalFormat { get; }
        PixelSize Size { get; }
    }
}
