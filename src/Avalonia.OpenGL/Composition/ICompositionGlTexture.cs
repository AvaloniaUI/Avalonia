using System;
using System.Threading.Tasks;
using Avalonia.Metadata;
using Avalonia.Rendering.Composition;

namespace Avalonia.OpenGL;

/// <summary>
/// A texture that can be drawn to by user code and presented to a <see cref="CompositionDrawingSurface"/>.
/// Create instances via <see cref="ICompositionGlContext.CreateTexture"/>.
/// </summary>
/// <remarks>
/// All members can only be used on the thread the associated <see cref="Compositor"/> belongs to.
/// </remarks>
[NotClientImplementable]
public interface ICompositionGlTexture : IAsyncDisposable
{
    /// <summary>
    /// The pixel size of the texture.
    /// </summary>
    PixelSize Size { get; }

    /// <summary>
    /// Returns true when the texture can be drawn to: there is no active lease and the last
    /// presentation, if any, has completed successfully.
    /// A presentation failure leaves this property permanently false, check
    /// <see cref="ICompositionGlContext.IsValidForInterop"/> in that case.
    /// </summary>
    bool IsReadyForDraw { get; }

    /// <summary>
    /// Prepares the texture to be rendered to by user code.
    /// User code can bind the texture to their framebuffer, however the texture should be unbound
    /// before ending the returned lease.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The texture is not ready for drawing (<see cref="IsReadyForDraw"/> is false) or the owning
    /// context is no longer valid for interop.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The texture has been disposed.</exception>
    ICompositionGlTextureLease BeginDraw();
}

/// <summary>
/// An active drawing session on an <see cref="ICompositionGlTexture"/>.
/// The lease is ended either by <see cref="PresentAsync"/> or by <see cref="IDisposable.Dispose"/>,
/// the latter discards the frame. Disposing after <see cref="PresentAsync"/> is a no-op.
/// </summary>
[NotClientImplementable]
public interface ICompositionGlTextureLease : IDisposable
{
    /// <summary>
    /// Describes the OpenGL texture to draw into. Only valid until the lease ends;
    /// <see cref="CompositionGlTextureInfo.TextureId"/> is not guaranteed to be stable between leases.
    /// </summary>
    CompositionGlTextureInfo Texture { get; }

    /// <summary>
    /// Ends the lease and asynchronously presents the texture to the associated
    /// <see cref="CompositionDrawingSurface"/>. This will happen when the next composition commit
    /// is processed by the render thread.
    /// The texture must be unbound from user framebuffers before calling this method.
    /// The returned task completes when the texture can be drawn to again, NOT when the frame
    /// becomes visible on the screen.
    /// You should NOT await the returned task while keeping hold of a
    /// <see cref="IGlContext.MakeCurrent"/> disposable.
    /// </summary>
    Task PresentAsync();
}

/// <summary>
/// Describes an OpenGL texture provided by <see cref="ICompositionGlTextureLease"/>.
/// </summary>
public readonly record struct CompositionGlTextureInfo(int TextureId, int Target, int InternalFormat, PixelSize Size);
