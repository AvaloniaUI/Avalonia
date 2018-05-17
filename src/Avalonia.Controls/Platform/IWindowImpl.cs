// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines a platform-specific window implementation.
    /// </summary>
    public interface IWindowImpl : IWindowBaseImpl
    {
        /// <summary>
        /// Gets or sets the minimized/maximized state of the window.
        /// </summary>
        WindowState WindowState { get; set; }

        /// <summary>
        /// Gets or sets a method called when the minimized/maximized state of the window changes.
        /// </summary>
        Action<WindowState> WindowStateChanged { get; set; }

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
        /// Enables or disables system window decorations (title bar, buttons, etc)
        /// </summary>
        void SetSystemDecorations(bool enabled);

        /// <summary>
        /// Sets the icon of this window.
        /// </summary>
        void SetIcon(IWindowIconImpl icon);

        /// <summary>
        /// Enables or disables the taskbar icon
        /// </summary>
        void ShowTaskbarIcon(bool value);

        /// <summary>
        /// Enables or disables resizing of the window
        /// </summary>
        void CanResize(bool value);

        /// <summary>
        /// Gets or sets a method called before the underlying implementation is destroyed.
        /// Return true to prevent the underlying implementation from closing.
        /// </summary>
        Func<bool> Closing { get; set; }
    }
}
