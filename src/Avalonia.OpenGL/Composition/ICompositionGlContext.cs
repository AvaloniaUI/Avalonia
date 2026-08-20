using System;
using Avalonia.Metadata;
using Avalonia.Rendering.Composition;

namespace Avalonia.OpenGL;

/// <summary>
/// An OpenGL context that is suitable for interop with a <see cref="Rendering.Composition.Compositor"/>.
/// Create instances via <see cref="OpenGlCompositionInterop.TryCreateCompatibleGlContextAsync"/>.
/// </summary>
/// <remarks>
/// All members can only be used on the thread the associated <see cref="Rendering.Composition.Compositor"/>
/// belongs to.
/// </remarks>
[NotClientImplementable]
public interface ICompositionGlContext : IAsyncDisposable
{
    /// <summary>
    /// The associated compositor.
    /// </summary>
    Compositor Compositor { get; }

    /// <summary>
    /// The OpenGL context.
    /// </summary>
    IGlContext GlContext { get; }

    /// <summary>
    /// Returns true if this OpenGL context is still suitable for interop with the Compositor.
    /// This property might become false in cases like a device loss event on the compositor side.
    /// In that case presentation is no longer possible, however <see cref="GlContext"/> itself might
    /// still be usable for salvaging user resources. Once the property becomes false, this instance
    /// should be disposed and recreated via
    /// <see cref="OpenGlCompositionInterop.TryCreateCompatibleGlContextAsync"/>.
    /// A recreated context shares no OpenGL objects with the old one.
    /// </summary>
    bool IsValidForInterop { get; }

    /// <summary>
    /// Creates a texture that can be used to update a <see cref="CompositionDrawingSurface"/> instance.
    /// </summary>
    /// <param name="surface">The surface that will be updated by the texture. Must belong to <see cref="Compositor"/>.</param>
    /// <param name="size">The pixel size of the texture.</param>
    /// <exception cref="InvalidOperationException">
    /// The context is no longer valid for interop or the surface belongs to a different compositor.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The context has been disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="surface"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is empty or negative.</exception>
    ICompositionGlTexture CreateTexture(CompositionDrawingSurface surface, PixelSize size);
}
