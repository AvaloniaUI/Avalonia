// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Media;

namespace Perspex.Platform
{
    /// <summary>
    /// Defines a renderer.
    /// </summary>
    /// <remarks>
    /// The interface used to render <see cref="IVisual"/>s. You will usually want to inherit from
    /// <see cref="Perspex.Rendering.RendererBase"/> rather than implementing the whole interface
    /// as RenderBase has a default implementation for the non-platform specific parts of a
    /// renderer.
    /// </remarks>
    public interface IRenderer : IDisposable
    {
        /// <summary>
        /// Creates an <see cref="IDrawingContext"/> for a rendering session.
        /// </summary>
        /// <param name="handle">The handle to use to create the context.</param>
        /// <returns>An <see cref="IDrawingContext"/>.</returns>
        IDrawingContext CreateDrawingContext(IPlatformHandle target);

        /// <summary>
        /// Resizes the rendered viewport.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        void Resize(int width, int height);
    }
}
