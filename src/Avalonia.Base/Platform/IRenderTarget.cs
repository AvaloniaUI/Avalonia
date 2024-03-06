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
}
