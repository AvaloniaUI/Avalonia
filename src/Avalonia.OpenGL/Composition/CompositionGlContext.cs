using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Rendering.Composition;

namespace Avalonia.OpenGL;

internal sealed class CompositionGlContext : ICompositionGlContext
{
    private readonly ICompositionGpuInterop _interop;
    private readonly IOpenGlTextureSharingRenderInterfaceContextFeature? _sharingFeature;
    private readonly IGlContextExternalObjectsFeature? _externalObjects;
    private readonly List<CompositionGlTexture> _textures = new();
    private bool _disposed;

    public CompositionGlContext(Compositor compositor, IGlContext glContext, ICompositionGpuInterop interop,
        IOpenGlTextureSharingRenderInterfaceContextFeature? sharingFeature,
        IGlContextExternalObjectsFeature? externalObjects)
    {
        Compositor = compositor;
        GlContext = glContext;
        _interop = interop;
        _sharingFeature = sharingFeature;
        _externalObjects = externalObjects;
    }

    public Compositor Compositor { get; }

    public IGlContext GlContext { get; }

    public bool IsValidForInterop => !_disposed && !GlContext.IsLost && !_interop.IsLost;

    public ICompositionGlTexture CreateTexture(CompositionDrawingSurface surface, PixelSize size)
    {
        if (surface == null)
            throw new ArgumentNullException(nameof(surface));
        Compositor.Dispatcher.VerifyAccess();
        if (_disposed)
            throw new ObjectDisposedException(nameof(ICompositionGlContext));
        if (!IsValidForInterop)
            throw new InvalidOperationException("This context is no longer valid for interop with the compositor");
        if (surface.Compositor != Compositor)
            throw new InvalidOperationException("The surface belongs to a different Compositor");
        if (size.Width < 1 || size.Height < 1)
            throw new ArgumentOutOfRangeException(nameof(size));

        CompositionGlTexture texture = _sharingFeature != null
            ? new SharedCompositionGlTexture(this, _sharingFeature, _interop, surface, size)
            : new ExternalImageCompositionGlTexture(this, _externalObjects!, _interop, surface, size);
        _textures.Add(texture);
        return texture;
    }

    internal void OnTextureDisposed(CompositionGlTexture texture) => _textures.Remove(texture);

    public async ValueTask DisposeAsync()
    {
        Compositor.Dispatcher.VerifyAccess();
        if (_disposed)
            return;
        _disposed = true;

        foreach (var texture in _textures.ToArray())
            await texture.DisposeAsync();
        _textures.Clear();

        GlContext.Dispose();
    }
}
