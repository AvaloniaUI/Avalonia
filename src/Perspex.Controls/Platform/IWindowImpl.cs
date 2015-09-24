// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Platform
{
    /// <summary>
    /// Defines a platform-specific window implementation.
    /// </summary>
    public interface IWindowImpl : ITopLevelImpl
    {
        /// <summary>
        /// Gets the maximum size of a window on the system.
        /// </summary>
        Size MaxClientSize { get; }

        /// <summary>
        /// Sets the title of the window.
        /// </summary>
        /// <param name="title">The title.</param>
        void SetTitle(string title);

        /// <summary>
        /// Shows the window.
        /// </summary>
        void Show();

        /// <summary>
        /// Shows the window as a dialog.
        /// </summary>
        /// <returns>
        /// An <see cref="IDisposable"/> that should be used to close the window.
        /// </returns>
        IDisposable ShowDialog();

        /// <summary>
        /// Hides the window.
        /// </summary>
        void Hide();
    }
}
