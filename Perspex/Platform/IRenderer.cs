// -----------------------------------------------------------------------
// <copyright file="IRenderer.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using System;

    public interface IRenderer
    {
        /// <summary>
        /// Initializes the renderer to draw to the specified handle.
        /// </summary>
        /// <param name="handle">The window etc handle</param>
        /// <param name="width">The initial viewport width.</param>
        /// <param name="height">The initial viewport height.</param>
        /// <remarks>
        /// TODO: This probably should be somewhere else...
        /// </remarks>
        void Initialize(IntPtr handle, double width, double height);

        /// <summary>
        /// Renders the specified visual.
        /// </summary>
        /// <param name="visual">The visual to render.</param>
        void Render(IVisual visual);

        /// <summary>
        /// Resizes the rendered viewport.
        /// </summary>
        /// <param name="width">The new width.</param>
        /// <param name="height">The new height.</param>
        void Resize(int width, int height);
    }
}
