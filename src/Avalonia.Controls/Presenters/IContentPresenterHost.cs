// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Collections;
using Avalonia.LogicalTree;
using Avalonia.Styling;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Represents a control which hosts a content presenter.
    /// </summary>
    /// <remarks>
    /// This interface is implemented by <see cref="ContentControl"/> which usually contains a
    /// <see cref="ContentPresenter"/> and exposes it through its 
    /// <see cref="ContentControl.Presenter"/> property. ContentPresenters can be within
    /// nested templates or in popups and so are not necessarily created immediately when the
    /// parent control's template is instantiated so they register themselves using this 
    /// interface.
    /// </remarks>
    public interface IContentPresenterHost : ITemplatedControl
    {
        /// <summary>
        /// Gets a collection describing the logical children of the host control.
        /// </summary>
        IAvaloniaList<ILogical> LogicalChildren { get; }

        /// <summary>
        /// Registers an <see cref="IContentPresenter"/> with a host control.
        /// </summary>
        /// <param name="presenter">The content presenter.</param>
        /// <returns>
        /// True if the content presenter should add its child to the logical children of the
        /// host; otherwise false.
        /// </returns>
        bool RegisterContentPresenter(IContentPresenter presenter);
    }
}
