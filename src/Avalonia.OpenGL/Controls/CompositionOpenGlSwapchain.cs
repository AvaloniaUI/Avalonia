using System;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;

namespace Avalonia.OpenGL.Controls;

internal class CompositionOpenGlSwapchain : SwapchainBase<IGlSwapchainImage>
{
    private readonly IGlContext _context;
    private readonly IGlContextExternalObjectsFeature? _externalObjectsFeature;
    private readonly IOpenGlTextureSharingRenderInterfaceContextFeature? _sharingFeature;

    public CompositionOpenGlSwapchain(IGlContext context, ICompositionGpuInterop interop, CompositionDrawingSurface target,
        IOpenGlTextureSharingRenderInterfaceContextFeature sharingFeature
        ) : base(interop, target)
    {
        _context = context;
        _sharingFeature = sharingFeature;
    }
    
    public CompositionOpenGlSwapchain(IGlContext context, ICompositionGpuInterop interop, CompositionDrawingSurface target,
        IGlContextExternalObjectsFeature? externalObjectsFeature) : base(interop, target)
    {
        _context = context;
        _externalObjectsFeature = externalObjectsFeature;
    }
    
    

    protected override IGlSwapchainImage CreateImage(PixelSize size)
    {
        if (_sharingFeature != null)
            return new CompositionOpenGlSwapChainImage(_context, _sharingFeature, size, Interop, Target);
        return new DxgiMutexOpenGlSwapChainImage(Interop, Target, _externalObjectsFeature!, size);
    }

    public IDisposable BeginDraw(PixelSize size, out IGlTexture texture)
    {
        var rv = BeginDrawCore(size, out var tex);
        texture = tex;
        return rv;
    }
}

internal interface IGlTexture
{
    int TextureId { get; }
    int InternalFormat { get; }
    PixelSize Size { get; }
}


interface IGlSwapchainImage : ISwapchainImage, IGlTexture
{
    
}
internal class DxgiMutexOpenGlSwapChainImage : IGlSwapchainImage
{
    private readonly ICompositionGpuInterop _interop;
    private readonly CompositionDrawingSurface _surface;
    private readonly IGlExportableExternalImageTexture _texture;
    private Task? _lastPresent;
    private ICompositionImportedGpuImage? _imported;

    public DxgiMutexOpenGlSwapChainImage(ICompositionGpuInterop interop, CompositionDrawingSurface surface,
        IGlContextExternalObjectsFeature externalObjects, PixelSize size)
    {
        _interop = interop;
        _surface = surface;
        _texture = externalObjects.CreateImage(KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureGlobalSharedHandle,
            size, PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm);
    }
    public async ValueTask DisposeAsync()
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
        _texture.Dispose();
    }

    public int TextureId => _texture.TextureId;
    public int InternalFormat => _texture.InternalFormat;
    public PixelSize Size => new(_texture.Properties.Width, _texture.Properties.Height);
    public Task? LastPresent => _lastPresent;
    public void BeginDraw() => _texture.AcquireKeyedMutex(0);

    public void Present()
    {
        _texture.ReleaseKeyedMutex(1);
        _imported ??= _interop.ImportImage(_texture.GetHandle(), _texture.Properties);
        _lastPresent = _surface.UpdateWithKeyedMutexAsync(_imported, 1, 0);
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

    
    public async ValueTask DisposeAsync()
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

        _texture.Dispose();
    }

    public int TextureId => _texture.TextureId;
    public int InternalFormat => _texture.InternalFormat;
    public PixelSize Size => _texture.Size;
    public Task? LastPresent { get; private set; }
    public void BeginDraw()
    {
        // No-op for texture sharing
    }

    public void Present()
    {
        _imported ??= _interop.ImportImage(_texture);
        LastPresent = _target.UpdateAsync(_imported);
    }
}
