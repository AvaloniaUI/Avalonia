// -----------------------------------------------------------------------
// <copyright file="IRenderManager.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Rendering
{
    using System;
    using System.Reactive;

    /// <summary>
    /// Defines the interface for a <see cref="RenderManager"/>.
    /// </summary>
    public interface IRenderManager
    {
        /// <summary>
        /// Gets an observable that is fired whenever a render is required.
        /// </summary>
        IObservable<Unit> RenderNeeded { get; }

        /// <summary>
        /// Invalidates the render for the specified visual and raises <see cref="RenderNeeded"/>.
        /// </summary>
        /// <param name="visual">The visual.</param>
        void InvalidateRender(IVisual visual);

        /// <summary>
        /// Called when rendering is finished.
        /// </summary>
        void RenderFinished();
    }
}
