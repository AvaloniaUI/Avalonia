using System;

namespace Avalonia.OpenGL
{
    public interface IGlContextWithOSTextureSharing : IGlContext
    {
        IGlOSSharedTexture CreateOSSharedTexture(string type, int width, int height);
        bool SupportsOSSharedTextureType(string type);
        IGlOSSharedTexture ImportOSSharedTexture(IGlOSSharedTexture osSharedTexture);
        bool AreOSTextureSharingCompatible(IGlContext compatibleWith);
        IGlOSSharedTexture CreateOSSharedTexture(IGlContext compatibleWith, int width, int height);
    }

    public interface IGlOSSharedTexture : IDisposable
    {
        public int TextureId { get; }
        public int Fbo { get; }
        IDisposable Lock();
        int Width { get; }
        int Height { get; }
    }
}
