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

    public class RenderManager : IRenderManager
    {
        private Subject<Unit> renderNeeded = new Subject<Unit>();

        private bool renderQueued;

        public IObservable<Unit> RenderNeeded
        {
            get { return this.renderNeeded; }
        }

        public bool RenderQueued
        {
            get { return this.renderQueued; }
        }

        public void InvalidateRender(IVisual visual)
        {
            if (!this.renderQueued)
            {
                this.renderNeeded.OnNext(Unit.Default);
                this.renderQueued = true;
            }
        }

        public void RenderFinished()
        {
            this.renderQueued = false;
        }
    }
}
