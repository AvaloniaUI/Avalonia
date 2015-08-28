﻿// -----------------------------------------------------------------------
// <copyright file="ILayoutManager.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Layout
{
    using System;
    using System.Reactive;

    /// <summary>
    /// Manages measuring and arranging of controls.
    /// </summary>
    /// <remarks>
    /// Each layout root element such as a window has its own LayoutManager that is responsible
    /// for laying out its child controls. When a layout is required the <see cref="LayoutNeeded"/>
    /// observable will fire and the root element should respond by calling
    /// <see cref="ExecuteLayoutPass"/> at the earliest opportunity to carry out the layout.
    /// </remarks>
    public interface ILayoutManager
    {
        /// <summary>
        /// Gets or sets the root element that the manager is attached to.
        /// </summary>
        /// <remarks>
        /// This must be set before the layout manager can be used.
        /// </remarks>
        ILayoutRoot Root { get; set; }

        /// <summary>
        /// Gets an observable that is fired when a layout pass is needed.
        /// </summary>
        IObservable<Unit> LayoutNeeded { get; }

        /// <summary>
        /// Gets an observable that is fired when a layout pass is completed.
        /// </summary>
        IObservable<Unit> LayoutCompleted { get; }

        /// <summary>
        /// Gets a value indicating whether a layout is queued.
        /// </summary>
        /// <remarks>
        /// Returns true when <see cref="LayoutNeeded"/> has been fired, but
        /// <see cref="ExecuteLayoutPass"/> has not yet been called.
        /// </remarks>
        bool LayoutQueued { get; }

        /// <summary>
        /// Executes a layout pass.
        /// </summary>
        void ExecuteLayoutPass();

        /// <summary>
        /// Notifies the layout manager that a control requires a measure.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="distance">The control's distance from the layout root.</param>
        void InvalidateMeasure(ILayoutable control, int distance);

        /// <summary>
        /// Notifies the layout manager that a control requires an arrange.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="distance">The control's distance from the layout root.</param>
        void InvalidateArrange(ILayoutable control, int distance);
    }
}
