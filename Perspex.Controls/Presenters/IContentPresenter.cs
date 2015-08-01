// -----------------------------------------------------------------------
// <copyright file="IContentPresenter.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Presenters
{
    /// <summary>
    /// Interface for controls that present a single item of data inside a 
    /// <see cref="TemplatedControl"/> template.
    /// </summary>
    public interface IContentPresenter : IPresenter
    {
        /// <summary>
        /// Gets the control displayed by the presenter.
        /// </summary>
        IControl Child { get; }

        /// <summary>
        /// Gets or sets the content to be displayed by the presenter.
        /// </summary>
        object Content { get; set; }
    }
}