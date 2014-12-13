// -----------------------------------------------------------------------
// <copyright file="IScrollInfo.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using System;

    /// <summary>
    /// Allows the child of a <see cref="ScrollViewer"/> to communicate with the scroll viewer.
    /// </summary>
    /// <remarks>
    /// Note that this interface has a different purpose to IScrollInfo in WPF! I would give it
    /// a different name, but it does what it suggests - give information about the scrolling
    /// requirements of a control.
    /// </remarks>
    public interface IScrollInfo
    {
        IObservable<bool> CanScrollHorizontally { get; }

        IObservable<bool> IsHorizontalScrollBarVisible { get; }
    }
}
