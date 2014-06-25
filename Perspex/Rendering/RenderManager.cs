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

        public IObservable<Unit> RenderNeeded
        {
            get { return this.renderNeeded; }
        }

        public void InvalidateRender(IVisual visual)
        {
            this.renderNeeded.OnNext(Unit.Default);
        }
    }
}
