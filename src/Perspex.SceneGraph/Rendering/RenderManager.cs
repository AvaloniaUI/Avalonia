// -----------------------------------------------------------------------
// <copyright file="RenderManager.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Rendering
{
    using System;
    using System.Reactive;
    using System.Reactive.Subjects;

    /// <summary>
    /// Schedules the rendering of a tree.
    /// </summary>
    public class RenderManager : IRenderManager
    {
        private Subject<Unit> renderNeeded = new Subject<Unit>();

        private bool renderQueued;

        /// <summary>
        /// Gets an observable that is fired whenever a render is required.
        /// </summary>
        public IObservable<Unit> RenderNeeded
        {
            get { return this.renderNeeded; }
        }

        /// <summary>
        /// Gets a valuue indicating whether a render is queued.
        /// </summary>
        public bool RenderQueued
        {
            get { return this.renderQueued; }
        }

        /// <summary>
        /// Invalidates the render for the specified visual and raises <see cref="RenderNeeded"/>.
        /// </summary>
        /// <param name="visual">The visual.</param>
        public void InvalidateRender(IVisual visual)
        {
            if (!this.renderQueued)
            {
                this.renderNeeded.OnNext(Unit.Default);
                this.renderQueued = true;
            }
        }

        /// <summary>
        /// Called when rendering is finished.
        /// </summary>
        public void RenderFinished()
        {
            this.renderQueued = false;
        }
    }
}
