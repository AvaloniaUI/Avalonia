using Avalonia.OpenGL;

namespace Avalonia.Skia;

internal class GlSkiaSharedTextureForComposition : ICompositionImportableOpenGlSharedTexture
{
    public IGlContext Context { get; }
    private readonly object _lock = new();

    public GlSkiaSharedTextureForComposition(IGlContext context, int textureId, int internalFormat, PixelSize size)
    {
        Context = context;
        TextureId = textureId;
        InternalFormat = internalFormat;
        Size = size;
    }
    public void Dispose(IGlContext context)
    {
        lock (_lock)
        {
            if(TextureId == 0)
                return;
            try
            {
                using (context.EnsureCurrent())
                    context.GlInterface.DeleteTexture(TextureId);
            }
            catch
            {
                // Ignore
            }
            
            TextureId = 0;
        }
    }

    public int TextureId { get; private set; }
    public int InternalFormat { get; }
    public PixelSize Size { get; }
    public void Dispose()
    {
        Dispose(Context);
    }
}