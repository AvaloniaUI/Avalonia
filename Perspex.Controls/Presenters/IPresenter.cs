// -----------------------------------------------------------------------
// <copyright file="IPresenter.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Presenters
{
    using Perspex.Controls.Primitives;

    /// <summary>
    /// Interface for presenters such as <see cref="ContentPresenter"/> and 
    /// <see cref="ItemsPresenter"/>.
    /// </summary>
    /// <remarks>
    /// A presenter is the gateway between a templated control and its content. When
    /// a control which implements <see cref="IPresenter"/> is found in the template
    /// of a <see cref="TemplatedControl"/> then that signals that the visual child
    /// of the presenter is not a part of the template.
    /// </remarks>
    public interface IPresenter : IVisual, INamed
    {
    }
}
