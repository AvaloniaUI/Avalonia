using System;
using Avalonia.Metadata;
using Avalonia.Rendering;

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
        /// Creates an <see cref="IDrawingContextImpl"/> for a rendering session.
        /// </summary>
        /// <param name="useScaledDrawing">Apply DPI reported by the render target as a hidden transform matrix</param>
        IDrawingContextImpl CreateDrawingContext(bool useScaledDrawing);
        
        /// <summary>
        /// Indicates if the render target is no longer usable and needs to be recreated
        /// </summary>
        public bool IsCorrupted { get; }
    }

    [PrivateApi, Obsolete("Use IRenderTarget2", true)]
    // TODO12: Remove
    public interface IRenderTargetWithProperties : IRenderTarget
    {
        RenderTargetProperties Properties { get; }
    }
    
    [PrivateApi]
    // TODO12: Merge into IRenderTarget
    public interface IRenderTarget2 : IRenderTarget
    {
        RenderTargetProperties Properties { get; }

        /// <summary>
        /// Creates an <see cref="IDrawingContextImpl"/> for a rendering session.
        /// </summary>
        /// <param name="expectedPixelSize">The pixel size of the surface</param>
        /// <param name="properties">Returns various properties about the returned drawing context</param>
        IDrawingContextImpl CreateDrawingContext(PixelSize expectedPixelSize,
            out RenderTargetDrawingContextProperties properties);
    }
    
    internal static class RenderTargetExtensions
    {
        public static IDrawingContextImpl CreateDrawingContextWithProperties(
            this IRenderTarget renderTarget,
            PixelSize expectedPixelSize,
            out RenderTargetDrawingContextProperties properties)
        {
            if (renderTarget is IRenderTarget2 target)
                return target.CreateDrawingContext(expectedPixelSize, out properties);
            properties = default;
            return renderTarget.CreateDrawingContext(false);
        }
    }
}
