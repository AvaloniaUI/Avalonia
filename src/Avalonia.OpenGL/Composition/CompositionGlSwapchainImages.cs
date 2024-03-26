using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;

namespace Avalonia.OpenGL.Composition;

interface IGlSwapchainImage
{
    int TextureId { get; }
    int InternalFormat { get; }
    PixelSize Size { get; }
    ValueTask DisposeImportedAsync();
    void DisposeTexture();
    void BeginDraw();
    Task Present();
}

internal class DxgiMutexOpenGlSwapChainImage : IGlSwapchainImage
{
    private readonly ICompositionGpuInterop _interop;
    private readonly CompositionDrawingSurface _surface;
    private readonly IGlExportableExternalImageTexture _texture;
    private ICompositionImportedGpuImage? _imported;

    public DxgiMutexOpenGlSwapChainImage(ICompositionGpuInterop interop, CompositionDrawingSurface surface,
        IGlContextExternalObjectsFeature externalObjects, PixelSize size)
    {
        _interop = interop;
        _surface = surface;
        _texture = externalObjects.CreateImage(KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureGlobalSharedHandle,
            size, PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm);
    }
    
    public async ValueTask DisposeImportedAsync()
    {
        // The texture is already sent to the compositor, so we need to wait for its attempts to use the texture
        // before destroying it
        if (_imported != null)
        {
            // No need to wait for import / LastPresent since calls are serialized on the compositor side anyway
            try
            {
                await _imported.DisposeAsync();
            }
            catch
            {
                // Ignore
            }
        }
    }

    public void DisposeTexture() => _texture.Dispose();

    public int TextureId => _texture.TextureId;
    public int InternalFormat => _texture.InternalFormat;
    public PixelSize Size => new(_texture.Properties.Width, _texture.Properties.Height);
    public void BeginDraw() => _texture.AcquireKeyedMutex(0);

    public Task Present()
    {
        _texture.ReleaseKeyedMutex(1);
        _imported ??= _interop.ImportImage(_texture.GetHandle(), _texture.Properties);
        return _surface.UpdateWithKeyedMutexAsync(_imported, 1, 0);
    }
}

internal class CompositionOpenGlSwapChainImage : IGlSwapchainImage
{
    private readonly ICompositionGpuInterop _interop;
    private readonly CompositionDrawingSurface _target;
    private readonly ICompositionImportableOpenGlSharedTexture _texture;
    private ICompositionImportedGpuImage? _imported;

    public CompositionOpenGlSwapChainImage(
        IGlContext context,
        IOpenGlTextureSharingRenderInterfaceContextFeature sharingFeature,
        PixelSize size,
        ICompositionGpuInterop interop,
        CompositionDrawingSurface target)
    {
        _interop = interop;
        _target = target;
        _texture = sharingFeature.CreateSharedTextureForComposition(context, size);
    }
    
    public async ValueTask DisposeImportedAsync()
    {
        // The texture is already sent to the compositor, so we need to wait for its attempts to use the texture
        // before destroying it
        if (_imported != null)
        {
            // No need to wait for import / LastPresent since calls are serialized on the compositor side anyway
            try
            {
                await _imported.DisposeAsync();
            }
            catch
            {
                // Ignore
            }
        }
    }

    public void DisposeTexture() => _texture.Dispose();

    public int TextureId => _texture.TextureId;
    public int InternalFormat => _texture.InternalFormat;
    public PixelSize Size => _texture.Size;
    public void BeginDraw()
    {
        // No-op for texture sharing
    }

    public Task Present()
    {
        _imported ??= _interop.ImportImage(_texture);
        return _target.UpdateAsync(_imported);
    }
}
