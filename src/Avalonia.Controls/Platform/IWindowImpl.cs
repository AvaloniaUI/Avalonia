// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;

namespace Avalonia.Platform
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
        /// Gets or sets the minimized/maximized state of the window.
        /// </summary>
        WindowState WindowState { get; set; }

        /// <summary>
        /// Sets the title of the window.
        /// </summary>
        /// <param name="title">The title.</param>
        void SetTitle(string title);

        /// <summary>
        /// Shows the window as a dialog.
        /// </summary>
        /// <returns>
        /// An <see cref="IDisposable"/> that should be used to close the window.
        /// </returns>
        IDisposable ShowDialog();

        /// <summary>
        /// Enables of disables system window decorations (title bar, buttons, etc)
        /// </summary>
        void SetSystemDecorations(bool enabled);

        /// <summary>
        /// Sets the icon of this window.
        /// </summary>
        void SetIcon(IWindowIconImpl icon);
    }
}
