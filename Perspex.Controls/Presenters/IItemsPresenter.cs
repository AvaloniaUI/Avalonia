// -----------------------------------------------------------------------
// <copyright file="IPresenter.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Presenters
{
    public interface IItemsPresenter : IPresenter
    {
        Panel Panel { get; }
    }
}
