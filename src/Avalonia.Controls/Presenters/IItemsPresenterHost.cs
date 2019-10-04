// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.LogicalTree;
using Avalonia.Styling;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Represents a control which hosts an items presenter.
    /// </summary>
    /// <remarks>
    /// This interface is implemented by <see cref="ItemsControl"/> which usually contains an
    /// <see cref="ItemsPresenter"/> and exposes it through its 
    /// <see cref="ItemsControl.Presenter"/> property. ItemsPresenters can be within
    /// nested templates or in popups and so are not necessarily created immediately when the
    /// parent control's template is instantiated so they register themselves using this 
    /// interface.
    /// </remarks>
    public interface IItemsPresenterHost : ITemplatedControl, ILogical
    {
        /// <summary>
        /// Creates the container which will display an item in the presenter.
        /// </summary>
        /// <param name="data">The item.</param>
        /// <returns>The container control.</returns>
        IControl CreateContainer(object data);

        /// <summary>
        /// Registers an <see cref="IItemsPresenter"/> with a host control.
        /// </summary>
        /// <param name="presenter">The items presenter.</param>
        void RegisterItemsPresenter(IItemsPresenter presenter);
    }
}
