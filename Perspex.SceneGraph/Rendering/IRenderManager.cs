// -----------------------------------------------------------------------
// <copyright file="IRenderManager.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Rendering
{
    using System;
    using System.Reactive;

    public interface IRenderManager
    {
        IObservable<Unit> RenderNeeded { get; }

        void InvalidateRender(IVisual visual);

        void RenderFinished();
    }
}
