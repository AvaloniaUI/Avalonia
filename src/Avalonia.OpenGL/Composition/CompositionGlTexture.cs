using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;

namespace Avalonia.OpenGL;

internal abstract class CompositionGlTexture : ICompositionGlTexture
{
    private TextureLease? _activeLease;
    private Task? _lastPresent;
    private ICompositionImportedGpuImage? _imported;
    private bool _disposed;

    protected CompositionGlTexture(CompositionGlContext owner, ICompositionGpuInterop interop,
        CompositionDrawingSurface surface, PixelSize size)
    {
        Owner = owner;
        Interop = interop;
        Surface = surface;
        Size = size;
    }

    protected CompositionGlContext Owner { get; }
    protected ICompositionGpuInterop Interop { get; }
    protected CompositionDrawingSurface Surface { get; }

    public PixelSize Size { get; }

    public bool IsReadyForDraw =>
        !_disposed
        && _activeLease == null
        && (_lastPresent == null || _lastPresent.Status == TaskStatus.RanToCompletion);

    public ICompositionGlTextureLease BeginDraw()
    {
        Owner.Compositor.Dispatcher.VerifyAccess();
        if (_disposed)
            throw new ObjectDisposedException(nameof(ICompositionGlTexture));
        if (!Owner.IsValidForInterop)
            throw new InvalidOperationException(
                "The owning context is no longer valid for interop with the compositor");
        if (_activeLease != null)
            throw new InvalidOperationException("The previous drawing session is still active");
        if (_lastPresent is { Status: not TaskStatus.RanToCompletion } lastPresent)
            throw new InvalidOperationException(
                lastPresent.IsCompleted
                    ? "The previous presentation has failed"
                    : "The previous presentation hasn't been completed yet");

        using (Owner.GlContext.EnsureCurrent())
            BeginDrawCore();
        return _activeLease = new TextureLease(this);
    }

    private Task Present(TextureLease lease)
    {
        Debug.Assert(_activeLease == lease);
        try
        {
            using (Owner.GlContext.EnsureCurrent())
            {
                Owner.GlContext.GlInterface.Flush();
                PresentGlCore();
            }
        }
        finally
        {
            _activeLease = null;
        }

        _imported ??= Import();
        return _lastPresent = UpdateSurface(_imported);
    }

    private void DiscardDraw(TextureLease lease)
    {
        if (_activeLease != lease)
            return;
        Owner.Compositor.Dispatcher.VerifyAccess();
        _activeLease = null;
        try
        {
            using (Owner.GlContext.EnsureCurrent())
                DiscardDrawCore();
        }
        catch
        {
            // The context is likely lost, nothing to unwind
        }
    }

    public async ValueTask DisposeAsync()
    {
        Owner.Compositor.Dispatcher.VerifyAccess();
        if (_disposed)
            return;
        _activeLease?.Dispose();
        _disposed = true;
        Owner.OnTextureDisposed(this);

        // The texture might have already been sent to the compositor, so we need to wait for its
        // attempts to use the texture before destroying it
        if (_imported != null)
        {
            // No need to wait for import / last present since calls are serialized on the compositor side anyway
            try
            {
                await _imported.DisposeAsync();
            }
            catch
            {
                // Ignore
            }
        }

        try
        {
            DisposeGlResources();
        }
        catch
        {
            // The context is likely lost, nothing to free
        }
    }

    protected abstract CompositionGlTextureInfo GetTextureInfo();
    protected abstract void BeginDrawCore();
    protected abstract void DiscardDrawCore();
    protected abstract void PresentGlCore();
    protected abstract ICompositionImportedGpuImage Import();
    protected virtual Task UpdateSurface(ICompositionImportedGpuImage imported) => Surface.UpdateAsync(imported);
    protected abstract void DisposeGlResources();

    private sealed class TextureLease : ICompositionGlTextureLease
    {
        private CompositionGlTexture? _texture;

        public TextureLease(CompositionGlTexture texture) => _texture = texture;

        public CompositionGlTextureInfo Texture
        {
            get
            {
                var texture = _texture ?? throw new ObjectDisposedException(nameof(ICompositionGlTextureLease));
                texture.Owner.Compositor.Dispatcher.VerifyAccess();
                return texture.GetTextureInfo();
            }
        }

        public Task PresentAsync()
        {
            var texture = _texture ?? throw new ObjectDisposedException(nameof(ICompositionGlTextureLease));
            texture.Owner.Compositor.Dispatcher.VerifyAccess();
            _texture = null;
            return texture.Present(this);
        }

        public void Dispose()
        {
            if (_texture is not { } texture)
                return;
            _texture = null;
            texture.DiscardDraw(this);
        }
    }
}

internal sealed class SharedCompositionGlTexture : CompositionGlTexture
{
    private readonly ICompositionImportableOpenGlSharedTexture _texture;

    public SharedCompositionGlTexture(CompositionGlContext owner,
        IOpenGlTextureSharingRenderInterfaceContextFeature sharingFeature,
        ICompositionGpuInterop interop, CompositionDrawingSurface surface, PixelSize size)
        : base(owner, interop, surface, size)
    {
        _texture = sharingFeature.CreateSharedTextureForComposition(owner.GlContext, size);
    }

    protected override CompositionGlTextureInfo GetTextureInfo() =>
        new(_texture.TextureId, GlConsts.GL_TEXTURE_2D, _texture.InternalFormat, Size);

    protected override void BeginDrawCore()
    {
    }

    protected override void DiscardDrawCore()
    {
    }

    protected override void PresentGlCore()
    {
    }

    protected override ICompositionImportedGpuImage Import() => Interop.ImportImage(_texture);

    protected override void DisposeGlResources() => _texture.Dispose();
}

internal sealed class ExternalImageCompositionGlTexture : CompositionGlTexture
{
    private readonly IGlExportableExternalImageTexture _texture;

    public ExternalImageCompositionGlTexture(CompositionGlContext owner,
        IGlContextExternalObjectsFeature externalObjects,
        ICompositionGpuInterop interop, CompositionDrawingSurface surface, PixelSize size)
        : base(owner, interop, surface, size)
    {
        _texture = externalObjects.CreateImage(
            KnownPlatformGraphicsExternalImageHandleTypes.D3D11TextureGlobalSharedHandle,
            size, PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm);
    }

    protected override CompositionGlTextureInfo GetTextureInfo() =>
        new(_texture.TextureId, _texture.TextureType, _texture.InternalFormat, Size);

    protected override void BeginDrawCore() => _texture.AcquireKeyedMutex(0);

    protected override void DiscardDrawCore() => _texture.ReleaseKeyedMutex(0);

    protected override void PresentGlCore() => _texture.ReleaseKeyedMutex(1);

    protected override ICompositionImportedGpuImage Import() =>
        Interop.ImportImage(_texture.GetHandle(), _texture.Properties);

    protected override Task UpdateSurface(ICompositionImportedGpuImage imported) =>
        Surface.UpdateWithKeyedMutexAsync(imported, 1, 0);

    protected override void DisposeGlResources() => _texture.Dispose();
}
