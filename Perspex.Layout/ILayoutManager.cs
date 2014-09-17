// -----------------------------------------------------------------------
// <copyright file="ILayoutManager.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Layout
{
    using System;
    using System.Reactive;

    public interface ILayoutManager
    {
        IObservable<Unit> LayoutNeeded { get; }

        void ExecuteLayoutPass();

        void InvalidateMeasure(ILayoutable item);

        void InvalidateArrange(ILayoutable item);
    }
}
