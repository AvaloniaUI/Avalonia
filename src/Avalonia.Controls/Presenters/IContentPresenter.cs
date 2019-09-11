// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Primitives;

namespace Avalonia.Controls.Presenters
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
