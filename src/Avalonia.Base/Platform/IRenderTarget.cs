using System;
using Avalonia.Metadata;
using Avalonia.Rendering.Composition;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines a render target
    /// </summary>
    /// <remarks>
    /// The interface used for obtaining drawing context from surfaces you can render on.
    /// </remarks>
    [PrivateApi]
    public interface IRenderTarget : IDisposable
    {
        /// <summary>
        /// Gets the properties of the render target.
        /// </summary>
        RenderTargetProperties Properties { get; }

        /// <summary>
        /// Creates an <see cref="IDrawingContextImpl"/> for a rendering session.
        /// </summary>
        /// <param name="sceneInfo">Information about the scene that's about to be rendered into this render target.
        /// This is expected to be reported to the underlying platform and affect the framebuffer size, however
        /// the implementation may choose to ignore that information.
        /// </param>
        /// <param name="properties">Returns various properties about the returned drawing context</param>
        IDrawingContextImpl CreateDrawingContext(RenderTargetSceneInfo sceneInfo, out RenderTargetDrawingContextProperties properties);

        /// <summary>
        /// Gets the current readiness state of the render target.
        /// </summary>
        PlatformRenderTargetState PlatformRenderTargetState => PlatformRenderTargetState.Ready;
        
        public readonly record struct RenderTargetSceneInfo(PixelSize Size, double Scaling, CompositionTransparencyLevel TransparencyLevel);
    }
}
