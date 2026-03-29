using System;
using Avalonia.Metadata;

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
        /// Indicates if the render target is no longer usable and needs to be recreated
        /// </summary>
        bool IsCorrupted { get; }

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
        /// Indicates if the render target is currently ready to be rendered to
        /// </summary>
        bool IsReady => true;
        
        public record struct RenderTargetSceneInfo(PixelSize Size, double Scaling, Thickness ShadowExtents = default);
    }
}
