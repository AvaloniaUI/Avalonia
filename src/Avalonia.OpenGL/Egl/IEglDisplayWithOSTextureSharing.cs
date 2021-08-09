using System.Collections.Generic;

namespace Avalonia.OpenGL.Egl
{
    public interface IEglDisplayWithOSTextureSharing
    {
        IGlOSSharedTexture CreateOSSharedTexture(EglContext ctx, string type, int width, int height);
        bool SupportsOSSharedTextureType(EglContext ctx, string type);
        IGlOSSharedTexture ImportOSSharedTexture(EglContext ctx, IGlOSSharedTexture osSharedTexture);
        bool AreOSTextureSharingCompatible(EglContext ctx, IGlContext compatibleWith);
        IGlOSSharedTexture CreateOSSharedTexture(EglContext ctx, IGlContext compatibleWith, int width, int height);
        IGlContextWithOSTextureSharing CreateOSTextureSharingCompatibleContext(EglContext compatibleWith, IGlContext shareWith, IList<GlVersion> probeVersions);
    }
}
