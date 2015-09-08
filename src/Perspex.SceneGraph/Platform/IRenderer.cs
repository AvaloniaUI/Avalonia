// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

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
        /// Gets the number of times <see cref="Render"/> has been called.
        /// </summary>
        int RenderCount { get; }

        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        /// <param name="handle">An optional platform-specific handle.</param>
        void Render(IVisual visual, IPlatformHandle handle);

        /// <summary>
        /// Resizes the rendered viewport.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        void Resize(int width, int height);
    }
}
