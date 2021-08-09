using System.Collections.Generic;

namespace Avalonia.OpenGL
{
    public interface IPlatformOpenGlInterface
    {
        IGlContext PrimaryContext { get; }
        IGlContext CreateSharedContext();
        bool CanShareContexts { get; }
        bool CanCreateContexts { get; }
        IGlContext CreateContext();
        IGlContext CreateContext(IGlContext shareWith, IList<GlVersion> probeVersions);
        IGlContextWithOSTextureSharing CreateOSTextureSharingCompatibleContext(IGlContext shareWith, IList<GlVersion> probeVersions);
    }
}
