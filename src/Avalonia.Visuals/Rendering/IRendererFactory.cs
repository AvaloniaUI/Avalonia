using System;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Defines a factory for creating <see cref="IRenderer"/> instances.
    /// </summary>
    public interface IRendererFactory
    {
        /// <summary>
        /// Creates a new renderer for the specified render root.
        /// </summary>
        /// <param name="root">The render root.</param>
        /// <param name="renderLoop">The render loop.</param>
        /// <returns>An instance of an <see cref="IRenderer"/>.</returns>
        IRenderer CreateRenderer(IRenderRoot root, IRenderLoop renderLoop);
    }
}
